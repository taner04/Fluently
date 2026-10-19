using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class NavigationPaneTests : TestBase, IDisposable
{
    private readonly INavigationService _navigation;
    private readonly ServiceProvider _serviceProvider = Services.Create();
    private NavigationPaneItem _details = null!;
    private NavigationPaneItem _guarded = null!;
    private NavigationPaneItem _home = null!;
    private NavigationPaneItem _settings = null!;

    public NavigationPaneTests()
    {
        _navigation = _serviceProvider.GetRequiredService<INavigationService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task NavigationPane_Should_Navigate_When_ItemIsSelected() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();

            ItemList(pane).SelectedItem = _details;
            await WaitUntilAsync(() => _navigation.CurrentPage is DetailsViewModel);

            _navigation.CurrentPage.Should().BeOfType<DetailsViewModel>();
            pane.SelectedItem.Should().BeSameAs(_details);
        });

    [Fact]
    public Task NavigationPane_Should_SelectItemOfCurrentPage_When_NavigatedElsewhere() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();

            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            ItemList(pane).SelectedItem.Should().BeSameAs(_home);

            await _navigation.NavigateToAsync<GreetingViewModel>(CurrentCancellationToken);
            ItemList(pane).SelectedItem.Should().BeNull();
            FooterList(pane).SelectedItem.Should().BeSameAs(_settings);
            pane.SelectedItem.Should().BeSameAs(_settings);

            await _navigation.GoBackAsync(CurrentCancellationToken);
            ItemList(pane).SelectedItem.Should().BeSameAs(_home);
            FooterList(pane).SelectedItem.Should().BeNull();
        });

    [Fact]
    public Task NavigationPane_Should_SelectCurrentItemAgain_When_GuardRefuses() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            await _navigation.NavigateToAsync<GuardedPageViewModel>(CurrentCancellationToken);

            ItemList(pane).SelectedItem = _home;
            await WaitUntilAsync(() => ItemList(pane).SelectedItem == _guarded);

            _navigation.CurrentPage.Should().BeOfType<GuardedPageViewModel>();
            ItemList(pane).SelectedItem.Should().BeSameAs(_guarded);
        });

    [Fact]
    public Task NavigationPane_Should_KeepSelection_When_PageHasNoItem() =>
        UiThread.RunAsync(async () =>
        {
            CreateItems();
            var pane = new NavigationPane { Navigation = _navigation, Items = { _home } };
            TestWindow.Show(pane);
            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);

            await _navigation.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);

            ItemList(pane).SelectedItem.Should().BeSameAs(_home);
        });

    [Fact]
    public Task NavigationPane_Should_EnableBackButton_When_NavigationCanGoBack() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            var back = PaneButton(pane, "navigation-back");
            back.IsEnabled.Should().BeFalse();

            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            await _navigation.NavigateToAsync<DetailsViewModel>(CurrentCancellationToken);
            back.IsEnabled.Should().BeTrue();

            back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => _navigation.CurrentPage is HomeViewModel);

            _navigation.CurrentPage.Should().BeOfType<HomeViewModel>();
            back.IsEnabled.Should().BeFalse();
        });

    [Fact]
    public Task NavigationPane_Should_CollapseAndHideSearch_When_ToggleIsClicked() =>
        UiThread.Run(() =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            var search = Search(pane);
            search.IsVisible.Should().BeTrue();

            PaneButton(pane, "navigation-toggle").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            pane.IsPaneOpen.Should().BeFalse();
            search.IsVisible.Should().BeFalse();
        });

    [Fact]
    public Task NavigationPane_Should_NotNavigate_When_SearchOnlySelectsResult() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            var search = Search(pane);

            search.Text = "Settings";
            search.SelectedItem = _settings;
            await Task.Delay(50, CurrentCancellationToken);

            _navigation.CurrentPage.Should().BeOfType<HomeViewModel>();
        });

    [Fact]
    public Task NavigationPane_Should_NavigateToFirstMatch_When_EnterIsPressed() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            var search = Search(pane);
            search.Text = "sett";

            search.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await WaitUntilAsync(() => _navigation.CurrentPage is GreetingViewModel && search.Text == "");

            _navigation.CurrentPage.Should().BeOfType<GreetingViewModel>();
            FooterList(pane).SelectedItem.Should().BeSameAs(_settings);
            search.Text.Should().BeEmpty();
        });

    [Fact]
    public Task NavigationPane_Should_Navigate_When_SearchResultIsClicked() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            var search = Search(pane);
            Dispatcher.UIThread.RunJobs();
            search.GetVisualDescendants().OfType<TextBox>().Single().Text = "Det";
            await WaitUntilAsync(() => search.IsDropDownOpen);
            var result = search.GetVisualDescendants().OfType<Popup>().Single().Child!
                .GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Details");

            result.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
            await WaitUntilAsync(() => _navigation.CurrentPage is DetailsViewModel && search.Text == "");

            _navigation.CurrentPage.Should().BeOfType<DetailsViewModel>();
            ItemList(pane).SelectedItem.Should().BeSameAs(_details);
        });

    [Fact]
    public Task NavigationPane_Should_KeepSuggesting_When_TypingOn() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            var search = Search(pane);
            Dispatcher.UIThread.RunJobs();
            var text = search.GetVisualDescendants().OfType<TextBox>().Single();

            text.Text = "D";
            await WaitUntilAsync(() => search.IsDropDownOpen);
            text.Text = "De";
            await Task.Delay(50, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();
            text.Text = "Det";
            await Task.Delay(50, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            search.IsDropDownOpen.Should().BeTrue();
            search.GetVisualDescendants().OfType<Popup>().Single().Child!
                .GetVisualDescendants().OfType<TextBlock>().Select(static result => result.Text)
                .Should().Equal("Details");
        });

    [Fact]
    public Task NavigationPane_Should_KeepShowingItems_When_ItemsAreReplaced() =>
        UiThread.Run(() =>
        {
            var pane = Show();

            pane.Items.Clear();
            pane.Items.Add(_details);
            Dispatcher.UIThread.RunJobs();

            ItemList(pane).GetVisualDescendants().OfType<TextBlock>().Select(static title => title.Text)
                .Should().Equal("Details");
        });

    [Fact]
    public Task NavigationPane_Should_NotNavigate_When_EnterIsPressedWithoutMatch() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            await _navigation.NavigateToAsync<HomeViewModel>(CurrentCancellationToken);
            var search = Search(pane);
            search.Text = "nothing like this";

            search.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Task.Delay(50, CurrentCancellationToken);

            _navigation.CurrentPage.Should().BeOfType<HomeViewModel>();
            search.Text.Should().Be("nothing like this");
        });

    [Fact]
    public Task NavigationPane_Should_FocusSearch_When_ShortcutIsPressed() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            pane.IsPaneOpen = false;
            var window = (Window)TopLevel.GetTopLevel(pane)!;

            window.RaiseEvent(Shortcut(pane.SearchGesture));
            await WaitUntilAsync(() => Search(pane).IsKeyboardFocusWithin);

            pane.IsPaneOpen.Should().BeTrue();
            Search(pane).IsKeyboardFocusWithin.Should().BeTrue();
        });

    [Fact]
    public Task NavigationPane_Should_IgnoreShortcut_When_ItIsDisabled() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            pane.IsSearchShortcutEnabled = false;
            pane.IsPaneOpen = false;
            var window = (Window)TopLevel.GetTopLevel(pane)!;

            var shortcut = Shortcut(pane.SearchGesture);
            window.RaiseEvent(shortcut);
            await Task.Delay(50, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            shortcut.Handled.Should().BeFalse();
            pane.IsPaneOpen.Should().BeFalse();
            Search(pane).IsKeyboardFocusWithin.Should().BeFalse();
        });

    [Fact]
    public Task NavigationPane_Should_UseCustomSearchGesture() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            pane.SearchGesture = new KeyGesture(Key.K, KeyModifiers.Control);
            var window = (Window)TopLevel.GetTopLevel(pane)!;

            window.RaiseEvent(Shortcut(pane.SearchGesture));
            await WaitUntilAsync(() => Search(pane).IsKeyboardFocusWithin);

            Search(pane).IsKeyboardFocusWithin.Should().BeTrue();
            var setNull = () => pane.SearchGesture = null!;
            setNull.Should().Throw<ArgumentException>();
        });

    [Fact]
    public Task NavigationPane_Should_CenterSearchTextVertically() =>
        UiThread.Run(() =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            Dispatcher.UIThread.RunJobs();

            var text = Search(pane).GetVisualDescendants().OfType<TextBox>().Single();

            text.VerticalContentAlignment.Should().Be(VerticalAlignment.Center);
        });

    [Fact]
    public Task NavigationPane_Should_CenterIcons_When_Collapsed() =>
        UiThread.Run(() =>
        {
            var pane = Show();
            _home.Icon = new RectangleGeometry(new Rect(0, 0, 24, 24));
            pane.IsPaneOpen = false;
            Dispatcher.UIThread.RunJobs();

            var container = ItemList(pane).GetVisualDescendants().OfType<ListBoxItem>().First();
            var icon = container.GetVisualDescendants().OfType<PathIcon>().Single();
            var containerCenter = container.TranslatePoint(new Point(container.Bounds.Width / 2, 0), pane)!.Value.X;
            var iconCenter = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, 0), pane)!.Value.X;

            iconCenter.Should().BeApproximately(containerCenter, 0.5);
        });

    [Fact]
    public Task NavigationPane_Should_OpenAndFocusSearch_When_SearchButtonIsClicked() =>
        UiThread.RunAsync(async () =>
        {
            var pane = Show();
            pane.IsSearchVisible = true;
            pane.IsPaneOpen = false;
            var button = PaneButton(pane, "navigation-search-button");
            button.IsVisible.Should().BeTrue();

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => Search(pane).IsKeyboardFocusWithin);

            pane.IsPaneOpen.Should().BeTrue();
            button.IsVisible.Should().BeFalse();
            Search(pane).IsKeyboardFocusWithin.Should().BeTrue();
        });

    [Fact]
    public Task NavigationPane_Should_HideSearchButton_When_SearchIsNotVisible() =>
        UiThread.Run(() =>
        {
            var pane = Show();

            pane.IsPaneOpen = false;

            PaneButton(pane, "navigation-search-button").IsVisible.Should().BeFalse();
        });

    [Fact]
    public Task NavigationPane_Should_SeparateSections() =>
        UiThread.Run(() =>
        {
            var pane = Show();

            Separators(pane).Count(static separator => separator.IsVisible).Should().Be(2);

            pane.PaneContent = new Border();
            Separators(pane).Count(static separator => separator.IsVisible).Should().Be(3);
        });

    [Fact]
    public Task NavigationPane_Should_ShowContentNextToPane() =>
        UiThread.Run(() =>
        {
            var host = new NavigationHost { Navigation = _navigation };
            var pane = new NavigationPane { Navigation = _navigation, Content = host };

            TestWindow.Show(pane);

            pane.GetLogicalDescendants().Should().Contain(host);
        });

    private void CreateItems()
    {
        _home = new NavigationPaneItem { Title = "Home", Page = typeof(HomeViewModel) };
        _details = new NavigationPaneItem { Title = "Details", Page = typeof(DetailsViewModel) };
        _guarded = new NavigationPaneItem { Title = "Guarded", Page = typeof(GuardedPageViewModel) };
        _settings = new NavigationPaneItem { Title = "Settings", Page = typeof(GreetingViewModel) };
    }

    private NavigationPane Show()
    {
        CreateItems();
        var pane = new NavigationPane
        {
            Navigation = _navigation,
            Items = { _home, _details, _guarded },
            FooterItems = { _settings }
        };
        TestWindow.Show(pane);
        return pane;
    }

    private static ListBox ItemList(NavigationPane pane) =>
        pane.GetLogicalDescendants().OfType<ListBox>().Single(list => list.Classes.Contains("navigation-items"));

    private static ListBox FooterList(NavigationPane pane) =>
        pane.GetLogicalDescendants().OfType<ListBox>().Single(list => list.Classes.Contains("navigation-footer-items"));

    private static KeyEventArgs Shortcut(KeyGesture gesture) =>
        new() { RoutedEvent = InputElement.KeyDownEvent, Key = gesture.Key, KeyModifiers = gesture.KeyModifiers };

    private static List<Separator> Separators(NavigationPane pane) =>
    [
        .. pane.GetLogicalDescendants().OfType<Separator>()
            .Where(static separator => separator.Classes.Contains("navigation-separator"))
    ];

    private static AutoCompleteBox Search(NavigationPane pane) =>
        pane.GetLogicalDescendants().OfType<AutoCompleteBox>().Single();

    private static Button PaneButton(NavigationPane pane, string styleClass) =>
        pane.GetLogicalDescendants().OfType<Button>().Single(button => button.Classes.Contains(styleClass));
}
