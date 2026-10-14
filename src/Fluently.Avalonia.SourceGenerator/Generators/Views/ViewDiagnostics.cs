namespace Fluently.Avalonia.SourceGenerator.Generators.Views;

internal enum ViewDiagnosticKind
{
    NotAControl,
    NotAccessible,
    DuplicateView
}

internal static class ViewDiagnostics
{
    private const string Category = "Fluently.Views";

    private static readonly DiagnosticDescriptor NotAControl = new(
        "FLUENTLY001",
        "View is not an Avalonia control",
        "View '{0}' implements IViewFor<{1}> but does not derive from Avalonia.Controls.Control",
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

    internal static Diagnostic Create(
        ViewDiagnosticKind kind,
        SourceLocation location,
        string viewTypeName,
        string viewModelTypeName,
        string? otherViewTypeName = null)
    {
        var descriptor = kind switch
        {
            ViewDiagnosticKind.NotAControl => NotAControl,
            ViewDiagnosticKind.NotAccessible => NotAccessible,
            ViewDiagnosticKind.DuplicateView => DuplicateView,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        return Diagnostic.Create(descriptor, location.ToLocation(), viewTypeName, viewModelTypeName, otherViewTypeName);
    }
}
