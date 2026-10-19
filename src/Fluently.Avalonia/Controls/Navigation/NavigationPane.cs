using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Specialized;

namespace Fluently;

/// <summary>
///     A side pane that navigates an <see cref="INavigationService" />: selecting one of its <see cref="Items" /> or
///     <see cref="FooterItems" /> navigates to that item's page, and the item of the current page is selected, also after
///     going back or when a page's guard refuses to be left. It has a back button, a button that collapses the pane to
///     its icons, an optional search box, and room for your own <see cref="PaneContent" />. Thin lines separate the
///     buttons and search box at the top, the items, your content and the footer items. Its <see cref="Content" /> is
///     shown next to the pane, usually a <see cref="NavigationHost" />.
///     <code>
/// &lt;fluently:NavigationPane Navigation="{Binding Navigation}" IsSearchVisible="True"&gt;
///     &lt;fluently:NavigationPane.Items&gt;
///         &lt;fluently:NavigationPaneItem Title="Home" Icon="{StaticResource HomeIcon}" Page="{x:Type vm:HomeViewModel}" /&gt;
///     &lt;/fluently:NavigationPane.Items&gt;
///     &lt;fluently:NavigationHost Navigation="{Binding Navigation}" /&gt;
/// &lt;/fluently:NavigationPane&gt;
/// </code>
///     The pane is built from a <see cref="SplitView" />, <see cref="ListBox" />es, an <see cref="AutoCompleteBox" />,
///     <see cref="Button" />s and <see cref="Separator" />s, so it takes the look of your theme; restyle its parts with
///     the
///     style classes <c>navigation-back</c>, <c>navigation-toggle</c>, <c>navigation-search</c>,
///     <c>navigation-search-button</c>, <c>navigation-items</c>, <c>navigation-footer-items</c> and
///     <c>navigation-separator</c>. The pane opens and collapses in 0.3 seconds with Fluent's decelerating curve.
/// </summary>
public class NavigationPane : Control
{
    private const double ItemIconSize = 16;

    /// <summary>
    ///     Defines the <see cref="Navigation" /> property.
    /// </summary>
    public static readonly StyledProperty<INavigationService?> NavigationProperty =
        FluentWindow.NavigationProperty.AddOwner<NavigationPane>();

    /// <summary>
    ///     Defines the <see cref="Content" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<NavigationPane, Control?>(nameof(Content));

    /// <summary>
    ///     Defines the <see cref="PaneContent" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> PaneContentProperty =
        AvaloniaProperty.Register<NavigationPane, Control?>(nameof(PaneContent));

