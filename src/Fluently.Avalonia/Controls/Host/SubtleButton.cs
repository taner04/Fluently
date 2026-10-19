using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Fluently;

/// <summary>
///     Gives an icon button of Fluently's controls its built-in look: transparent until hovered, without a border.
/// </summary>
internal static class SubtleButton
{
    public static void Apply(Button button, Geometry icon, string name, string styleClass)
    {
        button.Content = new PathIcon { Data = icon, Width = 14, Height = 14 };
        button.Classes.Add(styleClass);
        AutomationProperties.SetName(button, name);
        ToolTip.SetTip(button, name);
        StyleDefaults.Set(button, TemplatedControl.BackgroundProperty, Brushes.Transparent);
        StyleDefaults.Set(button, TemplatedControl.BorderThicknessProperty, new Thickness(0));
        StyleDefaults.Set(button, TemplatedControl.PaddingProperty, new Thickness(0));
        StyleDefaults.Set(button, Layoutable.WidthProperty, 40d);
        StyleDefaults.Set(button, Layoutable.HeightProperty, 36d);
        StyleDefaults.Set(button, Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        StyleDefaults.Set(button, Layoutable.VerticalAlignmentProperty, VerticalAlignment.Center);
        StyleDefaults.Set(button, ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
        StyleDefaults.Set(button, ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center);
    }
}
