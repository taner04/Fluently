namespace Fluently.SourceGenerator.Generators.Views;

internal static class ViewDiagnostics
{
    private const string Category = "Fluently.Views";

    private static readonly DiagnosticDescriptor NotAUiElement = new(
        "FLUENTLY001",
        "View is not a UI element",
        "View '{0}' implements IViewFor<{1}> but does not derive from {2}",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NotAccessible = new(
        "FLUENTLY002",
        "View is not accessible",
        "View '{0}' must be public or internal to be registered",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DuplicateView = new(
        "FLUENTLY003",
        "View model has more than one view",
        "View model '{1}' already has the view '{2}', so '{0}' is not registered",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NoViewModelConstructor = new(
        "FLUENTLY004",
        "View has no constructor for its view model",
        "View '{0}' needs a public or internal constructor whose first parameter is '{1}'; further parameters are resolved from the services",
        Category,
        DiagnosticSeverity.Error,
        true);

    internal static Diagnostic Create(
        ViewDiagnosticKind kind,
        SourceLocation location,
        string viewTypeName,
        string viewModelTypeName,
        string? detail = null)
    {
        var descriptor = kind switch
        {
            ViewDiagnosticKind.NotAUiElement => NotAUiElement,
            ViewDiagnosticKind.NotAccessible => NotAccessible,
            ViewDiagnosticKind.DuplicateView => DuplicateView,
            ViewDiagnosticKind.NoViewModelConstructor => NoViewModelConstructor,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        return Diagnostic.Create(descriptor, location.ToLocation(), viewTypeName, viewModelTypeName, detail);
    }
}
