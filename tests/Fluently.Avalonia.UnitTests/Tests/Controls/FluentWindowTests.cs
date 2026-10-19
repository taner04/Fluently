using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class FluentWindowTests : TestBase, IDisposable
{
    private static readonly Point Inside = new(50, 50);

    private readonly INavigationService _navigation;
    private readonly ServiceProvider _serviceProvider = Services.Create();

    public FluentWindowTests()
    {
        _navigation = _serviceProvider.GetRequiredService<INavigationService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task FluentWindow_Should_GoBackAndForward_When_MouseSideButtonsAreReleased() =>
        UiThread.RunAsync(async () =>
        {
            await NavigateHomeThenDetailsAsync();
            var window = Show(new Button { Content = "Covers the window" });

            Click(window, MouseButton.XButton1);
            await WaitUntilAsync(() => _navigation.CurrentPage is HomeViewModel);
            _navigation.CurrentPage.Should().BeOfType<HomeViewModel>();

            Click(window, MouseButton.XButton2);
            await WaitUntilAsync(() => _navigation.CurrentPage is DetailsViewModel);
            _navigation.CurrentPage.Should().BeOfType<DetailsViewModel>();
        });

    [Fact]
    public Task FluentWindow_Should_IgnoreMouseSideButtons_When_Disabled() =>
        UiThread.RunAsync(async () =>
        {
            await NavigateHomeThenDetailsAsync();
            var window = Show(new Border());
            window.IsMouseNavigationEnabled = false;

            Click(window, MouseButton.XButton1);
            await Task.Delay(50, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            _navigation.CurrentPage.Should().BeOfType<DetailsViewModel>();
        });

    [Fact]
    public Task FluentWindow_Should_IgnoreMouseSideButtons_When_DialogIsOpen() =>
        UiThread.RunAsync(async () =>
        {
            await NavigateHomeThenDetailsAsync();
            var dialogs = _serviceProvider.GetRequiredService<IDialogService>();
            var window = Show(new DialogHost { Dialogs = dialogs, Content = new Border() });
            var showing = dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            await WaitUntilAsync(() => dialogs.CurrentDialog is not null);

            Click(window, MouseButton.XButton1);
            await Task.Delay(50, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            _navigation.CurrentPage.Should().BeOfType<DetailsViewModel>();
            await dialogs.DismissAsync(dialogs.CurrentDialog!);
            (await showing).IsCompleted.Should().BeFalse();
        });

    [Fact]
    public Task FluentWindow_Should_GiveItsNavigationToTitleBarAndPane() =>
        UiThread.RunAsync(async () =>
        {
            await NavigateHomeThenDetailsAsync();
            var titleBar = new TitleBar();
            var pane = new NavigationPane();
            Show(new StackPanel { Children = { titleBar, pane } });

            titleBar.Navigation.Should().BeSameAs(_navigation);
            pane.Navigation.Should().BeSameAs(_navigation);
            titleBar.GetLogicalDescendants().OfType<Button>()
                .Single(static button => button.Classes.Contains("title-bar-back")).IsEnabled.Should().BeTrue();
        });

    [Fact]
    public Task FluentWindow_Should_LetPaneKeepItsOwnNavigation() =>
        UiThread.Run(() =>
        {
            var nested = _serviceProvider.GetRequiredService<INavigationServiceFactory>().Create();
            var pane = new NavigationPane { Navigation = nested };

            Show(pane);

            pane.Navigation.Should().BeSameAs(nested);
        });

    [Fact]
    public Task FluentWindow_Should_ExtendContentIntoTitleBar() =>
        UiThread.Run(() =>
        {
            var window = new TestFluentWindow();

            window.ExtendClientAreaToDecorationsHint.Should().BeTrue();
            window.ExtendClientAreaTitleBarHeightHint.Should().Be(48);
        });

    [Fact]
    public Task FluentWindow_Should_UpdateTitleBar_When_PropertiesChange() =>
        UiThread.Run(() =>
        {
            var window = new TestFluentWindow { TitleBarHeight = 40 };

            window.ExtendsContentIntoTitleBar = false;

            window.ExtendClientAreaToDecorationsHint.Should().BeFalse();
            window.ExtendClientAreaTitleBarHeightHint.Should().Be(40);
        });

    [Fact]
    public Task FluentWindow_Should_RequestMicaOnlyOnWindows11() =>
        UiThread.Run(() =>
        {
            var window = new TestFluentWindow();

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                window.TransparencyLevelHint.Should().Equal(WindowTransparencyLevel.Mica);
            }
            else
            {
                window.TransparencyLevelHint.Should().BeEmpty();
            }

            window.IsMicaEnabled = false;
            window.TransparencyLevelHint.Should().BeEmpty();
        });

    private TestFluentWindow Show(Control content)
    {
        var window = new TestFluentWindow { Navigation = _navigation, Content = content, Width = 400, Height = 300 };
        TestWindow.Track(window);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private async Task NavigateHomeThenDetailsAsync()
    {
        await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
        await _navigation.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);
    }

    private static void Click(Window window, MouseButton button)
    {
        window.MouseDown(Inside, button);
        window.MouseUp(Inside, button);
    }
}