    /// <summary>
    ///     Defines the <see cref="IsPaneOpen" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsPaneOpenProperty =
        AvaloniaProperty.Register<NavigationPane, bool>(nameof(IsPaneOpen), true,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    ///     Defines the <see cref="OpenPaneLength" /> property.
    /// </summary>
    public static readonly StyledProperty<double> OpenPaneLengthProperty =
        AvaloniaProperty.Register<NavigationPane, double>(nameof(OpenPaneLength), 288);

    /// <summary>
    ///     Defines the <see cref="CompactPaneLength" /> property.
    /// </summary>
    public static readonly StyledProperty<double> CompactPaneLengthProperty =
        AvaloniaProperty.Register<NavigationPane, double>(nameof(CompactPaneLength), 48);

    /// <summary>
    ///     Defines the <see cref="IsBackButtonVisible" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsBackButtonVisibleProperty =
        AvaloniaProperty.Register<NavigationPane, bool>(nameof(IsBackButtonVisible), true);

    /// <summary>
    ///     Defines the <see cref="IsPaneToggleVisible" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsPaneToggleVisibleProperty =
        AvaloniaProperty.Register<NavigationPane, bool>(nameof(IsPaneToggleVisible), true);

    /// <summary>
    ///     Defines the <see cref="IsSearchVisible" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSearchVisibleProperty =
        AvaloniaProperty.Register<NavigationPane, bool>(nameof(IsSearchVisible));

    /// <summary>
    ///     Defines the <see cref="SearchPlaceholder" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> SearchPlaceholderProperty =
        AvaloniaProperty.Register<NavigationPane, string?>(nameof(SearchPlaceholder), "Search");

    /// <summary>
    ///     Defines the <see cref="IsSearchShortcutEnabled" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSearchShortcutEnabledProperty =
        AvaloniaProperty.Register<NavigationPane, bool>(nameof(IsSearchShortcutEnabled), true);

    /// <summary>
    ///     Defines the <see cref="SearchGesture" /> property.
    /// </summary>
    public static readonly StyledProperty<KeyGesture> SearchGestureProperty =
        AvaloniaProperty.Register<NavigationPane, KeyGesture>(
            nameof(SearchGesture),
            new KeyGesture(Key.L, OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control),
            validate: static gesture => gesture is not null);

    /// <summary>
    ///     Defines the <see cref="SelectedItem" /> property.
    /// </summary>
    public static readonly DirectProperty<NavigationPane, NavigationPaneItem?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<NavigationPane, NavigationPaneItem?>(nameof(SelectedItem),
            pane => pane.SelectedItem);

    private readonly BackButton _back = new("navigation-back");
    private readonly Separator _contentSeparator = new();
    private readonly ServiceFollower<INavigationService> _follower;
    private readonly ListBox _footerItems = new();
    private readonly ListBox _items = new();
    private readonly Decorator _paneContent = new();
    private readonly AutoCompleteBox _search = new();
    private readonly Button _searchButton = new();
    private readonly SplitView _split = new() { DisplayMode = SplitViewDisplayMode.CompactInline };
    private readonly Button _toggle = new();
    private bool _isSynchronizing;
    private TopLevel? _topLevel;

    /// <summary>
    ///     Creates a navigation pane.
    /// </summary>
    public NavigationPane()
    {
        _follower = new ServiceFollower<INavigationService>(
            navigation => navigation.Navigated += OnNavigated,
            navigation => navigation.Navigated -= OnNavigated,
            _ => Synchronize());

        Items.CollectionChanged += OnItemsChanged;
        FooterItems.CollectionChanged += OnItemsChanged;
        DataTemplates.Add(IconTemplate.Create(ItemIconSize));

        _split[!SplitView.IsPaneOpenProperty] = this[!IsPaneOpenProperty];
        _split[!SplitView.OpenPaneLengthProperty] = this[!OpenPaneLengthProperty];
        _split[!SplitView.CompactPaneLengthProperty] = this[!CompactPaneLengthProperty];
        StyleDefaults.Set(_split, SplitView.PaneBackgroundProperty, Brushes.Transparent);

        _back[!IsVisibleProperty] = this[!IsBackButtonVisibleProperty];
        StyleDefaults.Set(_back, MarginProperty, new Thickness(4, 2));
        PaneButton(_toggle, Glyphs.Menu, "Menu", "navigation-toggle", IsPaneToggleVisibleProperty);
        _toggle.Click += (_, _) => SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);
        SubtleButton.Apply(_searchButton, Glyphs.Search, "Search", "navigation-search-button");
        StyleDefaults.Set(_searchButton, MarginProperty, new Thickness(4, 2));
        _searchButton.Click += (_, _) => OpenSearch();
        _split.TemplateApplied += static (_, e) =>
        {
            if (e.NameScope.Find<Panel>("PART_PaneRoot") is { } paneRoot)
            {
                paneRoot.Transitions = PaneTransitions();
            }
        };

        _search.Classes.Add("navigation-search");
        _search.FilterMode = AutoCompleteFilterMode.Custom;
        _search.ItemFilter = static (text, item) => Matches(item, text);
        _search.ItemTemplate = new FuncDataTemplate<NavigationPaneItem?>((item, _) => SearchResultView(item));
        _search[!AutoCompleteBox.PlaceholderTextProperty] = this[!SearchPlaceholderProperty];
        _search.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
        _search.TemplateApplied += static (_, e) =>
        {
            if (e.NameScope.Find<TextBox>("PART_TextBox") is { } text)
            {
                StyleDefaults.Set(text, TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            }
        };
        StyleDefaults.Set(_search, MarginProperty, new Thickness(8, 4, 8, 8));
        UpdateSearch();

        ItemList(_items, Items, "navigation-items");
        ItemList(_footerItems, FooterItems, "navigation-footer-items");

        var buttons = new StackPanel { Children = { _back, _toggle, _searchButton } };
        var headerSeparator = SectionSeparator();
        var footerSeparator = SectionSeparator();
        _contentSeparator.Classes.Add("navigation-separator");
        StyleDefaults.Set(_contentSeparator, MarginProperty, new Thickness(8, 4));
        _contentSeparator.IsVisible = false;
        DockPanel.SetDock(buttons, Dock.Top);
        DockPanel.SetDock(_search, Dock.Top);
        DockPanel.SetDock(headerSeparator, Dock.Top);
        DockPanel.SetDock(_items, Dock.Top);
        DockPanel.SetDock(_contentSeparator, Dock.Top);
        DockPanel.SetDock(_footerItems, Dock.Bottom);
        DockPanel.SetDock(footerSeparator, Dock.Bottom);
        var pane = new DockPanel
        {
            Children =
            {
                buttons, _search, headerSeparator, _items, _contentSeparator, _footerItems, footerSeparator,
                _paneContent
            }
        };
        StyleDefaults.Set(pane, MarginProperty, new Thickness(0, 4, 0, 8));
        _split.Pane = pane;

        VisualChildren.Add(_split);
        LogicalChildren.Add(_split);
    }

    /// <summary>
    ///     The navigation service that the pane navigates and follows; the window's <see cref="FluentWindow.Navigation" />
    ///     unless set.
    /// </summary>
    public INavigationService? Navigation
    {
        get => GetValue(NavigationProperty);
        set => SetValue(NavigationProperty, value);
    }

    /// <summary>
    ///     The content shown next to the pane, usually a <see cref="NavigationHost" />.
    /// </summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>
    ///     Content of your own, shown in the pane between the <see cref="Items" /> and the <see cref="FooterItems" />, such
    ///     as a list of recent documents.
    /// </summary>
    public Control? PaneContent
    {
        get => GetValue(PaneContentProperty);
        set => SetValue(PaneContentProperty, value);
    }

    /// <summary>
    ///     The items at the top of the pane.
    /// </summary>
    public AvaloniaList<NavigationPaneItem> Items { get; } = [];

    /// <summary>
    ///     The items at the bottom of the pane, such as settings.
    /// </summary>
    public AvaloniaList<NavigationPaneItem> FooterItems { get; } = [];

    /// <summary>
    ///     The item of the current page, or of the page last shown that has an item. <see langword="null" /> before the
    ///     first navigation.
    /// </summary>
    public NavigationPaneItem? SelectedItem
    {
        get;
        private set => SetAndRaise(SelectedItemProperty, ref field, value);
    }

    /// <summary>
    ///     Whether the pane shows its titles, or only its icons. <see langword="true" /> by default.
    /// </summary>
    public bool IsPaneOpen
    {
        get => GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    /// <summary>
    ///     The width of the open pane. 288 by default.
    /// </summary>
    public double OpenPaneLength
    {
        get => GetValue(OpenPaneLengthProperty);
        set => SetValue(OpenPaneLengthProperty, value);
    }

    /// <summary>
    ///     The width of the collapsed pane, which shows only the icons. 48 by default.
    /// </summary>
    public double CompactPaneLength
    {
        get => GetValue(CompactPaneLengthProperty);
        set => SetValue(CompactPaneLengthProperty, value);
    }

    /// <summary>
    ///     Whether the pane shows a back button, enabled while the navigation service can go back.
    ///     <see langword="true" /> by default; turn it off when a <see cref="TitleBar" /> shows the back button.
    /// </summary>
    public bool IsBackButtonVisible
    {
        get => GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    /// <summary>
    ///     Whether the pane shows a button that opens and collapses it. <see langword="true" /> by default.
    /// </summary>
    public bool IsPaneToggleVisible
    {
        get => GetValue(IsPaneToggleVisibleProperty);
        set => SetValue(IsPaneToggleVisibleProperty, value);
    }

    /// <summary>
    ///     Whether the open pane shows a box that suggests items by their title, and the collapsed pane a search button
    ///     that opens the pane and puts the cursor into the box. It navigates when Enter is pressed, to the highlighted
    ///     suggestion or else the first match, or when a suggestion is clicked. <see langword="false" /> by default.
    /// </summary>
    public bool IsSearchVisible
    {
        get => GetValue(IsSearchVisibleProperty);
        set => SetValue(IsSearchVisibleProperty, value);
    }

    /// <summary>
    ///     The text shown in the empty search box. "Search" by default.
    /// </summary>
    public string? SearchPlaceholder
    {
        get => GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    /// <summary>
    ///     Whether <see cref="SearchGesture" /> moves the focus to the search box while it is visible, opening the pane
    ///     first when it is collapsed. <see langword="true" /> by default.
    /// </summary>
    public bool IsSearchShortcutEnabled
    {
        get => GetValue(IsSearchShortcutEnabledProperty);
        set => SetValue(IsSearchShortcutEnabledProperty, value);
    }

    /// <summary>
    ///     The keys that move the focus to the search box, anywhere in the window. Ctrl+L by default, Cmd+L on macOS,
    ///     as in browsers.
    /// </summary>
    public KeyGesture SearchGesture
    {
        get => GetValue(SearchGestureProperty);
        set => SetValue(SearchGestureProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _follower.Attach(Navigation);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, OnWindowKeyDown);
        _topLevel = null;
        _follower.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NavigationProperty)
        {
            _follower.Change(Navigation);
            _back.Navigation = Navigation;
        }
        else if (change.Property == ContentProperty)
        {
            _split.Content = Content;
        }
        else if (change.Property == PaneContentProperty)
        {
            if (change.GetOldValue<Control?>() is { } oldContent)
            {
                oldContent.PropertyChanged -= OnPaneContentPropertyChanged;
            }

            if (PaneContent is { } newContent)
            {
                newContent.PropertyChanged += OnPaneContentPropertyChanged;
            }

            _paneContent.Child = PaneContent;
            UpdateContentSeparator();
        }
        else if (change.Property == IsPaneOpenProperty)
        {
            UpdateSearch();
            ArrangeContainers();
        }
        else if (change.Property == IsSearchVisibleProperty)
        {
            UpdateSearch();
        }
    }

    private void PaneButton(Button button, Geometry icon, string name, string styleClass,
        StyledProperty<bool> isVisible)
    {
        SubtleButton.Apply(button, icon, name, styleClass);
        button[!IsVisibleProperty] = this[!isVisible];
        StyleDefaults.Set(button, MarginProperty, new Thickness(4, 2));
    }

    private void ItemList(ListBox list, AvaloniaList<NavigationPaneItem> items, string styleClass)
    {
        list.Classes.Add(styleClass);
        list.ItemsSource = items;
        list.ItemTemplate = new FuncDataTemplate<NavigationPaneItem?>((item, _) => ItemView(item));
        list.ContainerPrepared += (_, e) => PrepareContainer(e.Container);
        list.SelectionChanged += OnItemSelected;
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        StyleDefaults.Set(list, TemplatedControl.BackgroundProperty, Brushes.Transparent);
    }

    private Control ItemView(NavigationPaneItem? item)
    {
        if (item is null)
        {
            return new Panel();
        }

        var icon = new ContentPresenter
        {
            MinWidth = ItemIconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.Bind(ContentPresenter.ContentProperty, item.GetObservable(NavigationPaneItem.IconProperty));

        var title = new TextBlock { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        title.Bind(TextBlock.TextProperty, item.GetObservable(NavigationPaneItem.TitleProperty));
        title.Bind(IsVisibleProperty, _split.GetObservable(SplitView.IsPaneOpenProperty));
        Grid.SetColumn(title, 1);

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { icon, title }
        };
    }

    private void PrepareContainer(Control container)
    {
        if (container.DataContext is NavigationPaneItem item)
        {
            ToolTip.SetTip(container, item.Title);
        }

        StyleDefaults.Set(container, MinWidthProperty, 0d);
        StyleDefaults.Set(container, MarginProperty, new Thickness(4, 1));
        StyleDefaults.Set(container, TemplatedControl.CornerRadiusProperty, new CornerRadius(4));
        ArrangeContainer(container);
    }

    private void ArrangeContainer(Control container)
    {
        StyleDefaults.Set(container, TemplatedControl.PaddingProperty,
            IsPaneOpen ? new Thickness(12, 10) : new Thickness(0, 10));
        StyleDefaults.Set(
            container,
            ContentControl.HorizontalContentAlignmentProperty,
            IsPaneOpen ? HorizontalAlignment.Left : HorizontalAlignment.Center);
    }

    private void OnPaneContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty)
        {
            UpdateContentSeparator();
        }
    }

    private void UpdateContentSeparator() => _contentSeparator.IsVisible = PaneContent is { IsVisible: true };

    private void ArrangeContainers()
    {
        foreach (var container in _items.GetRealizedContainers().Concat(_footerItems.GetRealizedContainers()))
        {
            ArrangeContainer(container);
        }

        foreach (var button in new[] { _back, _toggle, _searchButton })
        {
            StyleDefaults.Set(
                button,
                HorizontalAlignmentProperty,
                IsPaneOpen ? HorizontalAlignment.Left : HorizontalAlignment.Center);
        }
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isSynchronizing && sender is ListBox { SelectedItem: NavigationPaneItem item })
        {
            await OpenAsync(item);
        }
    }

    private static bool Matches(object? item, string? text) =>
        item is NavigationPaneItem { Title: { } title } &&
        title.Contains(text ?? "", StringComparison.CurrentCultureIgnoreCase);

    private Control SearchResultView(NavigationPaneItem? item)
    {
        if (item is null)
        {
            return new Panel();
        }

        var title = new TextBlock { Background = Brushes.Transparent };
        title.Bind(TextBlock.TextProperty, item.GetObservable(NavigationPaneItem.TitleProperty));
        title.AddHandler(TappedEvent, (_, _) => OnSearchResultTapped(item), handledEventsToo: true);
        return title;
    }

    private async void OnSearchResultTapped(NavigationPaneItem item) => await OpenSearchResultAsync(item);

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsSearchShortcutEnabled || !IsSearchVisible || !IsEffectivelyEnabled || !SearchGesture.Matches(e))
        {
            return;
        }

