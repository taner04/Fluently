using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Fluently;

/// <summary>
///     The built-in look of a <see cref="Toast" />, like WinUI's InfoBar: a card tinted in the color of its severity, with
///     its icon, the title, the message and a close button. The card uses the flyout background and border that Avalonia's
///     Fluent theme and FluentAvalonia both define; the text keeps the theme's foreground. Every value is a default that
///     application styles override: the card has the classes <c>toast</c> and its severity (<c>information</c>,
///     <c>success</c>, <c>warning</c>, <c>error</c>), and its parts <c>toast-tint</c>, <c>toast-icon</c>,
///     <c>toast-title</c>, <c>toast-message</c> and <c>toast-close</c>.
/// </summary>
internal sealed class ToastView : Border
{
    public ToastView(Toast toast, Action dismiss)
    {
        Toast = toast;
        var color = SeverityColor(toast.Severity);

        Classes.Add("toast");
        Classes.Add(toast.Severity.ToString().ToLowerInvariant());
        StyleDefaults.Set(this, WidthProperty, 400d);
        StyleDefaults.Set(this, MinHeightProperty, 64d);
        StyleDefaults.Set(this, CornerRadiusProperty, new CornerRadius(8));
        StyleDefaults.Set(this, BorderThicknessProperty, new Thickness(1));
        StyleDefaults.Set(this, BoxShadowProperty, BoxShadows.Parse("0 12 32 0 #59000000"));
        Bind(BackgroundProperty, this.GetResourceObservable("FlyoutPresenterBackground"), BindingPriority.Style);
        Bind(BorderBrushProperty, this.GetResourceObservable("FlyoutBorderThemeBrush"), BindingPriority.Style);

        var tint = new Border();
        tint.Classes.Add("toast-tint");
        StyleDefaults.Set(tint, CornerRadiusProperty, new CornerRadius(7));
        StyleDefaults.Set(tint, BackgroundProperty, new ImmutableSolidColorBrush(color, 0.16));
        Grid.SetColumnSpan(tint, 3);

        var icon = new PathIcon { Data = SeverityIcon(toast.Severity) };
        icon.Classes.Add("toast-icon");
        StyleDefaults.Set(icon, TemplatedControl.ForegroundProperty, new ImmutableSolidColorBrush(color));
        StyleDefaults.Set(icon, WidthProperty, 20d);
        StyleDefaults.Set(icon, HeightProperty, 20d);
        StyleDefaults.Set(icon, MarginProperty, new Thickness(16, 17, 0, 0));
        StyleDefaults.Set(icon, VerticalAlignmentProperty, VerticalAlignment.Top);

        var title = new TextBlock { Text = toast.Title };
        title.Classes.Add("toast-title");
        StyleDefaults.Set(title, TextBlock.FontSizeProperty, 14d);
        StyleDefaults.Set(title, TextBlock.FontWeightProperty, FontWeight.SemiBold);
        StyleDefaults.Set(title, TextBlock.TextWrappingProperty, TextWrapping.Wrap);

        var message = new TextBlock { Text = toast.Message, IsVisible = toast.Message.Length > 0 };
        message.Classes.Add("toast-message");
        StyleDefaults.Set(message, TextBlock.FontSizeProperty, 13d);
        StyleDefaults.Set(message, OpacityProperty, 0.8);
        StyleDefaults.Set(message, TextBlock.TextWrappingProperty, TextWrapping.Wrap);

        var text = new StackPanel { Children = { title, message } };
        StyleDefaults.Set(text, StackPanel.SpacingProperty, 4d);
        StyleDefaults.Set(text, MarginProperty, new Thickness(12, 16, 8, 16));
        StyleDefaults.Set(text, VerticalAlignmentProperty, VerticalAlignment.Center);
        Grid.SetColumn(text, 1);

        var close = new Button { Content = new PathIcon { Data = Glyphs.Close, Width = 12, Height = 12 } };
        close.Classes.Add("toast-close");
        StyleDefaults.Set(close, WidthProperty, 32d);
        StyleDefaults.Set(close, HeightProperty, 32d);
        StyleDefaults.Set(close, TemplatedControl.PaddingProperty, new Thickness(0));
        StyleDefaults.Set(close, MarginProperty, new Thickness(0, 10, 10, 0));
        StyleDefaults.Set(close, TemplatedControl.CornerRadiusProperty, new CornerRadius(4));
        StyleDefaults.Set(close, TemplatedControl.BackgroundProperty, Brushes.Transparent);
        StyleDefaults.Set(close, TemplatedControl.BorderThicknessProperty, new Thickness(0));
        StyleDefaults.Set(close, ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
        StyleDefaults.Set(close, ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center);
        StyleDefaults.Set(close, VerticalAlignmentProperty, VerticalAlignment.Top);
        AutomationProperties.SetName(close, "Dismiss");
        close.Click += (_, _) => dismiss();
        Grid.SetColumn(close, 2);

        Child = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Children = { tint, icon, text, close }
        };
    }

    public Toast Toast { get; }

    private static Color SeverityColor(ToastSeverity severity) => severity switch
    {
        ToastSeverity.Success => Color.FromRgb(0x2E, 0xA0, 0x43),
        ToastSeverity.Warning => Color.FromRgb(0xE0, 0xA1, 0x00),
        ToastSeverity.Error => Color.FromRgb(0xE0, 0x45, 0x48),
        _ => Color.FromRgb(0x2B, 0x88, 0xD8)
    };

    private static Geometry SeverityIcon(ToastSeverity severity) => StreamGeometry.Parse(severity switch
    {
        ToastSeverity.Success =>
            "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M10 17L5 12L6.41 10.59L10 14.17L17.59 6.58L19 8L10 17Z",
        ToastSeverity.Warning => "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z",
        ToastSeverity.Error =>
            "M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z",
        _ => "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
    });
}
