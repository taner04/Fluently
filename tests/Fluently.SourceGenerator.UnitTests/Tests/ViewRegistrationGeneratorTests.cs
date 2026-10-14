using Fluently.SourceGenerator.UnitTests.Infrastructure;

namespace Fluently.SourceGenerator.UnitTests.Tests;

public sealed class ViewRegistrationGeneratorTests
{
    private const string GetRequiredService =
        "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService";

    private const string GetService =
        "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService";

    [Fact]
    public void Generator_Should_RegisterViewsOrderedByViewModel_When_ViewsImplementViewFor()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              namespace App;

                              public sealed class ZViewModel;
                              public sealed class AViewModel;

                              internal sealed class ZView(ZViewModel viewModel) : UserControl, IViewFor<ZViewModel>
                              {
                                  public ZViewModel ViewModel { get; } = viewModel;
                              }

                              public sealed class AView(AViewModel viewModel) : UserControl, IViewFor<AViewModel>
                              {
                                  public AViewModel ViewModel { get; } = viewModel;
                              }
                              """;

        var first = GeneratorTestHelper.Run(source);
        var second = GeneratorTestHelper.Run(source);

        var a = first.GeneratedSource!.IndexOf(
            "global::Fluently.ViewRegistry.Add<global::App.AViewModel>(static (services, viewModel) => new global::App.AView(viewModel));",
            StringComparison.Ordinal);
        var z = first.GeneratedSource.IndexOf(
            "global::Fluently.ViewRegistry.Add<global::App.ZViewModel>(static (services, viewModel) => new global::App.ZView(viewModel));",
            StringComparison.Ordinal);
        a.Should().BeGreaterThanOrEqualTo(0);
        z.Should().BeGreaterThan(a);
        first.GeneratedSource.Should().Contain("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        second.GeneratedSource.Should().Be(first.GeneratedSource);
        AssertNoErrors(first);
    }

    [Fact]
    public void Generator_Should_ResolveFurtherConstructorParameters_FromServices()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;
                              public sealed class Audio;
                              public sealed class Theme;

                              public sealed class CallView : UserControl, IViewFor<ViewModel>
                              {
                                  public CallView(ViewModel viewModel) => ViewModel = viewModel;

                                  public CallView(ViewModel viewModel, Audio audio, Theme? theme) => ViewModel = viewModel;

                                  public ViewModel ViewModel { get; }
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratedSource.Should().Contain(
            $"new global::CallView(viewModel, {GetRequiredService}<global::Audio>(services), {GetService}<global::Theme>(services))");
        AssertNoErrors(result);
    }

    [Fact]
    public void Generator_Should_RegisterNestedAndPartialViewsOnce()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;

                              public static partial class Views
                              {
                                  internal sealed partial class NestedView : UserControl, IViewFor<ViewModel>;

                                  internal sealed partial class NestedView
                                  {
                                      public NestedView(ViewModel viewModel) => ViewModel = viewModel;

                                      public ViewModel ViewModel { get; }
                                  }
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratedSource.Should().Contain("new global::Views.NestedView(viewModel)");
        result.GeneratedSource!.Split("ViewRegistry.Add<").Should().HaveCount(2);
        AssertNoErrors(result);
    }

    [Fact]
    public void Generator_Should_SkipAbstractAndGenericViews()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;

                              public abstract class BaseView : UserControl, IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel => null!;
                              }

                              public sealed class GenericView<T> : UserControl, IViewFor<T>
                              {
                                  public T ViewModel => default!;
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratedSource.Should().BeNull();
        AssertNoErrors(result);
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewIsNotAUiElement()
    {
        const string source = """
                              using Fluently;

                              public sealed class ViewModel;

                              public sealed class PlainView(ViewModel viewModel) : IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel { get; } = viewModel;
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY001")
            .Which.GetMessage().Should().Contain("Avalonia.Controls.Control");
        result.GeneratedSource.Should().BeNull();
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewIsPrivate()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;

                              public sealed class Outer
                              {
                                  private sealed class HiddenView(ViewModel viewModel) : UserControl, IViewFor<ViewModel>
                                  {
                                      public ViewModel ViewModel { get; } = viewModel;
                                  }
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY002");
        result.GeneratedSource.Should().BeNull();
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewModelHasTwoViews()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;

                              public sealed class FirstView(ViewModel viewModel) : UserControl, IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel { get; } = viewModel;
                              }

                              public sealed class SecondView(ViewModel viewModel) : UserControl, IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel { get; } = viewModel;
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY003")
            .Which.GetMessage().Should().Contain("SecondView").And.Contain("FirstView");
        result.GeneratedSource.Should().Contain("new global::FirstView(").And.NotContain("new global::SecondView(");
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewHasNoConstructorForViewModel()
    {
        const string source = """
                              using Avalonia.Controls;
                              using Fluently;

                              public sealed class ViewModel;

                              public sealed class LooseView : UserControl, IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel => null!;
                              }
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY004")
            .Which.GetMessage().Should().Contain("LooseView").And.Contain("ViewModel");
        result.GeneratedSource.Should().BeNull();
    }

    [Fact]
    public void Generator_Should_GenerateNothing_When_FluentlyIsNotReferenced()
    {
        var result = GeneratorTestHelper.Run("public sealed class Anything;", referenceFluently: false);

        result.GeneratedSource.Should().BeNull();
        result.GeneratorDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generator_Should_GenerateNothing_When_ProjectHasNoViews()
    {
        const string source = """
                              using Fluently;

                              public sealed class ViewModel : INavigableViewModel;
                              """;

        var result = GeneratorTestHelper.Run(source);

        result.GeneratedSource.Should().BeNull();
        result.GeneratorDiagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Generator_Should_RegisterAnyClass_When_NoKnownUiFrameworkIsReferenced()
    {
        const string source = """
                              using Fluently;

                              public sealed class ViewModel;

                              public sealed class ConsoleView(ViewModel viewModel) : IViewFor<ViewModel>
                              {
                                  public ViewModel ViewModel { get; } = viewModel;
                              }
                              """;

        var result = GeneratorTestHelper.Run(source, referenceAvalonia: false);

        result.GeneratedSource.Should().Contain("new global::ConsoleView(viewModel)");
        AssertNoErrors(result);
    }

    private static void AssertNoErrors(GeneratorTestResult result)
    {
        result.GeneratorDiagnostics.Should().BeEmpty();
        result.CompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}
