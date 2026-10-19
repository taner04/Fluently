using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
///     An open dialog whose result completes once, when it is closed or dismissed.
/// </summary>
internal sealed class DialogEntry<TResult>(IDialog dialog, NavigationParameter? parameter, AsyncServiceScope scope)
    : DialogEntry(dialog, parameter, scope)
{
    private readonly TaskCompletionSource<DialogResult<TResult>> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override Type ResultType => typeof(TResult);

    public Task<DialogResult<TResult>> Result => _result.Task;

    public void Complete(TResult result) => _result.TrySetResult(DialogResult<TResult>.Completed(result));

    public override void Dismiss() => _result.TrySetResult(DialogResult<TResult>.Dismissed);
}
