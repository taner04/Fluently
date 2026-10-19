using Avalonia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class SeveralContainersTests : TestBase, IDisposable
{
    private readonly ServiceProvider _first = Services.Create();
    private readonly ServiceProvider _second = Services.Create();

    public void Dispose()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [Fact]
    public Task ToastHost_Should_CreateViewsFromItsOwnContainer() =>
        UiThread.Run(() =>
        {
            var firstHost = new ToastHost { Toasts = _first.GetRequiredService<IToastService>() };
            var secondHost = new ToastHost { Toasts = _second.GetRequiredService<IToastService>() };
            TestWindow.Show(firstHost);
            TestWindow.Show(secondHost);

            firstHost.Toasts!.Show(new ClockToastViewModel());
            secondHost.Toasts!.Show(new ClockToastViewModel());
            Dispatcher.UIThread.RunJobs();

            Single<ClockToastView>(firstHost).Clock.Should().BeSameAs(_first.GetRequiredService<Clock>());
            Single<ClockToastView>(secondHost).Clock.Should().BeSameAs(_second.GetRequiredService<Clock>());
        });

    [Fact]
    public Task NavigationHost_Should_CreateViewsFromItsOwnContainer() =>
        UiThread.RunAsync(async () =>
        {
            var firstNavigation = _first.GetRequiredService<INavigationService>();
            var secondNavigation = _second.GetRequiredService<INavigationService>();
            var firstHost = new NavigationHost { Navigation = firstNavigation };
            var secondHost = new NavigationHost { Navigation = secondNavigation };
            TestWindow.Show(firstHost);
            TestWindow.Show(secondHost);

            await firstNavigation.NavigateToAsync<GreetingViewModel>(CurrentCancellationToken);
            await secondNavigation.NavigateToAsync<GreetingViewModel>(CurrentCancellationToken);

            var firstPage = (GreetingViewModel)firstNavigation.CurrentPage!;
            var secondPage = (GreetingViewModel)secondNavigation.CurrentPage!;
            firstHost.Content.Should().BeOfType<GreetingView>().Which.Greeting.Should().BeSameAs(firstPage.Greeting);
            secondHost.Content.Should().BeOfType<GreetingView>().Which.Greeting.Should().BeSameAs(secondPage.Greeting);
            firstPage.Greeting.Should().NotBeSameAs(secondPage.Greeting);
        });

    [Fact]
    public Task NavigationHost_Should_CreateNestedViewsFromItsOwnContainer() =>
        UiThread.RunAsync(async () =>
        {
            var firstNavigation = _first.GetRequiredService<INavigationService>();
            var secondNavigation = _second.GetRequiredService<INavigationService>();
            var firstHost = new NavigationHost { Navigation = firstNavigation };
            var secondHost = new NavigationHost { Navigation = secondNavigation };
            TestWindow.Show(firstHost);
            TestWindow.Show(secondHost);

            await firstNavigation.NavigateToAsync<ParentViewModel>(CurrentCancellationToken);
            await secondNavigation.NavigateToAsync<ParentViewModel>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            Single<ClockView>(firstHost).Clock.Should().BeSameAs(_first.GetRequiredService<Clock>());
            Single<ClockView>(secondHost).Clock.Should().BeSameAs(_second.GetRequiredService<Clock>());
        });

    private static T Single<T>(Visual host) => host.GetVisualDescendants().OfType<T>().Single();
}
