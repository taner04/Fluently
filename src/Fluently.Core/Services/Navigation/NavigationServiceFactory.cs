using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed class NavigationServiceFactory(IServiceProvider serviceProvider) : INavigationServiceFactory
{
    public INavigationService Create() => ActivatorUtilities.CreateInstance<NavigationService>(serviceProvider);
}
