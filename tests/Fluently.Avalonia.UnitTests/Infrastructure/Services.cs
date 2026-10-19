using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

internal static class Services
{
    internal static ServiceProvider Create(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection().AddLogging();
        configure?.Invoke(services);
        return services
            .AddFluently()
            .AddScoped<Greeting>()
            .AddSingleton<Clock>()
            .AddSingleton<GuardedDialogViewModel>()
            .AddViewModels()
            .BuildServiceProvider();
    }
}
