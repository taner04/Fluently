namespace Fluently.SourceGenerator.UnitTests.Infrastructure;

internal sealed record GeneratorTestResult(
    string? GeneratedSource,
    IReadOnlyList<Diagnostic> GeneratorDiagnostics,
    IReadOnlyList<Diagnostic> CompilationDiagnostics)
{
    internal void ShouldHaveNoErrors()
    {
        GeneratorDiagnostics.Should().BeEmpty();
        CompilationDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }
}
