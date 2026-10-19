namespace Fluently;

/// <summary>
///     Where toasts are shown, set with <see cref="IToastService.Position" />. The newest toast is always the one nearest
///     the edge, and toasts slide in from the side they are shown on.
/// </summary>
public enum ToastPosition
{
    /// <summary>
    ///     The top right corner; toasts slide in from the right.
    /// </summary>
    TopRight,

    /// <summary>
    ///     The top left corner; toasts slide in from the left.
    /// </summary>
    TopLeft,

    /// <summary>
    ///     The middle of the top edge; toasts slide down from the top.
    /// </summary>
    TopCenter,

    /// <summary>
    ///     The bottom right corner; toasts slide in from the right.
    /// </summary>
    BottomRight,

    /// <summary>
    ///     The bottom left corner; toasts slide in from the left.
    /// </summary>
    BottomLeft,

    /// <summary>
    ///     The middle of the bottom edge; toasts slide up from the bottom.
    /// </summary>
    BottomCenter
}
