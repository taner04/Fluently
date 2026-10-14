using Fluently.Avalonia.SourceGenerator.UnitTests.Infrastructure;

namespace Fluently.Avalonia.SourceGenerator.UnitTests.Tests;

public sealed class ViewRegistrationGeneratorTests
{
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

                              internal static class Startup
                              {
                                  internal static void Configure(
                                      Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                                      services.AddFluentlyViews();
                              }
                              """;

        var first = GeneratorTestHelper.Run(source);
        var second = GeneratorTestHelper.Run(source);

        var a = first.GeneratedSource!.IndexOf(
            "views.Add<global::App.AView, global::App.AViewModel>();", StringComparison.Ordinal);
        var z = first.GeneratedSource.IndexOf(
            "views.Add<global::App.ZView, global::App.ZViewModel>();", StringComparison.Ordinal);
        a.Should().BeGreaterThanOrEqualTo(0);
        z.Should().BeGreaterThan(a);
        second.GeneratedSource.Should().Be(first.GeneratedSource);
        AssertNoErrors(first);
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

        result.GeneratedSource.Should().Contain("views.Add<global::Views.NestedView, global::ViewModel>();");
        result.GeneratedSource!.Split("views.Add<").Should().HaveCount(2);
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

        result.GeneratedSource.Should().NotContain("views.Add<");
        AssertNoErrors(result);
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewIsNotAControl()
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

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY001");
        result.GeneratedSource.Should().NotContain("PlainView");
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
        result.GeneratedSource.Should().NotContain("HiddenView");
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
        result.GeneratedSource.Should().Contain("global::FirstView").And.NotContain("global::SecondView");
    }

    [Fact]
    public void Generator_Should_GenerateNothing_When_FluentlyIsNotReferenced()
    {
        var result = GeneratorTestHelper.Run("public sealed class Anything;", referenceFluently: false);

        result.GeneratedSource.Should().BeNull();
        result.GeneratorDiagnostics.Should().BeEmpty();
    }

    private static void AssertNoErrors(GeneratorTestResult result)
    {
        result.GeneratorDiagnostics.Should().BeEmpty();
        result.CompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}
