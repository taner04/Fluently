namespace Fluently;

internal sealed partial class NavigationService
{
    private async Task<bool> NavigateCoreAsync(
        Type viewModelType,
        NavigationParameter? parameter,
        CancellationToken cancellationToken)
    {
        NavigationEntry target;
        HistoryState before;
        await _transitionLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            before = CaptureState();

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
                    await ReleaseAsync(target);
                }
            }

            if (previous is not null)
            {
                if (isSamePage)
                {
                    await ReleaseAsync(previous);
                }
                else
                {
                    _backStack.Push(previous);
                }
            }

            await ReleaseAllAsync(_forwardStack);
            _current = target;
        }
        finally
        {
            _transitionLock.Release();
        }

        RaisePropertyChanges(before);
        Navigated?.Invoke(this, new NavigatedEventArgs(target.Page));
        return true;
    }

    private async Task<bool> NavigateHistoryAsync(
        Stack<NavigationEntry> source,
        Stack<NavigationEntry> destination,
        CancellationToken cancellationToken)
    {
        NavigationEntry? target;
        HistoryState before;
        await _transitionLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            before = CaptureState();

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

        RaisePropertyChanges(before);
        Navigated?.Invoke(this, new NavigatedEventArgs(target.Page));
        return true;
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
}
