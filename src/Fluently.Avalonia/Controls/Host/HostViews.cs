namespace Fluently;

/// <summary>
///     Gives a host the view of a page, dialog or toast from the container of the service that created it. A service that
///     does not know its container, such as a test fake, gets the view model back, which the data templates then show.
/// </summary>
internal static class HostViews
{
    public static object? For(object? service, object? viewModel)
    {
        if (viewModel is null || service is not IViewSource source)
        {
            return viewModel;
        }

        var provider = ViewProviders.For(source.Scopes);
        return provider.CanProvide(viewModel)
            ? ViewProviders.Create(source.Scopes, provider, viewModel)
            : viewModel;
    }
}
