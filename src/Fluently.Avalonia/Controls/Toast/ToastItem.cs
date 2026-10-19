using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Fluently;

/// <summary>
///     Wraps one toast and moves it the way Fluent moves notifications: it slides in from its edge with the decelerating
///     curve while fading in gently, slides back out with the accelerating curve, which is shorter, and glides to its new
///     place when other toasts come or go.
/// </summary>
internal sealed class ToastItem : Decorator
{
    private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(167);
    private static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(250);

    private static readonly Easing Decelerate = new SplineEasing(0, 0, 0);
    private static readonly Easing Accelerate = new SplineEasing(0.7, 0, 1, 0.5);
    private static readonly Easing EasyEase = new SplineEasing(0.33, 0, 0.67);

    private readonly TranslateTransform _move = new();
    private readonly Vector _slideFrom;
    private readonly TranslateTransform _transform;
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
        _transform = new TranslateTransform { X = slideFrom.X, Y = slideFrom.Y };
        Opacity = 0;
        RenderTransform = new TransformGroup { Children = { _transform, _move } };
        _move.Transitions =
        [
            new DoubleTransition { Property = TranslateTransform.YProperty, Duration = MoveDuration, Easing = Decelerate }
        ];
        Animate(EnterDuration, Decelerate, FadeInDuration, EasyEase);
    }

    public IToast Toast { get; }

    public bool IsLeaving { get; private set; }

    /// <summary>
    ///     Slides the toast back in, when it is shown again while it was sliding out.
    /// </summary>
    public void Return()
    {
        IsLeaving = false;
        IsHitTestVisible = true;
        Animate(EnterDuration, Decelerate, FadeInDuration, EasyEase);
        _transform.X = 0;
        _transform.Y = 0;
        Opacity = 1;
    }

    /// <summary>
    ///     Slides the toast out. Returns <see langword="true" /> when it is gone and can be removed, or
    ///     <see langword="false" /> when it came back, or started leaving again, while it slid out.
    /// </summary>
    public async Task<bool> LeaveAsync()
    {
        var exit = ++_exit;
        IsLeaving = true;
        IsHitTestVisible = false;
        Animate(ExitDuration, Accelerate, ExitDuration, Accelerate);
        _transform.X = _slideFrom.X;
        _transform.Y = _slideFrom.Y;
        Opacity = 0;
        await Task.Delay(ExitDuration);
        return IsLeaving && exit == _exit;
    }

    public void MoveFrom(double offset)
    {
        var transitions = _move.Transitions;
        _move.Transitions = null;
        _move.Y = offset;
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
                _transform.X = 0;
                _transform.Y = 0;
                Opacity = 1;
            }
        }, DispatcherPriority.Background);
    }

    private void Animate(TimeSpan slideDuration, Easing slideEasing, TimeSpan fadeDuration, Easing fadeEasing)
    {
        _transform.Transitions =
        [
            new DoubleTransition
                { Property = TranslateTransform.XProperty, Duration = slideDuration, Easing = slideEasing },
            new DoubleTransition
                { Property = TranslateTransform.YProperty, Duration = slideDuration, Easing = slideEasing }
        ];
        Transitions =
            [new DoubleTransition { Property = OpacityProperty, Duration = fadeDuration, Easing = fadeEasing }];
    }
}
