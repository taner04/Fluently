using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Fluently;

/// <summary>
///     The title bar of a window whose content extends into its title bar, such as a <see cref="FluentWindow" />: shows a
///     back button for its <see cref="Navigation" />, an <see cref="Icon" />, the <see cref="Title" /> (the window's title
///     by
///     default) and your own <see cref="Content" /> on the right. It leaves room for the system's window buttons, moves
///     the window when dragged, and maximizes or restores it
///     when double-clicked. It hides itself while the window shows its own title bar.
///     <code>
/// &lt;fluently:TitleBar Navigation="{Binding Navigation}" Icon="{StaticResource AppIcon}" /&gt;
/// </code>
///     Restyle its parts with the style classes <c>title-bar-back</c>, <c>title-bar-icon</c> and <c>title-bar-title</c>.
/// </summary>
public class TitleBar : Control
{
    private const double MacWindowButtonsWidth = 78;
    private const double WindowsWindowButtonsWidth = 140;

    /// <summary>
    ///     Defines the <see cref="Title" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Title));

    /// <summary>
    ///     Defines the <see cref="Icon" /> property.
    /// </summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(Icon));

    /// <summary>
    ///     Defines the <see cref="Navigation" /> property.
    /// </summary>
    public static readonly StyledProperty<INavigationService?> NavigationProperty =
        FluentWindow.NavigationProperty.AddOwner<TitleBar>();

    /// <summary>
    ///     Defines the <see cref="IsBackButtonVisible" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsBackButtonVisibleProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(IsBackButtonVisible), true);

    /// <summary>
    ///     Defines the <see cref="Content" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<TitleBar, Control?>(nameof(Content));

    private readonly BackButton _back = new("title-bar-back");
    private readonly Decorator _content = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ContentControl _icon = new() { VerticalAlignment = VerticalAlignment.Center, Focusable = false };
    private readonly Grid _root = new() { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*,Auto") };
    private readonly TextBlock _title = new() { VerticalAlignment = VerticalAlignment.Center };
    private Window? _window;

    /// <summary>
    ///     Creates a title bar.
    /// </summary>
    public TitleBar()
    {
        DataTemplates.Add(IconTemplate.Create(16));

        StyleDefaults.Set(_back, MarginProperty, new Thickness(0, 0, 8, 0));
        UpdateBack();

        _icon.Classes.Add("title-bar-icon");
        Grid.SetColumn(_icon, 1);
        _icon[!ContentControl.ContentProperty] = this[!IconProperty];
        StyleDefaults.Set(_icon, MarginProperty, new Thickness(0, 0, 12, 0));

        _title.Classes.Add("title-bar-title");
        _title.IsHitTestVisible = false;
        StyleDefaults.Set(_title, TextBlock.FontSizeProperty, 12d);
        Grid.SetColumn(_title, 2);

        Grid.SetColumn(_content, 4);
        _root.Children.AddRange([_back, _icon, _title, _content]);
        StyleDefaults.Set(_root, Panel.BackgroundProperty, Brushes.Transparent);
        StyleDefaults.Set(this, HeightProperty, 48d);

        VisualChildren.Add(_root);
        LogicalChildren.Add(_root);
        AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    /// <summary>
    ///     The navigation service the back button goes back in; the window's <see cref="FluentWindow.Navigation" /> unless
    ///     set. Without one, the title bar has no back button.
    /// </summary>
    public INavigationService? Navigation
    {
        get => GetValue(NavigationProperty);
        set => SetValue(NavigationProperty, value);
    }

    /// <summary>
    ///     Whether the title bar shows a back button, enabled while its <see cref="Navigation" /> can go back.
    ///     <see langword="true" /> by default.
    /// </summary>
    public bool IsBackButtonVisible
    {
        get => GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    /// <summary>
    ///     The title shown; the window's title when <see langword="null" />.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    ///     The icon shown before the title: a <see cref="Geometry" />, shown as a <see cref="PathIcon" />, or any other
    ///     content, such as an <see cref="Image" />.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    ///     Content of your own, shown on the right, such as a search box or an account button.
    /// </summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowPropertyChanged;
        }

        UpdateTitle();
        UpdateDecorations();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowPropertyChanged;
            _window = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            UpdateTitle();
        }
        else if (change.Property == NavigationProperty || change.Property == IsBackButtonVisibleProperty)
        {
            UpdateBack();
        }
        else if (change.Property == ContentProperty)
        {
            _content.Child = Content;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.TitleProperty)
        {
            UpdateTitle();
        }
        else if (e.Property == Window.IsExtendedIntoWindowDecorationsProperty)
        {
            UpdateDecorations();
        }
    }

    private void UpdateBack()
    {
        _back.Navigation = Navigation;
        _back.IsVisible = IsBackButtonVisible && Navigation is not null;
    }

    private void UpdateTitle() => _title.Text = Title ?? _window?.Title;

    private void UpdateDecorations()
    {
        var isExtended = _window?.IsExtendedIntoWindowDecorations == true;
        StyleDefaults.Set(this, IsVisibleProperty, isExtended);
        StyleDefaults.Set(_root, MarginProperty, new Thickness(
            OperatingSystem.IsMacOS() ? MacWindowButtonsWidth : 8,
            0,
            OperatingSystem.IsMacOS() ? 8 : WindowsWindowButtonsWidth,
            0));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window is not { } window || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            if (window.CanResize)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }
        else
        {
            window.BeginMoveDrag(e);
        }

        e.Handled = true;
    }
}
