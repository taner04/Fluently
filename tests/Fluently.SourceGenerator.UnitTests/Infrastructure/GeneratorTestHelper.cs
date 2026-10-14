using Avalonia.Controls;

namespace Fluently.SourceGenerator.UnitTests.Infrastructure;

internal static class GeneratorTestHelper
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

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
            typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location)
    ];

    private static readonly MetadataReference[] AvaloniaReferences =
    [
        MetadataReference.CreateFromFile(typeof(Control).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Avalonia.AvaloniaObject).Assembly.Location)
    ];

    internal static GeneratorTestResult Run(string source, bool referenceFluently = true, bool referenceAvalonia = true)
    {
        MetadataReference[] references =
        [
            .. PlatformReferences,
            .. referenceFluently ? FluentlyReferences : [],
            .. referenceAvalonia ? AvaloniaReferences : []
        ];
        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions, "TestSource.cs");
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ViewRegistrationGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var runResult = driver.GetRunResult();
        var generatedSource = runResult.Results
            .Single()
            .GeneratedSources
            .SingleOrDefault(static source => source.HintName == "Fluently.ViewRegistrations.g.cs")
            .SourceText?
            .ToString();

        return new GeneratorTestResult(
            generatedSource,
            runResult.Diagnostics,
            outputCompilation.GetDiagnostics());
    }
}
