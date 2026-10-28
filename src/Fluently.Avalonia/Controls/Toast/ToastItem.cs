using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Fluently;

/// <summary>
///     Wraps one toast and moves it the way Fluent moves notifications: it slides in from its edge while fading in, slides
///     back out while fading out, and glides to its new place when other toasts come or go. Slide and fade follow one
///     progress value, so they always start and finish together, and a toast that comes back while leaving turns around
///     from where it is.
/// </summary>
internal sealed class ToastItem : Decorator
{
    private static readonly StyledProperty<double> PresenceProperty =
        AvaloniaProperty.Register<ToastItem, double>("Presence");

    private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(367);
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(367);

    private static readonly Easing Decelerate = new SplineEasing(0.1, 0.9, 0.2);
    private static readonly Easing Accelerate = new SplineEasing(0.4, 0, 1);

    private readonly TranslateTransform _move = new();
    private readonly DoubleTransition _presence = new() { Property = PresenceProperty };
    private readonly Vector _slideFrom;
    private readonly TranslateTransform _slide = new();
    private int _exit;
    private bool _hasEntered;

    /// <summary>
    ///     Creates the item; <paramref name="slideFrom" /> is the offset the toast slides in from and out to.
    /// </summary>
    public ToastItem(IToast toast, Control content, Vector slideFrom)
    {
        Toast = toast;
        Child = content;
        _slideFrom = slideFrom;
        RenderTransform = new TransformGroup { Children = { _slide, _move } };
        ApplyPresence(0);
        Transitions = [_presence];
        _move.Transitions =
        [
            new DoubleTransition { Property = TranslateTransform.YProperty, Duration = MoveDuration, Easing = Decelerate }
        ];
    }

    public IToast Toast { get; }

    public bool IsLeaving { get; private set; }

    /// <summary>
    ///     Slides the toast back in, when it is shown again while it was sliding out.
    /// </summary>
    public void Return()
    {
        SetLeaving(false);
        AnimatePresence(1, EnterDuration, Decelerate);
    }

    /// <summary>
    ///     Slides the toast out. Returns <see langword="true" /> when it is gone and can be removed, or
    ///     <see langword="false" /> when it came back, or started leaving again, while it slid out.
    /// </summary>
    public async Task<bool> LeaveAsync()
    {
        var exit = ++_exit;
        SetLeaving(true);
        AnimatePresence(0, ExitDuration, Accelerate);
        await Task.Delay(ExitDuration);
        return IsLeaving && exit == _exit;
    }

    public void MoveFrom(double offset)
    {
        var transitions = _move.Transitions;
        _move.Transitions = null;
        _move.Y += offset;
        _move.Transitions = transitions;
        Dispatcher.UIThread.Post(() => _move.Y = 0, DispatcherPriority.Background);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_hasEntered)
        {
            return;
        }

        _hasEntered = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsLeaving)
            {
                AnimatePresence(1, EnterDuration, Decelerate);
            }
        }, DispatcherPriority.Background);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresenceProperty)
        {
            ApplyPresence(change.GetNewValue<double>());
        }
    }

    private void AnimatePresence(double presence, TimeSpan duration, Easing easing)
    {
        _presence.Duration = duration;
        _presence.Easing = easing;
        SetValue(PresenceProperty, presence);
    }

    private void ApplyPresence(double presence)
    {
        Opacity = presence;
        _slide.X = _slideFrom.X * (1 - presence);
        _slide.Y = _slideFrom.Y * (1 - presence);
    }

    private void SetLeaving(bool isLeaving)
    {
        IsLeaving = isLeaving;
        IsHitTestVisible = !isLeaving;
        (Parent as Layoutable)?.InvalidateMeasure();
    }
}
