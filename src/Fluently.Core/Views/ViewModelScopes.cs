using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Fluently;

/// <summary>
///     Remembers which service scope each page, dialog and toast of one container came from, so its view is created from
///     the same scope, and keeps one view per view model instance while that scope lives. A view model that gets a new
///     scope, or whose scope is released, gets a new view, so a view never keeps services of a disposed scope. View models
///     that Fluently did not create use the
///     container's root provider. Every container has its own instance; the most recently created one is used by a
///     <c>ViewLocator</c> outside any host.
/// </summary>
internal sealed class ViewModelScopes
{
    private static ViewModelScopes? _latest;

    private readonly ConditionalWeakTable<object, IServiceProvider> _scopes = new();
    private readonly ConditionalWeakTable<object, CachedView> _views = new();
    private IViewProvider? _viewProvider;

    public ViewModelScopes(IServiceProvider? root)
    {
        Root = root;
        if (root is not null)
        {
            Volatile.Write(ref _latest, this);
        }
    }

    public static ViewModelScopes? Latest => Volatile.Read(ref _latest);

    public IServiceProvider? Root { get; }

    public IViewProvider? ViewProvider => _viewProvider ??= Root?.GetService(typeof(IViewProvider)) as IViewProvider;

    public void Track(object viewModel, IServiceProvider scope)
    {
        if (!_scopes.TryGetValue(viewModel, out var current) || !ReferenceEquals(current, scope))
        {
            _views.Remove(viewModel);
        }

        _scopes.AddOrUpdate(viewModel, scope);
    }

    public void Untrack(object viewModel, IServiceProvider scope)
    {
        if (_scopes.TryGetValue(viewModel, out var current) && ReferenceEquals(current, scope))
        {
            _scopes.Remove(viewModel);
            _views.Remove(viewModel);
        }
    }

    /// <summary>
    ///     Stops tracking <paramref name="viewModel" /> and disposes its scope, reporting a failure to
    ///     <paramref name="onFailure" />.
    /// </summary>
    public async Task ReleaseAsync(object viewModel, AsyncServiceScope scope, Action<Exception> onFailure)
    {
        Untrack(viewModel, scope.ServiceProvider);
        try
        {
            await scope.DisposeAsync();
        }
        catch (Exception exception)
        {
            onFailure(exception);
        }
    }

    /// <summary>
    ///     Like <see cref="ReleaseAsync" />, but synchronous, for <c>Dispose()</c>: a service that can only be disposed
    ///     asynchronously makes the scope throw, which is reported to <paramref name="onFailure" />.
    /// </summary>
    public void Release(object viewModel, AsyncServiceScope scope, Action<Exception> onFailure)
    {
        Untrack(viewModel, scope.ServiceProvider);
        try
        {
            scope.Dispose();
        }
        catch (Exception exception)
        {
            onFailure(exception);
        }
    }

    public IServiceProvider? For(object viewModel) => _scopes.TryGetValue(viewModel, out var scope) ? scope : Root;

    /// <summary>
    /// The view of <paramref name="viewModel"/> created by <paramref name="provider"/>: the cached one while the view
    /// model keeps its scope, otherwise a new one from <paramref name="create"/>. A view is only cached when the scope
    /// did not change while it was created, so a view built from a scope that was just replaced is never reused.
    /// </summary>
    public object GetView(object viewModel, object provider, Func<object, object> create)
    {
        var scope = For(viewModel);
        if (_views.TryGetValue(viewModel, out var cached)
            && ReferenceEquals(cached.Scope, scope)
            && ReferenceEquals(cached.Provider, provider))
        {
            return cached.View;
        }

        var view = create(viewModel);
        if (ReferenceEquals(For(viewModel), scope))
        {
            _views.AddOrUpdate(viewModel, new CachedView(scope, provider, view));
        }

        return view;
    }
}
