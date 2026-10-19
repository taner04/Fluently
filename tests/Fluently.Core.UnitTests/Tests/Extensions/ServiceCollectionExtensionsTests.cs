using Fluently.Core.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Core.UnitTests.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests : TestBase
{
    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddLogging()
        .AddTransient<FirstPage>()
        .AddFluently()
        .BuildServiceProvider();

    public static TheoryData<string, Action<FluentlyOptions>> NotAllowedSettings => new()
    {
        { "Toasts.Duration", static options => options.Toasts.Duration = TimeSpan.Zero },
        { "Toasts.MaxCount", static options => options.Toasts.MaxCount = 0 },
        { "Toasts.Position", static options => options.Toasts.Position = (ToastPosition)42 }
    };

    [Fact]
    public async Task AddFluently_Should_RegisterNavigationService_When_PagesAreRegistered()
    {
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        var navigated = await navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeTrue();
        navigationService.Should().BeSameAs(_serviceProvider.GetRequiredService<INavigationService>());
    }

    [Fact]
    public void AddFluently_Should_RegisterSingletonDialogService() =>
        _serviceProvider.GetRequiredService<IDialogService>()
            .Should().BeSameAs(_serviceProvider.GetRequiredService<IDialogService>());

    [Fact]
    public void AddFluently_Should_RegisterSingletonToastService() =>
        _serviceProvider.GetRequiredService<IToastService>()
            .Should().BeSameAs(_serviceProvider.GetRequiredService<IToastService>());

    [Fact]
    public void Create_Should_ReturnIndependentNavigationService()
    {
        var factory = _serviceProvider.GetRequiredService<INavigationServiceFactory>();

        var navigationService = factory.Create();

        navigationService.Should().NotBeSameAs(_serviceProvider.GetRequiredService<INavigationService>());
        navigationService.Should().NotBeSameAs(factory.Create());
    }

    [Fact]
    public void AddFluently_Should_ConfigureToasts()
    {
        using var serviceProvider = new ServiceCollection().AddLogging()
            .AddFluently(options =>
            {
                options.Toasts.Position = ToastPosition.BottomCenter;
                options.Toasts.MaxCount = 1;
            })
            .BuildServiceProvider();
        var toasts = serviceProvider.GetRequiredService<IToastService>();

        var first = toasts.ShowInformation("First", "");
        toasts.ShowInformation("Second", "");

        toasts.Position.Should().Be(ToastPosition.BottomCenter);
        toasts.Toasts.Should().ContainSingle().Which.Should().NotBeSameAs(first);
    }

    [Fact]
    public void AddFluently_Should_KeepDefaults_When_NotConfigured()
    {
        var options = _serviceProvider.GetRequiredService<ToastOptions>();

        options.Position.Should().Be(ToastPosition.TopRight);
        options.Duration.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxCount.Should().Be(5);
    }

    [Theory]
    [MemberData(nameof(NotAllowedSettings))]
    public void AddFluently_Should_Throw_When_ConfiguredValueIsNotAllowed(string setting,
        Action<FluentlyOptions> configure)
    {
        var add = () => new ServiceCollection().AddFluently(configure);

        add.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(setting);
    }

    [Fact]
    public void AddFluently_Should_RegisterDefaultViewProvider()
    {
        _serviceProvider.GetRequiredService<IViewProvider>()
            .Should().BeSameAs(_serviceProvider.GetRequiredService<ViewProvider>());
    }

    [Fact]
    public void AddFluently_Should_KeepViewProviderRegisteredByApp()
    {
        var custom = new ViewProvider(new ViewModelScopes(null));
        using var serviceProvider = new ServiceCollection().AddLogging()
            .AddSingleton<IViewProvider>(custom)
            .AddFluently()
            .BuildServiceProvider();

        serviceProvider.GetRequiredService<IViewProvider>().Should().BeSameAs(custom);
    }
}
