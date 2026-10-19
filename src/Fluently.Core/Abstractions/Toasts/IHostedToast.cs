namespace Fluently;

/// <summary>
/// A toast that remembers the toast service showing it, so it can close itself: <see cref="ToastViewModel"/>.
/// </summary>
internal interface IHostedToast
{
    IToastService? Toasts { set; }
}
