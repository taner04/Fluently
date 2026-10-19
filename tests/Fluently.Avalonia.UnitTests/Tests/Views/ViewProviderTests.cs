using Avalonia.Threading;
using Avalonia.VisualTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Views;

[Collection(nameof(DefaultContainerCollection))]
public sealed class ViewProviderTests : TestBase, IDisposable
{
    private readonly ServiceProvider _serviceProvider =
        Services.Create(static services => services.AddSingleton<IViewProvider, BrandedViewProvider>());

    public ViewProviderTests()
    {
        _serviceProvider.GetRequiredService<INavigationService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task ViewLocator_Should_UseProviderRegisteredByApp() =>
        UiThread.Run(() =>
        {
            var locator = new ViewLocator();
            var toast = new Toast("Saved", "");

            locator.Match(toast).Should().BeTrue();
            locator.Build(toast).Should().BeOfType<BrandedToastView>().Which.ViewModel.Should().BeSameAs(toast);
            locator.Build(new HomeViewModel()).Should().BeOfType<HomeView>();
        });

    [Fact]
    public Task ViewLocator_Should_PreferProviderSetOnIt() =>
        UiThread.Run(() =>
        {
            using var other = new ServiceCollection().AddFluently().BuildServiceProvider();
            var locator = new ViewLocator { Provider = other.GetRequiredService<ViewProvider>() };

            locator.Match(new Toast("Saved", "")).Should().BeFalse();
        });

    [Fact]
    public Task ToastHost_Should_ShowProvidersView_ForBuiltInToast() =>
        UiThread.Run(() =>
        {
            var toasts = _serviceProvider.GetRequiredService<IToastService>();
            var host = new ToastHost { Toasts = toasts };
            TestWindow.Show(host);

            toasts.ShowSuccess("Saved", "");
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<BrandedToastView>().Should().ContainSingle();
            host.GetVisualDescendants().OfType<ToastView>().Should().BeEmpty();
        });
}
