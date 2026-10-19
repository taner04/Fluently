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
            Scopes.Track(page, scope.ServiceProvider);
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

    private Task ReleaseAsync(NavigationEntry entry) =>
        Scopes.ReleaseAsync(entry.Page, entry.Scope,
            exception => LogPageDisposeFailed(entry.Page.GetType(), exception));

    private void Release(NavigationEntry entry) =>
        Scopes.Release(entry.Page, entry.Scope, exception => LogPageDisposeFailed(entry.Page.GetType(), exception));

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
