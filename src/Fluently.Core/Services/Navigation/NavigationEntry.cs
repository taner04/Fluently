using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
/// One page in the navigation history, with the parameter it was entered with and the scope it was created from.
/// </summary>
internal sealed record NavigationEntry(
    INavigableViewModel Page,
    NavigationParameter? Parameter,
    AsyncServiceScope Scope)
{
    public Task<bool> CanLeaveAsync(CancellationToken cancellationToken) =>
        Page is ICanNavigateFrom page ? page.CanNavigateFromAsync(cancellationToken) : Task.FromResult(true);

    public Task EnterAsync(CancellationToken cancellationToken) =>
        Parameter is not null
            ? Parameter.EnterAsync(Page, cancellationToken)
            : Page is INavigatedTo page
                ? page.OnNavigatedToAsync(cancellationToken)
                : Task.CompletedTask;

    public Task LeaveAsync(CancellationToken cancellationToken) =>
        Page is INavigatedFrom page ? page.OnNavigatedFromAsync(cancellationToken) : Task.CompletedTask;
}
