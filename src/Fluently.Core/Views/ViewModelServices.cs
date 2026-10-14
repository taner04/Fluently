using System.Runtime.CompilerServices;

namespace Fluently;

/// <summary>
/// Remembers which service provider a view model came from, so its view is created from the same scope.
/// View models that Fluently did not create fall back to the application's root provider, which the
/// <see cref="NavigationService"/> registers when the container creates it.
/// </summary>
internal static class ViewModelServices
{
    private static readonly ConditionalWeakTable<object, IServiceProvider> Scopes = new();
    private static IServiceProvider? _root;

    internal static void SetRoot(IServiceProvider root) => Volatile.Write(ref _root, root);

    internal static void Track(object viewModel, IServiceProvider scope) => Scopes.AddOrUpdate(viewModel, scope);

    internal static void Untrack(object viewModel, IServiceProvider scope)
    {
        // A singleton page can sit in several history entries; only the entry it was last created for owns it.
        if (Scopes.TryGetValue(viewModel, out var current) && ReferenceEquals(current, scope))
        {
            Scopes.Remove(viewModel);
        }
    }

    internal static IServiceProvider? For(object viewModel) =>
        Scopes.TryGetValue(viewModel, out var scope) ? scope : Volatile.Read(ref _root);
}
