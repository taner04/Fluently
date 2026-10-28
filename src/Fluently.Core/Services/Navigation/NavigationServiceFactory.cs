using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed class NavigationServiceFactory(IServiceProvider serviceProvider) : INavigationServiceFactory
{
    /// <inheritdoc />
    public INavigationService Create() => ActivatorUtilities.CreateInstance<NavigationService>(serviceProvider);
}
