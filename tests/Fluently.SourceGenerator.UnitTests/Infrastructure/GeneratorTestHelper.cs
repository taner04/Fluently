using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.SourceGenerator.UnitTests.Infrastructure;

internal static class GeneratorTestHelper
{
    private static readonly MetadataReference[] PlatformReferences =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(static path => !path.StartsWith(AppContext.BaseDirectory, StringComparison.Ordinal))
        .Select(static path => MetadataReference.CreateFromFile(path))
    ];

    private static readonly MetadataReference[] FluentlyReferences =
    [
        MetadataReference.CreateFromFile(typeof(ViewRegistry).Assembly.Location),
        MetadataReference.CreateFromFile(
            typeof(IServiceCollection).Assembly.Location)
    ];

    private static readonly MetadataReference[] AvaloniaReferences =
    [
        MetadataReference.CreateFromFile(typeof(Control).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(AvaloniaObject).Assembly.Location)
    ];

    internal static GeneratorTestResult Run(string source, bool referenceFluently = true,
        bool referenceAvalonia = true) =>
        Run(new ViewRegistrationGenerator(), "Fluently.ViewRegistrations.g.cs", source, referenceFluently,
            referenceAvalonia,
            LanguageVersion.Latest, null);

    internal static GeneratorTestResult RunViewModels(
        string source,
        bool referenceFluently = true,
        LanguageVersion languageVersion = LanguageVersion.Latest,
        string? rootNamespace = null) =>
        Run(new ViewModelRegistrationGenerator(), "Fluently.ViewModelRegistrations.g.cs", source, referenceFluently,
            false,
            languageVersion, rootNamespace);

    private static GeneratorTestResult Run(
        IIncrementalGenerator generator,
        string hintName,
        string source,
        bool referenceFluently,
        bool referenceAvalonia,
        LanguageVersion languageVersion,
        string? rootNamespace)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        MetadataReference[] references =
        [
            .. PlatformReferences,
            .. referenceFluently ? FluentlyReferences : [],
            .. referenceAvalonia ? AvaloniaReferences : []
        ];
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, "TestSource.cs");
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: new TestAnalyzerConfigOptionsProvider(rootNamespace is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["build_property.RootNamespace"] = rootNamespace }));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var runResult = driver.GetRunResult();
        var generatedSource = runResult.Results
            .Single()
            .GeneratedSources
            .SingleOrDefault(generated => generated.HintName == hintName)
            .SourceText?
            .ToString();

        return new GeneratorTestResult(
            generatedSource,
            runResult.Diagnostics,
            outputCompilation.GetDiagnostics());
    }
}
