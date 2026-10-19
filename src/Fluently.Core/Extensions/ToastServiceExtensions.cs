namespace Fluently;

/// <summary>
///     Shows the built-in <see cref="Toast" />, and toasts with their type checked at compile time.
/// </summary>
public static class ToastServiceExtensions
{
    /// <param name="toasts">The toast service to show the toast with.</param>
    extension(IToastService toasts)
    {
        /// <summary>
        ///     Shows a <see cref="Toast" /> with <paramref name="title" /> and <paramref name="message" /> and returns it,
        ///     so it can be dismissed early.
        /// </summary>
        /// <param name="title">The title, shown emphasized.</param>
        /// <param name="message">The message below the title; empty for a toast with only a title.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title" /> or <paramref name="message" /> is
        ///     <see langword="null" />.
        /// </exception>
        /// <param name="severity">How important the toast is, which decides its color and icon.</param>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that is shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public Toast Show(
            string title,
            string message,
            ToastSeverity severity = ToastSeverity.Information,
            TimeSpan? duration = null)
        {
            var toast = new Toast(title, message, severity);
            toasts.Show(toast, duration);
            return toast;
        }

        /// <summary>
        ///     Shows a <see cref="Toast" /> with <see cref="ToastSeverity.Information" />, telling something the user may want to
        ///     know, and returns it.
        /// </summary>
        /// <param name="title">The title, shown emphasized.</param>
        /// <param name="message">The message below the title; empty for a toast with only a title.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title" /> or <paramref name="message" /> is
        ///     <see langword="null" />.
        /// </exception>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that is shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public Toast ShowInformation(string title, string message, TimeSpan? duration = null) =>
            toasts.Show(title, message, ToastSeverity.Information, duration);

        /// <summary>
        ///     Shows a <see cref="Toast" /> with <see cref="ToastSeverity.Success" />, telling that something finished as
        ///     intended, and returns it.
        /// </summary>
        /// <param name="title">The title, shown emphasized.</param>
        /// <param name="message">The message below the title; empty for a toast with only a title.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title" /> or <paramref name="message" /> is
        ///     <see langword="null" />.
        /// </exception>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that is shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public Toast ShowSuccess(string title, string message, TimeSpan? duration = null) =>
            toasts.Show(title, message, ToastSeverity.Success, duration);

        /// <summary>
        ///     Shows a <see cref="Toast" /> with <see cref="ToastSeverity.Warning" />, telling that something needs attention but
        ///     did not fail, and returns it.
        /// </summary>
        /// <param name="title">The title, shown emphasized.</param>
        /// <param name="message">The message below the title; empty for a toast with only a title.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title" /> or <paramref name="message" /> is
        ///     <see langword="null" />.
        /// </exception>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that is shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public Toast ShowWarning(string title, string message, TimeSpan? duration = null) =>
            toasts.Show(title, message, ToastSeverity.Warning, duration);

        /// <summary>
        ///     Shows a <see cref="Toast" /> with <see cref="ToastSeverity.Error" />, telling that something failed, and returns
        ///     it.
        /// </summary>
        /// <param name="title">The title, shown emphasized.</param>
        /// <param name="message">The message below the title; empty for a toast with only a title.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title" /> or <paramref name="message" /> is
        ///     <see langword="null" />.
        /// </exception>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that is shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public Toast ShowError(string title, string message, TimeSpan? duration = null) =>
            toasts.Show(title, message, ToastSeverity.Error, duration);

        /// <summary>
        ///     Creates the toast of view model type <typeparamref name="TToast" /> from its own service scope, shows it and
        ///     returns it. The scope is disposed when the toast closes.
        ///     The same as <see cref="IToastService.Show(Type, TimeSpan?)" />, with the toast type checked at compile time: a
        ///     type that is not an <see cref="IToast" /> does not compile, so this call never throws
        ///     <see cref="ArgumentException" />, and it returns the toast as <typeparamref name="TToast" />.
        /// </summary>
        /// <typeparam name="TToast">The type of the toast view model, registered in the container.</typeparam>
        /// <param name="duration">How long the toast stays; <see langword="null" /> for <see cref="ToastOptions.Duration" />.</param>
        /// <returns>The toast that was created and shown, to dismiss it early.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="duration" /> is zero, negative other than
        ///     <see cref="Timeout.InfiniteTimeSpan" />, or longer than 49 days.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The toast service was disposed.</exception>
        public TToast Show<TToast>(TimeSpan? duration = null)
            where TToast : IToast =>
            (TToast)toasts.Show(typeof(TToast), duration);
    }
}