        e.Handled = true;
        OpenSearch();
    }

    private void OpenSearch()
    {
        SetCurrentValue(IsPaneOpenProperty, true);
        Dispatcher.UIThread.Post(FocusSearch, DispatcherPriority.Loaded);
    }

    private static Separator SectionSeparator()
    {
        var separator = new Separator();
        separator.Classes.Add("navigation-separator");
        StyleDefaults.Set(separator, MarginProperty, new Thickness(8, 4));
        return separator;
    }

    private static Transitions PaneTransitions() =>
    [
        new DoubleTransition
        {
            Property = WidthProperty,
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new SplineEasing(0.1, 0.9, 0.2)
        }
    ];

    private void FocusSearch()
    {
        var text = _search.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (text is null)
        {
            _search.Focus(NavigationMethod.Tab);
            return;
        }

        text.Focus(NavigationMethod.Tab);
        text.SelectAll();
    }

    private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        var item = _search.IsDropDownOpen && _search.SelectedItem is NavigationPaneItem highlighted
            ? highlighted
            : Items.Concat(FooterItems).FirstOrDefault(candidate => Matches(candidate, _search.Text));
        if (item is null || string.IsNullOrEmpty(_search.Text))
        {
            return;
        }

        e.Handled = true;
        await OpenSearchResultAsync(item);
    }

    private async Task OpenSearchResultAsync(NavigationPaneItem item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _search.IsDropDownOpen = false;
            _search.SelectedItem = null;
            _search.Text = "";
        });
        await OpenAsync(item);
    }

    private async Task OpenAsync(NavigationPaneItem item)
    {
        var navigation = _follower.Current;
        if (navigation is null || item.Page is null || navigation.CurrentPage?.GetType() == item.Page)
        {
            Synchronize();
            return;
        }

        if (!await navigation.NavigateToAsync(item.Page))
        {
            Synchronize();
        }
    }

    private void OnNavigated(object? sender, NavigatedEventArgs e) => HostDispatcher.Run(Synchronize);

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _search.ItemsSource = Items.Concat(FooterItems).ToList();
        Synchronize();
    }

    private void Synchronize()
    {
        var page = _follower.Current?.CurrentPage?.GetType();
        var all = Items.Concat(FooterItems).ToList();
        var item = page is null
            ? null
            : all.FirstOrDefault(candidate => candidate.Page == page) ??
              all.FirstOrDefault(candidate => candidate == SelectedItem);

        _isSynchronizing = true;
        try
        {
            _items.SelectedItem = item is not null && Items.Contains(item) ? item : null;
            _footerItems.SelectedItem = item is not null && FooterItems.Contains(item) ? item : null;
        }
        finally
        {
            _isSynchronizing = false;
        }

        SelectedItem = item;
    }

    private void UpdateSearch()
    {
        _search.IsVisible = IsSearchVisible && IsPaneOpen;
        _searchButton.IsVisible = IsSearchVisible && !IsPaneOpen;
    }
}
