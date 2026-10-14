namespace Fluently.Avalonia.SourceGenerator.UnitTests.Infrastructure;

internal sealed record GeneratorTestResult(
    string? GeneratedSource,
    IReadOnlyList<Diagnostic> GeneratorDiagnostics,
    IReadOnlyList<Diagnostic> CompilationDiagnostics);
