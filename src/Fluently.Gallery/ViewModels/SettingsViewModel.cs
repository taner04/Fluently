using System.Reflection;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed class SettingsViewModel(IThemeService theme) : INavigableViewModel
{
    public IThemeService Theme { get; } = theme;

    public IReadOnlyList<ApplicationTheme> Themes { get; } = Enum.GetValues<ApplicationTheme>();

    public string Version { get; } =
        typeof(INavigationService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "unknown";
}
