using Fluently.SourceGenerator.UnitTests.Infrastructure;

namespace Fluently.SourceGenerator.UnitTests.Tests;

public sealed class ViewModelRegistrationGeneratorTests
{
    private const string Extensions =
        "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions";

    private const string ViewModels = """
                                      using Fluently;

                                      namespace App;

                                      [SingletonPage]
                                      public sealed class HomeViewModel : INavigableViewModel;

                                      [TransientPage]
                                      internal sealed class DetailsViewModel : INavigatedTo<int>
                                      {
                                          public System.Threading.Tasks.Task OnNavigatedToAsync(int parameter, System.Threading.CancellationToken cancellationToken) =>
                                              System.Threading.Tasks.Task.CompletedTask;
                                      }

                                      [ScopedPage]
                                      public sealed class CheckoutViewModel : INavigableViewModel;

                                      [Dialog]
                                      public sealed class ConfirmViewModel : IDialog<bool>;

                                      [Toast]
                                      public sealed class UndoToastViewModel : IToast;
                                      """;

    [Fact]
    public void Generator_Should_RegisterRecordViewModels()
    {
        var result = GeneratorTestHelper.RunViewModels("""
                                                       using Fluently;

                                                       namespace App;

                                                       [Dialog]
                                                       public sealed record ConfirmViewModel : IDialog<bool>;
                                                       """);

        result.ShouldHaveNoErrors();
        result.GeneratedSource.Should()
            .Contain($"{Extensions}.TryAddTransient<global::App.ConfirmViewModel>(services);");
    }

    [Fact]
    public void Generator_Should_RegisterEveryAttributedViewModel_WithItsLifetime()
    {
        var result = GeneratorTestHelper.RunViewModels(ViewModels);

        result.GeneratedSource.Should()
            .Contain($"{Extensions}.TryAddSingleton<global::App.HomeViewModel>(services);")
            .And.Contain($"{Extensions}.TryAddTransient<global::App.DetailsViewModel>(services);")
            .And.Contain($"{Extensions}.TryAddScoped<global::App.CheckoutViewModel>(services);")
            .And.Contain($"{Extensions}.TryAddTransient<global::App.ConfirmViewModel>(services);")
            .And.Contain($"{Extensions}.TryAddTransient<global::App.UndoToastViewModel>(services);");
        result.ShouldHaveNoErrors();
    }

    [Fact]
    public void Generator_Should_OrderRegistrationsByType_AndBeDeterministic()
    {
        var first = GeneratorTestHelper.RunViewModels(ViewModels);
        var second = GeneratorTestHelper.RunViewModels(ViewModels);

        var source = first.GeneratedSource!;
        source.IndexOf("CheckoutViewModel", StringComparison.Ordinal).Should()
            .BeLessThan(source.IndexOf("ConfirmViewModel", StringComparison.Ordinal));
        source.IndexOf("HomeViewModel", StringComparison.Ordinal).Should()
            .BeLessThan(source.IndexOf("UndoToastViewModel", StringComparison.Ordinal));
        second.GeneratedSource.Should().Be(source);
    }

    [Fact]
    public void Generator_Should_CompileWithCSharp12_When_ProjectUsesTheNet8Default()
    {
        var result = GeneratorTestHelper.RunViewModels(
            ViewModels + """

                         internal static class Startup
                         {
                             internal static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                                 services.AddViewModels();
                         }
                         """,
            languageVersion: LanguageVersion.CSharp12,
            rootNamespace: "App");

        result.ShouldHaveNoErrors();
    }

    [Fact]
    public void Generator_Should_GenerateEmptyAddViewModels_When_NoViewModelHasAnAttribute()
    {
        var result = GeneratorTestHelper.RunViewModels("""
                                                       internal static class Startup
                                                       {
                                                           internal static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                                                               GeneratorTests.ViewModelRegistrations.AddViewModels(services);
                                                       }
                                                       """);

        result.GeneratedSource.Should().Contain("AddViewModels(").And.NotContain("TryAdd");
        result.ShouldHaveNoErrors();
    }

