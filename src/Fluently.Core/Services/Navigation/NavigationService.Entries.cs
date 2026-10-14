using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed partial class NavigationService
{
    private NavigationEntry CreateEntry(Type viewModelType, NavigationParameter? parameter)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var page = (INavigableViewModel)scope.ServiceProvider.GetRequiredService(viewModelType);
            ViewModelServices.Track(page, scope.ServiceProvider);
            return new NavigationEntry(page, parameter, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private async Task ReleaseAllAsync(Stack<NavigationEntry> entries)
    {
        while (entries.TryPop(out var entry))
        {
            await ReleaseAsync(entry);
        }
    }

    private async Task ReleaseAsync(NavigationEntry entry)
    {
        ViewModelServices.Untrack(entry.Page, entry.Scope.ServiceProvider);
        try
        {
            await entry.Scope.DisposeAsync();
        }
        catch (Exception exception)
        {
            LogPageDisposeFailed(entry.Page.GetType(), exception);
        }
    }

    // Only for Dispose(): a service that can only be disposed asynchronously makes this throw, which is logged.
    private void Release(NavigationEntry entry)
    {
        ViewModelServices.Untrack(entry.Page, entry.Scope.ServiceProvider);
        try
        {
            entry.Scope.Dispose();
        }
        catch (Exception exception)
        {
            LogPageDisposeFailed(entry.Page.GetType(), exception);
        }
    }

    private List<NavigationEntry> TakeAllEntries()
    {
        var entries = new List<NavigationEntry>();
        if (_current is not null)
        {
            entries.Add(_current);
            _current = null;
        }

        entries.AddRange(_backStack);
        entries.AddRange(_forwardStack);
        _backStack.Clear();
        _forwardStack.Clear();
        return entries;
    }
}
