namespace Fluently;

/// <summary>
///     A theme an app can ask for with <see cref="IThemeService.Theme" />.
/// </summary>
public enum ApplicationTheme
{
    /// <summary>
    ///     Follows the theme of the operating system, also when it changes while the app runs.
    /// </summary>
    System,

    /// <summary>
    ///     The light theme.
    /// </summary>
    Light,

    /// <summary>
    ///     The dark theme.
    /// </summary>
    Dark
}
