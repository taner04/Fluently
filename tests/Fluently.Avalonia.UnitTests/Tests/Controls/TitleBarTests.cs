using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class TitleBarTests : TestBase, IDisposable
{
    private readonly ServiceProvider _serviceProvider = Services.Create();

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task TitleBar_Should_HideBackButton_When_ItHasNoNavigation() =>
        UiThread.Run(() =>
        {
            var titleBar = new TitleBar();

            TestWindow.Show(titleBar);

            BackButton(titleBar).IsVisible.Should().BeFalse();
        });

    [Fact]
    public Task TitleBar_Should_GoBack_When_BackButtonIsClicked() =>
        UiThread.RunAsync(async () =>
        {
            var navigation = _serviceProvider.GetRequiredService<INavigationService>();
            var titleBar = new TitleBar { Navigation = navigation };
            TestWindow.Show(titleBar);
            var back = BackButton(titleBar);
            back.IsVisible.Should().BeTrue();
            back.IsEnabled.Should().BeFalse();

            await navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            await navigation.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);
            back.IsEnabled.Should().BeTrue();

            back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => navigation.CurrentPage is HomeViewModel);

            navigation.CurrentPage.Should().BeOfType<HomeViewModel>();
            back.IsEnabled.Should().BeFalse();
        });

    [Fact]
    public Task TitleBar_Should_ShowWindowTitle_When_NoTitleIsSet() =>
        UiThread.Run(() =>
        {
            var titleBar = new TitleBar();
            var window = TestWindow.Show(titleBar);

            window.Title = "Gallery";

            Title(titleBar).Text.Should().Be("Gallery");
        });

    [Fact]
    public Task TitleBar_Should_PreferOwnTitle() =>
        UiThread.Run(() =>
        {
            var titleBar = new TitleBar { Title = "Fluently" };
            var window = TestWindow.Show(titleBar);

            window.Title = "Gallery";

            Title(titleBar).Text.Should().Be("Fluently");
        });

    [Fact]
    public Task TitleBar_Should_KeepVisibilitySetByApp() =>
        UiThread.Run(() =>
        {
            var titleBar = new TitleBar { IsVisible = false };

            TestWindow.Show(titleBar);

            titleBar.IsVisible.Should().BeFalse();
        });

    [Fact]
    public Task TitleBar_Should_ShowContent() =>
        UiThread.Run(() =>
        {
            var content = new Button();
            var titleBar = new TitleBar { Content = content };

            TestWindow.Show(titleBar);

            titleBar.GetLogicalDescendants().Should().Contain(content);
        });

    private static Button BackButton(TitleBar titleBar) =>
        titleBar.GetLogicalDescendants().OfType<Button>().Single(button => button.Classes.Contains("title-bar-back"));

    private static TextBlock Title(TitleBar titleBar) =>
        titleBar.GetLogicalDescendants().OfType<TextBlock>().Single(text => text.Classes.Contains("title-bar-title"));
}
