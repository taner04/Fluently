using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
/// Activates Fluently in an Avalonia application.
/// </summary>
public static class ApplicationExtensions
{
    extension(Application application)
    {
        /// <summary>
        /// Adds the view locator registered with <c>AddFluentlyAvalonia</c> to the application's data templates,
        /// so view models shown in a <see cref="NavigationHost"/> or any content control are displayed as their views.
        /// </summary>
        public void UseFluently(IServiceProvider serviceProvider) =>
            application.DataTemplates.Add(serviceProvider.GetRequiredService<ViewLocator>());
    }
}
