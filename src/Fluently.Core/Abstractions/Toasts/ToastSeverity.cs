namespace Fluently;

/// <summary>
///     How important a <see cref="Toast" /> is, which decides how it is shown.
/// </summary>
public enum ToastSeverity
{
    /// <summary>
    ///     Something the user may want to know.
    /// </summary>
    Information,

    /// <summary>
    ///     Something finished as intended.
    /// </summary>
    Success,

    /// <summary>
    ///     Something needs attention but did not fail.
    /// </summary>
    Warning,

    /// <summary>
    ///     Something failed.
    /// </summary>
    Error
}
