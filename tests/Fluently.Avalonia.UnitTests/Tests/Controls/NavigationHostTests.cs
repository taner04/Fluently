using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class NavigationHostTests : TestBase, IDisposable
{
    private readonly INavigationService _navigation;
    private readonly ServiceProvider _serviceProvider = Services.Create();

    public NavigationHostTests()
    {
        _navigation = _serviceProvider.GetRequiredService<INavigationService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task NavigationHost_Should_ShowCurrentPage_When_Attached() =>
        UiThread.RunAsync(async () =>
        {
            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            var host = new NavigationHost { Navigation = _navigation };

            TestWindow.Show(host);

            host.Content.Should().BeOfType<HomeView>();
        });

    [Fact]
    public Task NavigationHost_Should_FollowNavigation() =>
        UiThread.RunAsync(async () =>
        {
            var host = new NavigationHost { Navigation = _navigation };
            TestWindow.Show(host);

            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            host.Content.Should().BeOfType<HomeView>();

            await _navigation.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);
            host.Content.Should().BeOfType<DetailsView>();

            await _navigation.GoBackAsync(CurrentCancellationToken);
            host.Content.Should().BeOfType<HomeView>();
        });

    [Fact]
    public Task NavigationHost_Should_StopFollowing_When_Detached() =>
        UiThread.RunAsync(async () =>
        {
            var host = new NavigationHost { Navigation = _navigation };
            var window = TestWindow.Show(host);
            window.Close();

            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);

            host.Content.Should().BeNull();
        });

    [Fact]
    public Task NavigationHost_Should_SwitchService_When_NavigationChanges() =>
        UiThread.RunAsync(async () =>
        {
            await using var other = _serviceProvider.GetRequiredService<INavigationServiceFactory>().Create();
            await other.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);
            var host = new NavigationHost { Navigation = _navigation };
            TestWindow.Show(host);

            host.Navigation = other;
            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);

            host.Content.Should().BeOfType<DetailsView>();
        });
}
