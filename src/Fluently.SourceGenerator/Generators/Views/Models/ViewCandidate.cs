namespace Fluently.SourceGenerator.Generators.Views.Models;

internal readonly record struct ViewCandidate(
    string ViewTypeName,
    string ViewModelTypeName,
    SourceLocation Location,
    ViewDiagnosticKind? Problem,
    string ExpectedBaseTypes,
    string ConstructorArguments);