    [Fact]
    public void Generator_Should_GenerateIntoRootNamespace_SoNestedNamespacesFindIt()
    {
        var result = GeneratorTestHelper.RunViewModels(
            """
            namespace App
            {
                [Fluently.TransientPage]
                public sealed class HomeViewModel : Fluently.INavigableViewModel;
            }

            namespace App.Startup
            {
                internal static class Registration
                {
                    internal static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                        services.AddViewModels();
                }
            }
            """,
            rootNamespace: "App");

        result.GeneratedSource.Should().MatchRegex(@"namespace App\r?\n");
        result.ShouldHaveNoErrors();
    }

    [Fact]
    public void Generator_Should_EscapeKeywords_When_RootNamespaceContainsThem()
    {
        var result = GeneratorTestHelper.RunViewModels(
            """
            [Fluently.TransientPage]
            public sealed class HomeViewModel : Fluently.INavigableViewModel;
            """,
            rootNamespace: "acme.event");

        result.GeneratedSource.Should().Contain("namespace acme.@event");
        result.ShouldHaveNoErrors();
    }

    [Fact]
    public void Generator_Should_GenerateIntoAssemblyNamespace_When_RootNamespaceIsUnknown()
    {
        var result = GeneratorTestHelper.RunViewModels(ViewModels);

        result.GeneratedSource.Should().Contain("namespace GeneratorTests");
    }

    [Fact]
    public void Generator_Should_RegisterPartialClassOnce()
    {
        var result = GeneratorTestHelper.RunViewModels("""
                                                       using Fluently;

                                                       [TransientPage]
                                                       public sealed partial class SplitViewModel : INavigableViewModel;

                                                       public sealed partial class SplitViewModel;
                                                       """);

        result.GeneratedSource.Should().Contain("TryAddTransient<global::SplitViewModel>(services);");
        result.GeneratedSource!.Split("SplitViewModel").Should().HaveCount(2);
        result.ShouldHaveNoErrors();
    }

    [Theory]
    [InlineData("SingletonPage", "IDialog<bool>", "INavigableViewModel")]
    [InlineData("Dialog", "INavigableViewModel", "IDialog<TResult>")]
    [InlineData("Toast", "INavigableViewModel", "IToast")]
    public void Generator_Should_ReportError_When_AttributeDoesNotMatchTheViewModel(
        string attribute,
        string implemented,
        string expected)
    {
        var result = GeneratorTestHelper.RunViewModels($$"""
                                                         using Fluently;

                                                         [{{attribute}}]
                                                         public sealed class MismatchViewModel : {{implemented}};
                                                         """);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY005")
            .Which.GetMessage().Should().Contain("MismatchViewModel").And.Contain(attribute).And.Contain(expected);
        result.GeneratedSource.Should().NotContain("MismatchViewModel");
    }

    [Fact]
    public void Generator_Should_ReportError_When_ViewModelHasSeveralAttributes()
    {
        var result = GeneratorTestHelper.RunViewModels("""
                                                       using Fluently;

                                                       [SingletonPage]
                                                       [TransientPage]
                                                       public sealed class TornViewModel : INavigableViewModel;
                                                       """);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY006")
            .Which.GetMessage().Should().Contain("TornViewModel");
        result.GeneratedSource.Should().NotContain("TornViewModel");
    }

    [Theory]
    [InlineData("public abstract class AbstractViewModel : INavigableViewModel;", "AbstractViewModel")]
    [InlineData("public sealed class GenericViewModel<T> : INavigableViewModel;", "GenericViewModel")]
    [InlineData(
        "public sealed class Outer { [TransientPage] private sealed class HiddenViewModel : INavigableViewModel; }",
        "HiddenViewModel")]
    public void Generator_Should_ReportError_When_ViewModelCannotBeCreated(string declaration, string name)
    {
        var attributed = declaration.Contains("[TransientPage]", StringComparison.Ordinal)
            ? declaration
            : "[TransientPage] " + declaration;

        var result = GeneratorTestHelper.RunViewModels("using Fluently;\n" + attributed);

        result.GeneratorDiagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "FLUENTLY007")
            .Which.GetMessage().Should().Contain(name);
        result.GeneratedSource.Should().NotContain(name);
    }

    [Fact]
    public void Generator_Should_GenerateNothing_When_FluentlyIsNotReferenced()
    {
        var result = GeneratorTestHelper.RunViewModels("public sealed class Anything;", false);

        result.GeneratedSource.Should().BeNull();
        result.GeneratorDiagnostics.Should().BeEmpty();
    }
}
