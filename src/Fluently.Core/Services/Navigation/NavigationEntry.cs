using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
///     One page in the navigation history, with the parameter it was entered with and the scope it was created from.
/// </summary>
internal sealed record NavigationEntry(
    INavigableViewModel Page,
    NavigationParameter? Parameter,
    AsyncServiceScope Scope)
{
    public Task EnterAsync(CancellationToken cancellationToken) =>
        Lifecycle.EnterAsync(Page, Parameter, cancellationToken);

    public Task LeaveAsync(CancellationToken cancellationToken) => Lifecycle.LeaveAsync(Page, cancellationToken);
}
