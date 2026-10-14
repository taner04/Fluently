using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fluently.Core.UnitTests.Infrastructure;

namespace Fluently.Core.UnitTests.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests : TestBase
{
    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
        .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
        .AddTransient<FirstPage>()
        .AddFluently()
        .BuildServiceProvider();

    [Fact]
    public async Task AddFluently_Should_RegisterNavigationService_When_PagesAreRegistered()
    {
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        var navigated = await navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeTrue();
        navigationService.Should().BeSameAs(_serviceProvider.GetRequiredService<INavigationService>());
    }

    [Fact]
    public void Create_Should_ReturnIndependentNavigationService()
    {
        var factory = _serviceProvider.GetRequiredService<INavigationServiceFactory>();

        var navigationService = factory.Create();

        navigationService.Should().NotBeSameAs(_serviceProvider.GetRequiredService<INavigationService>());
        navigationService.Should().NotBeSameAs(factory.Create());
    }
}
