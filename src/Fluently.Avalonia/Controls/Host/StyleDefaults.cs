using Avalonia;
using Avalonia.Data;

namespace Fluently;

/// <summary>
///     Sets the built-in look of Fluently's controls at style priority, so an app's own styles replace it.
/// </summary>
internal static class StyleDefaults
{
    public static void Set<T>(AvaloniaObject target, StyledProperty<T> property, T value) =>
        target.SetValue(property, value, BindingPriority.Style);
}
