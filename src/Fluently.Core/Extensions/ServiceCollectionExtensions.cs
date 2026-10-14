using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fluently;

/// <summary>
/// Registers Fluently in a service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a singleton <see cref="INavigationService"/> and an <see cref="INavigationServiceFactory"/>.
        /// Register your pages separately: transient pages are created on every navigation,
        /// singleton pages keep their state. Requires logging to be registered.
        /// </summary>
        public IServiceCollection AddFluently()
        {
            services.TryAddSingleton<INavigationService, NavigationService>();
            services.TryAddSingleton<INavigationServiceFactory, NavigationServiceFactory>();
            return services;
        }
    }
}
