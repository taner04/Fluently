using Fluently.Avalonia.SourceGenerator.Generators.Views;

namespace Fluently.Avalonia.SourceGenerator.Generators.Views.Models;

internal readonly record struct ViewCandidate(
    string ViewTypeName,
    string ViewModelTypeName,
    SourceLocation Location,
    ViewDiagnosticKind? Problem);
