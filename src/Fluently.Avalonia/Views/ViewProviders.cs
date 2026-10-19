using Avalonia.Controls;

namespace Fluently;

/// <summary>
///     Creates Avalonia views with the <see cref="IViewProvider" /> of a container, and keeps one view per view model
///     instance in that container.
/// </summary>
internal static class ViewProviders
{
    private static readonly ViewModelScopes NoContainer = new(null);
    private static readonly IViewProvider Fallback = new ViewProvider(NoContainer);

    public static IViewProvider For(ViewModelScopes? scopes) => scopes?.ViewProvider ?? Fallback;

    public static Control Create(ViewModelScopes? scopes, IViewProvider provider, object viewModel) =>
        (Control)(scopes ?? NoContainer).GetView(viewModel, provider, model => ToControl(provider.GetView(model), model));

    private static Control ToControl(IViewFor<object> view, object viewModel) =>
        view as Control
        ?? throw new InvalidOperationException(
            $"The view {view.GetType().Name} of {viewModel.GetType().Name} is not an Avalonia control.");
}
