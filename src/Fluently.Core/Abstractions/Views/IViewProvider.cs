namespace Fluently;

/// <summary>
///     Creates the views of view models. <c>AddFluently()</c> registers <see cref="ViewProvider" />, which creates the
///     views the source generator registered; register your own implementation to decide how views are created, and inject
///     <see cref="ViewProvider" /> into it to fall back to the default:
///     <code>
/// services.AddSingleton&lt;IViewProvider, MyViewProvider&gt;();
/// </code>
/// </summary>
public interface IViewProvider
{
    /// <summary>
    ///     Whether this provider has a view for <paramref name="viewModel" />.
    /// </summary>
    /// <param name="viewModel">The view model to show.</param>
    /// <returns><see langword="true" /> when <see cref="GetView" /> can create a view for it.</returns>
    bool CanProvide(object viewModel);

    /// <summary>
    ///     Creates the view of <paramref name="viewModel" />. Fluently calls it once per view model instance and reuses the
    ///     view, and calls it again when the view model is shown with a new service scope.
    /// </summary>
    /// <param name="viewModel">The view model to show.</param>
    /// <returns>The view, with <paramref name="viewModel" /> as its <see cref="IViewFor{TViewModel}.ViewModel" />.</returns>
    /// <exception cref="InvalidOperationException">There is no view for <paramref name="viewModel" />.</exception>
    IViewFor<object> GetView(object viewModel);
}
