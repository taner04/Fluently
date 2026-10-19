namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class BrandedViewProvider(ViewProvider defaults) : IViewProvider
{
    public bool CanProvide(object viewModel) => viewModel is Toast || defaults.CanProvide(viewModel);

    public IViewFor<object> GetView(object viewModel) =>
        viewModel is Toast toast ? new BrandedToastView(toast) : defaults.GetView(viewModel);
}
