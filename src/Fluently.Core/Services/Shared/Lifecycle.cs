namespace Fluently;

/// <summary>
///     Calls the opt-in lifecycle interfaces of a page or dialog when it is entered, asked to be left, or left.
/// </summary>
internal static class Lifecycle
{
    public static Task EnterAsync(object viewModel, NavigationParameter? parameter,
        CancellationToken cancellationToken) =>
        parameter is not null
            ? parameter.EnterAsync(viewModel, cancellationToken)
            : viewModel is INavigatedTo target
                ? target.OnNavigatedToAsync(cancellationToken)
                : Task.CompletedTask;

    /// <summary>
    ///     Asks <see cref="ICanNavigateFrom" /> whether <paramref name="viewModel" /> may be left. A guard that throws
    ///     refuses,
    ///     and its exception is reported to <paramref name="onFailure" />; cancellation is passed on.
    /// </summary>
    public static async Task<bool> CanLeaveAsync(
        object viewModel,
        Action<Exception> onFailure,
        CancellationToken cancellationToken)
    {
        if (viewModel is not ICanNavigateFrom guard)
        {
            return true;
        }

        try
        {
            return await guard.CanNavigateFromAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            onFailure(exception);
            return false;
        }
    }

    public static Task LeaveAsync(object viewModel, CancellationToken cancellationToken) =>
        viewModel is INavigatedFrom target ? target.OnNavigatedFromAsync(cancellationToken) : Task.CompletedTask;
}
