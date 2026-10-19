namespace Fluently;

/// <summary>
/// A view kept for a view model, with the scope it was created from and the provider that created it, so it is only
/// reused while both are still the same.
/// </summary>
internal sealed record CachedView(IServiceProvider? Scope, object Provider, object View);
