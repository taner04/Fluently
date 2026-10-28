using Avalonia;
using Avalonia.Controls;

namespace Fluently;

/// <summary>
///     Stacks toasts, the first one nearest the top edge or, with <see cref="StacksFromBottom" />, the bottom edge, and
///     moves the remaining ones into place when a toast is added or removed. A leaving toast keeps its place while it
///     slides out but no longer takes room, so the others close the gap at the same time.
/// </summary>
internal sealed class ToastPanel : Panel
{
    private const double PositionTolerance = 0.5;
    private const double Spacing = 10;

    private readonly Dictionary<Control, double> _previousOffsets = [];

    /// <summary>
    ///     Whether the first toast is placed at the bottom, so the newest is nearest a bottom edge.
    /// </summary>
    public bool StacksFromBottom { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d;
        var height = 0d;
        var stacked = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            if (!IsLeaving(child))
            {
                height += child.DesiredSize.Height;
                stacked++;
            }
        }

        return new Size(width, height + Spacing * Math.Max(0, stacked - 1));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var currentChildren = Children.ToHashSet();
        foreach (var child in _previousOffsets.Keys.Where(child => !currentChildren.Contains(child)).ToArray())
        {
            _previousOffsets.Remove(child);
        }

        var offset = 0d;
        foreach (var child in Children)
        {
            var height = child.DesiredSize.Height;
            if (IsLeaving(child))
            {
                child.Arrange(Place(_previousOffsets.GetValueOrDefault(child, offset), height, finalSize));
                continue;
            }

            if (_previousOffsets.TryGetValue(child, out var previousOffset)
                && Math.Abs(previousOffset - offset) > PositionTolerance
                && child is ToastItem item)
            {
                item.MoveFrom(StacksFromBottom ? offset - previousOffset : previousOffset - offset);
            }

            child.Arrange(Place(offset, height, finalSize));
            _previousOffsets[child] = offset;
            offset += height + Spacing;
        }

        return finalSize;
    }

    private static bool IsLeaving(Control child) => child is ToastItem { IsLeaving: true };

    private Rect Place(double offset, double height, Size finalSize) =>
        new(0, StacksFromBottom ? finalSize.Height - offset - height : offset, finalSize.Width, height);
}
