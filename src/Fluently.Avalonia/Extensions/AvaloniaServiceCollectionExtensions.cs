using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fluently;

/// <summary>
///     Registers the Avalonia services of Fluently in a service collection.
/// </summary>
public static class AvaloniaServiceCollectionExtensions
{
    /// <param name="services">The service collection to register the Avalonia services in.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers the services of Fluently that need Avalonia, such as <see cref="IThemeService" />. Call it next to
        ///     <see cref="ServiceCollectionExtensions.AddFluently(IServiceCollection)" />:
        ///     <code>
        /// services.AddFluently().AddFluentlyAvalonia();
        /// </code>
        ///     Resolve these services after Avalonia has created the <see cref="Application" />, for example in
        ///     <see cref="Application.OnFrameworkInitializationCompleted" />.
        /// </summary>
        /// <returns>The same service collection, to chain further registrations.</returns>
        public IServiceCollection AddFluentlyAvalonia()
        {
            services.TryAddSingleton<IThemeService>(static _ => new ThemeService(
                Application.Current
                ?? throw new InvalidOperationException(
                    "The IThemeService was resolved before Avalonia created the application.")));
            return services;
        }
    }
}
