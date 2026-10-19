using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
///     One dialog that is open or being opened, with the parameter it was opened with and the scope it was created from.
/// </summary>
internal abstract class DialogEntry(IDialog dialog, NavigationParameter? parameter, AsyncServiceScope scope)
{
    public IDialog Dialog { get; } = dialog;

    public AsyncServiceScope Scope { get; } = scope;

    public abstract Type ResultType { get; }

    public bool IsOpen { get; set; }

    public bool IsDismissing { get; set; }

    public Task EnterAsync(CancellationToken cancellationToken) =>
        Lifecycle.EnterAsync(Dialog, parameter, cancellationToken);

    public Task LeaveAsync() => Lifecycle.LeaveAsync(Dialog, CancellationToken.None);

    public abstract void Dismiss();
}
