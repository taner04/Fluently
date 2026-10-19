namespace Fluently;

/// <summary>
///     The default <see cref="IViewProvider" />: creates the views registered in <see cref="ViewRegistry" />, with the
///     services
///     of the scope the view model was created from, or the application's root provider. Inject it into your own provider
///     to fall back to it.
/// </summary>
public sealed class ViewProvider : IViewProvider
{
    private readonly ViewModelScopes _scopes;

    internal ViewProvider(ViewModelScopes scopes)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    public bool CanProvide(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return ViewRegistry.Contains(viewModel.GetType());
    }

    /// <inheritdoc />
    public IViewFor<object> GetView(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        return ViewRegistry.TryCreateView(viewModel, _scopes.For(viewModel) ?? new MissingServiceProvider(viewModel),
            out var view)
            ? view
            : throw new InvalidOperationException($"No view is registered for {viewModel.GetType().Name}.");
    }
}
