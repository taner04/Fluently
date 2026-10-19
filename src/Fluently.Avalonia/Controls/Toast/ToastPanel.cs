using Avalonia;
using Avalonia.Controls;

namespace Fluently;

/// <summary>
///     Stacks toasts, the first one at the top or, with <see cref="StacksFromBottom" />, at the bottom, and moves the
///     remaining ones into place when a toast is added or removed.
/// </summary>
internal sealed class ToastPanel : Panel
{
    private const double PositionTolerance = 0.5;
    private const double Spacing = 10;

    private readonly Dictionary<Control, Rect> _previousBounds = [];

    /// <summary>
    ///     Whether the first toast is placed at the bottom, so the newest is nearest a bottom edge.
    /// </summary>
    public bool StacksFromBottom { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d;
        var height = 0d;

        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        return new Size(width, height + Spacing * Math.Max(0, Children.Count - 1));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var currentChildren = Children.ToHashSet();
        foreach (var child in _previousBounds.Keys.Where(child => !currentChildren.Contains(child)).ToArray())
        {
            _previousBounds.Remove(child);
        }

        var y = 0d;
        foreach (var child in StacksFromBottom ? Children.Reverse() : Children)
        {
            var bounds = new Rect(0, y, finalSize.Width, child.DesiredSize.Height);
            if (_previousBounds.TryGetValue(child, out var previousBounds)
                && Math.Abs(previousBounds.Y - bounds.Y) > PositionTolerance)
            {
                AnimateToNewPosition(child, previousBounds.Y - bounds.Y);
            }

            child.Arrange(bounds);
            _previousBounds[child] = bounds;
            y += bounds.Height + Spacing;
        }

        return finalSize;
    }

    private static void AnimateToNewPosition(Control child, double offset)
    {
        if (child is ToastItem item)
        {
            item.MoveFrom(offset);
        }
    }
}
