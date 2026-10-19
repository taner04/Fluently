using Avalonia;

namespace Fluently;

/// <summary>
///     An entry of a <see cref="NavigationPane" />: selecting it navigates to <see cref="Page" />, and it is selected
///     while
///     that page is the current page.
///     <code>
/// &lt;fluently:NavigationPaneItem Title="Home" Icon="{StaticResource HomeIcon}" Page="{x:Type vm:HomeViewModel}" /&gt;
/// </code>
/// </summary>
public class NavigationPaneItem : AvaloniaObject
{
    /// <summary>
    ///     Defines the <see cref="Title" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NavigationPaneItem, string?>(nameof(Title));

    /// <summary>
    ///     Defines the <see cref="Icon" /> property.
    /// </summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<NavigationPaneItem, object?>(nameof(Icon));

    /// <summary>
    ///     Defines the <see cref="Page" /> property.
    /// </summary>
    public static readonly StyledProperty<Type?> PageProperty =
        AvaloniaProperty.Register<NavigationPaneItem, Type?>(nameof(Page));

    /// <summary>
    ///     The text shown next to the icon while the pane is open, and as the tooltip.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    ///     The icon: a <see cref="Avalonia.Media.Geometry" />, shown as a <see cref="Avalonia.Controls.PathIcon" />, or any
    ///     other content, such as an icon control.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    ///     The type of the page view model to navigate to.
    /// </summary>
    public Type? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    /// <inheritdoc />
    public override string ToString() => Title ?? "";
}
