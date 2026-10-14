using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

internal static class Services
{
    internal static ServiceProvider Create() =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddFluently()
            .AddSingleton<HomeViewModel>()
            .AddSingleton<DetailsViewModel>()
            .AddScoped<Greeting>()
            .AddTransient<GreetingViewModel>()
            .AddSingleton<Clock>()
            .BuildServiceProvider();
}
