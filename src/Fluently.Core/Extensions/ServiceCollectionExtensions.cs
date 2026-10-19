using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fluently;

/// <summary>
///     Registers Fluently in a service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection to register Fluently in.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Registers a singleton <see cref="INavigationService" />, an <see cref="INavigationServiceFactory" />,
        ///     a singleton <see cref="IDialogService" />, a singleton <see cref="IToastService" />, the default
        ///     <see cref="IViewProvider" /> and, unless one is registered, <see cref="TimeProvider.System" />, which times
        ///     the toasts.
        ///     Register your pages and dialogs separately: transient ones are created every time they are shown,
        ///     singleton ones keep their state. Requires logging to be registered.
        /// </summary>
        /// <returns>The same service collection, to chain further registrations.</returns>
        public IServiceCollection AddFluently()
        {
            services.TryAddSingleton(new ToastOptions());
            services.TryAddSingleton(static provider => new ViewModelScopes(provider));
            services.TryAddSingleton(static provider =>
                new ViewProvider(provider.GetRequiredService<ViewModelScopes>()));
            services.TryAddSingleton<IViewProvider>(static provider => provider.GetRequiredService<ViewProvider>());
            services.TryAddSingleton<INavigationService, NavigationService>();
            services.TryAddSingleton<INavigationServiceFactory, NavigationServiceFactory>();
            services.TryAddSingleton<IDialogService, DialogService>();
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<IToastService, ToastService>();
            return services;
        }

        /// <summary>
        ///     Registers Fluently like <see cref="ServiceCollectionExtensions.AddFluently(IServiceCollection)" /> and configures
        ///     it:
        ///     <code>
        /// services.AddFluently(options =>
        /// {
        ///     options.Toasts.Position = ToastPosition.BottomRight;
        ///     options.Toasts.Duration = TimeSpan.FromSeconds(8);
        /// });
        /// </code>
        /// </summary>
        /// <param name="configure">Sets the options, such as <see cref="FluentlyOptions.Toasts" />.</param>
        /// <returns>The same service collection, to chain further registrations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A configured value is not allowed; the exception names it.</exception>
        public IServiceCollection AddFluently(Action<FluentlyOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var options = new FluentlyOptions();
            configure(options);
            options.Toasts.Validate();

            services.Replace(ServiceDescriptor.Singleton(options.Toasts));
            return services.AddFluently();
        }
    }
}
