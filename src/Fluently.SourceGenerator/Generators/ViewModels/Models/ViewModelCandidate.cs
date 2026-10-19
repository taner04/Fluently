namespace Fluently.SourceGenerator.Generators.ViewModels.Models;

internal readonly record struct ViewModelCandidate(
    string TypeName,
    ViewModelRegistrationKind Kind,
    SourceLocation Location,
    ViewModelDiagnosticKind? Problem);
