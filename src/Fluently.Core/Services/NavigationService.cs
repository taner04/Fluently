using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed partial class NavigationService(
    IServiceScopeFactory scopeFactory,
    ILogger<NavigationService> logger) : INavigationService
{
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private NavigationEntry? _current;
    private bool _disposed;

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public INavigableViewModel? CurrentPage => _current?.Page;

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public Task<bool> NavigateToAsync(Type viewModelType, CancellationToken cancellationToken = default) =>
        NavigateCoreAsync(viewModelType, null, cancellationToken);

    public Task<bool> NavigateToAsync(
        Type viewModelType,
        object parameter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return NavigateCoreAsync(viewModelType, parameter, cancellationToken);
    }

    public Task<bool> GoBackAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_backStack, _forwardStack, cancellationToken);

    public Task<bool> GoForwardAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_forwardStack, _backStack, cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_current is not null)
        {
            Release(_current);
            _current = null;
        }

        ReleaseAll(_backStack);
        ReleaseAll(_forwardStack);
    }

    private async Task<bool> NavigateCoreAsync(
        Type viewModelType,
        object? parameter,
        CancellationToken cancellationToken)
    {
        viewModelType.ThrowIfNotAssignableTo<INavigableViewModel>();

        NavigationEntry target;
        await _transitionLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var previous = _current;
            var isSamePage = previous?.Page.GetType() == viewModelType;
            if (isSamePage && (parameter is null || Equals(previous?.Parameter, parameter)))
            {
                return false;
            }

            if (previous is not null && !await CanLeaveAsync(previous, cancellationToken))
            {
                return false;
            }

            target = CreateEntry(viewModelType, parameter);
            var committed = false;
            try
            {
                if (!await TransitionAsync(previous, target, cancellationToken))
                {
                    return false;
                }

                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    Release(target);
                }
            }

            if (previous is not null)
            {
                if (isSamePage)
                {
                    Release(previous);
                }
                else
                {
                    _backStack.Push(previous);
                }
            }

            ReleaseAll(_forwardStack);
            _current = target;
        }
        finally
        {
            _transitionLock.Release();
        }

        Navigated?.Invoke(this, new NavigatedEventArgs(target.Page));
        return true;
    }

    private async Task<bool> NavigateHistoryAsync(
        Stack<NavigationEntry> source,
        Stack<NavigationEntry> destination,
        CancellationToken cancellationToken)
    {
        NavigationEntry? target;
        await _transitionLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!source.TryPeek(out target))
            {
                return false;
            }

            var previous = _current;
            if (previous is not null && !await CanLeaveAsync(previous, cancellationToken))
            {
                return false;
            }

            if (!await TransitionAsync(previous, target, cancellationToken))
            {
                return false;
            }

            source.Pop();
            if (previous is not null)
            {
                destination.Push(previous);
            }

            _current = target;
        }
        finally
        {
            _transitionLock.Release();
        }

        Navigated?.Invoke(this, new NavigatedEventArgs(target.Page));
        return true;
    }

    private NavigationEntry CreateEntry(Type viewModelType, object? parameter)
    {
        var scope = scopeFactory.CreateScope();
        try
        {
            var page = (INavigableViewModel)scope.ServiceProvider.GetRequiredService(viewModelType);
            return new NavigationEntry(page, parameter, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private async Task<bool> CanLeaveAsync(NavigationEntry current, CancellationToken cancellationToken)
    {
        try
        {
            return await current.CanLeaveAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogNavigationGuardFailed(current.Page.GetType(), exception);
            return false;
        }
    }

    private async Task<bool> TransitionAsync(
        NavigationEntry? previous,
        NavigationEntry target,
        CancellationToken cancellationToken)
    {
        var targetEntered = false;

        try
        {
            if (previous is not null)
            {
                await previous.LeaveAsync(cancellationToken);
            }

            targetEntered = true;
            await target.EnterAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(previous, target, targetEntered);
            throw;
        }
        catch (Exception exception)
        {
            LogNavigationFailed(target.Page.GetType(), exception);
            await RollbackAsync(previous, target, targetEntered);
            return false;
        }
    }

    private async Task RollbackAsync(NavigationEntry? previous, NavigationEntry target, bool targetEntered)
    {
        if (targetEntered)
        {
            try
            {
                await target.LeaveAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                LogNavigationCleanupFailed(target.Page.GetType(), exception);
            }
        }

        if (previous is null)
        {
            return;
        }

        try
        {
            await previous.EnterAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            LogNavigationRestoreFailed(previous.Page.GetType(), exception);
        }
    }

    private void ReleaseAll(Stack<NavigationEntry> entries)
    {
        while (entries.TryPop(out var entry))
        {
            Release(entry);
        }
    }

    private void Release(NavigationEntry entry)
    {
        try
        {
            entry.Scope.Dispose();
        }
        catch (Exception exception)
        {
            LogPageDisposeFailed(entry.Page.GetType(), exception);
        }
    }

    [LoggerMessage(LogLevel.Error, "Navigation to {ViewModelType} failed.")]
    private partial void LogNavigationFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Asking {ViewModelType} whether it can be left failed.")]
    private partial void LogNavigationGuardFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Leaving {ViewModelType} after failed navigation failed.")]
    private partial void LogNavigationCleanupFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Restoring {ViewModelType} after failed navigation failed.")]
    private partial void LogNavigationRestoreFailed(Type viewModelType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Disposing {ViewModelType} after it left the history failed.")]
    private partial void LogPageDisposeFailed(Type viewModelType, Exception exception);

    private sealed record NavigationEntry(INavigableViewModel Page, object? Parameter, IServiceScope Scope)
    {
        public Task<bool> CanLeaveAsync(CancellationToken cancellationToken) =>
            Page is ICanNavigateFrom page ? page.CanNavigateFromAsync(cancellationToken) : Task.FromResult(true);

        public Task EnterAsync(CancellationToken cancellationToken) =>
            Page is INavigatedTo page ? page.OnNavigatedToAsync(Parameter, cancellationToken) : Task.CompletedTask;

        public Task LeaveAsync(CancellationToken cancellationToken) =>
            Page is INavigatedFrom page ? page.OnNavigatedFromAsync(cancellationToken) : Task.CompletedTask;
    }
}
