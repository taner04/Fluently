namespace Fluently;

/// <summary>
///     A service that creates pages, dialogs or toasts, and knows the scopes of its container, so a host can show their
///     views from the right container.
/// </summary>
internal interface IViewSource
{
    ViewModelScopes Scopes { get; }
}
