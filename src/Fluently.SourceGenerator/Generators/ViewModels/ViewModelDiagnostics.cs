namespace Fluently.SourceGenerator.Generators.ViewModels;

internal static class ViewModelDiagnostics
{
    private const string Category = "Fluently.ViewModels";

    private static readonly DiagnosticDescriptor AttributeDoesNotMatch = new(
        "FLUENTLY005",
        "Registration attribute does not match the view model",
        "'{0}' has [{1}] but does not implement {2}",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor SeveralAttributes = new(
        "FLUENTLY006",
        "View model has several registration attributes",
        "'{0}' has more than one of [SingletonPage], [TransientPage], [ScopedPage], [Dialog] and [Toast], so it is not registered",
        Category,
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NotCreatable = new(
        "FLUENTLY007",
        "View model cannot be created by the container",
        "'{0}' must be a public or internal class that is neither abstract, static nor generic to be registered",
        Category,
        DiagnosticSeverity.Error,
        true);

    internal static Diagnostic Create(ViewModelDiagnosticKind kind, ViewModelCandidate candidate)
    {
        var descriptor = kind switch
        {
            ViewModelDiagnosticKind.AttributeDoesNotMatch => AttributeDoesNotMatch,
            ViewModelDiagnosticKind.SeveralAttributes => SeveralAttributes,
            ViewModelDiagnosticKind.NotCreatable => NotCreatable,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        return Diagnostic.Create(
            descriptor,
            candidate.Location.ToLocation(),
            candidate.TypeName,
            AttributeName(candidate.Kind),
            ExpectedInterface(candidate.Kind));
    }

    private static string AttributeName(ViewModelRegistrationKind kind) => kind.ToString();

    private static string ExpectedInterface(ViewModelRegistrationKind kind) => kind switch
    {
        ViewModelRegistrationKind.Dialog => "IDialog<TResult>",
        ViewModelRegistrationKind.Toast => "IToast",
        _ => "INavigableViewModel"
    };
}
