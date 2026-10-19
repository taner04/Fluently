using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Fluently.Gallery.Controls;

public partial class SettingCard : UserControl
{
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SettingCard, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<SettingCard, string?>(nameof(Header));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingCard, string?>(nameof(Description));

    public static readonly StyledProperty<object?> ActionProperty =
        AvaloniaProperty.Register<SettingCard, object?>(nameof(Action));

    public SettingCard()
    {
        InitializeComponent();
    }

    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Action
    {
        get => GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }
}
