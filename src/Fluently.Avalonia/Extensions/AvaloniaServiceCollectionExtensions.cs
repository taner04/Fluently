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
        /// Registers the view locator with the views added in <paramref name="configureViews"/>.
        /// Usually you call the generated <c>AddFluentlyViews()</c> instead, which registers every
        /// <see cref="IViewFor{TViewModel}"/> view of your assembly for you.
        /// Call <c>UseFluently</c> on the application afterwards to activate the view locator.
        /// </summary>
        public IServiceCollection AddFluentlyAvalonia(Action<ViewRegistry> configureViews)
        {
            ArgumentNullException.ThrowIfNull(configureViews);

            var viewRegistry = new ViewRegistry();
            configureViews(viewRegistry);
            services.TryAddSingleton(viewRegistry);
            services.TryAddSingleton<ViewLocator>();
            return services;
        }
    }
}
