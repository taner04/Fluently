using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fluently;

/// <summary>
/// Registers Fluently's Avalonia integration in a service collection.
/// </summary>
public static class AvaloniaServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the view locator. Every view in <paramref name="viewAssemblies"/> that implements
        /// <see cref="IViewFor{TViewModel}"/> becomes the view of its view model.
        /// Call <c>UseFluently</c> on the application afterwards to activate it.
        /// </summary>
        public IServiceCollection AddFluentlyAvalonia(params Assembly[] viewAssemblies)
        {
            if (viewAssemblies.Length == 0)
            {
                throw new ArgumentException("Pass at least one assembly that contains views.", nameof(viewAssemblies));
            }

            services.TryAddSingleton(new ViewRegistry(viewAssemblies));
            services.TryAddSingleton<ViewLocator>();
            return services;
        }
    }
}
