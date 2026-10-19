using System.ComponentModel;

namespace Fluently;

/// <summary>
///     Shows short, non-blocking <see cref="IToast" /> notifications. The newest toast comes first, at most
///     <see cref="ToastOptions.MaxCount" /> are shown and the oldest is removed to make room, and every toast closes by
///     itself after its duration unless it is dismissed earlier. <see cref="Toasts" /> and <see cref="Position" /> raise
///     <see cref="INotifyPropertyChanged.PropertyChanged" />, so a toast host can bind to them directly.
/// </summary>
public interface IToastService : INotifyPropertyChanged
{
    /// <summary>
    ///     The toasts that are shown, newest first.
    /// </summary>
    IReadOnlyList<IToast> Toasts { get; }

    /// <summary>
    ///     Where the toasts are shown. Starts at <see cref="ToastOptions.Position" />; changing it moves the toasts that are
    ///     shown.
    /// </summary>
    ToastPosition Position { get; set; }

    /// <summary>
    ///     Shows <paramref name="toast" />, which you created, first. A toast that is already shown moves to the top and
    ///     starts its duration
    ///     again. <paramref name="duration" /> defaults to <see cref="ToastOptions.Duration" />; pass
    ///     <see cref="Timeout.InfiniteTimeSpan" /> to keep the toast until it is dismissed.
    /// </summary>
    /// <param name="toast">The toast to show.</param>
    /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="toast" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="duration" /> is zero, negative other than
    ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
    void Show(IToast toast, TimeSpan? duration = null);

    /// <summary>
    ///     Creates the toast of view model type <paramref name="toastType" /> from its own service scope, shows it like
    ///     <see cref="Show(IToast, TimeSpan?)" /> and returns it. The scope is disposed when the toast closes.
    ///     <see cref="ToastServiceExtensions.Show{TToast}(IToastService, TimeSpan?)" /> is the same with the type checked at
    ///     compile time.
    /// </summary>
    /// <param name="toastType">The type of the toast view model, registered in the container.</param>
    /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
    /// <returns>The toast that was created and shown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toastType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="toastType" /> does not implement <see cref="IToast" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="duration" /> is zero, negative other than
    ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
    IToast Show(Type toastType, TimeSpan? duration = null);

    /// <summary>
    ///     Closes <paramref name="toast" /> before its duration ends.
    ///     Returns <see langword="false" /> when it is not shown.
    /// </summary>
    /// <param name="toast">The toast to close.</param>
    /// <returns><see langword="true" /> when the toast was closed; <see langword="false" /> when it is not shown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="toast" /> is <see langword="null" />.</exception>
    bool Dismiss(IToast toast);
}
