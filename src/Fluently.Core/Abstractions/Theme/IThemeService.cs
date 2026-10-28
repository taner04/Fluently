using System.ComponentModel;

namespace Fluently;

/// <summary>
///     Switches the app between the light and the dark theme, or lets it follow the operating system. It only switches:
///     the colors come from the theme the app uses, and saving the choice between runs is up to the app.
///     <see cref="Theme" /> and <see cref="ActualTheme" /> raise <see cref="INotifyPropertyChanged.PropertyChanged" />,
///     so views can bind to them directly. The UI framework package registers the implementation.
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    /// <summary>
    ///     The theme the app asks for. Starts at the theme the app sets at startup, <see cref="ApplicationTheme.System" />
    ///     when it sets none. Setting it switches the app's theme and can be done from any thread.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is not an <see cref="ApplicationTheme" />.</exception>
    ApplicationTheme Theme { get; set; }

    /// <summary>
    ///     The theme the app is shown in: <see cref="ApplicationTheme.Light" /> or <see cref="ApplicationTheme.Dark" />,
    ///     never <see cref="ApplicationTheme.System" />. Follows the operating system while <see cref="Theme" /> is
    ///     <see cref="ApplicationTheme.System" />.
    /// </summary>
    ApplicationTheme ActualTheme { get; }
}
