namespace Fluently;

/// <summary>
///     A toast with a title and a message, shown with the toast host's built-in look.
///     Show it with <see cref="ToastServiceExtensions.Show(IToastService, string, string, ToastSeverity, TimeSpan?)" />.
/// </summary>
public sealed class Toast : IToast
{
    /// <summary>
    ///     Creates a toast.
    /// </summary>
    /// <param name="title">The title, shown emphasized.</param>
    /// <param name="message">The message below the title; empty for a toast with only a title.</param>
    /// <param name="severity">How important the toast is, which decides its color and icon.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="title" /> or <paramref name="message" /> is
    ///     <see langword="null" />.
    /// </exception>
    public Toast(string title, string message, ToastSeverity severity = ToastSeverity.Information)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        Title = title;
        Message = message;
        Severity = severity;
    }

    /// <summary>
    ///     The title, shown emphasized.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     The message below the title.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     How important the toast is.
    /// </summary>
    public ToastSeverity Severity { get; }
}
