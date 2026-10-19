using CommunityToolkit.Mvvm.ComponentModel;
using Fluently.Gallery.Services;
using System.Reflection;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class SettingsViewModel : ObservableObject, INavigableViewModel
{
    private readonly ThemeService _themes;

    public SettingsViewModel(ThemeService themes)
    {
        _themes = themes;
        Theme = themes.Theme;
    }

    public IReadOnlyList<string> Themes => _themes.Themes;

    public string Version { get; } =
        typeof(INavigationService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "unknown";

    [ObservableProperty] public partial string Theme { get; set; }

    partial void OnThemeChanged(string value) => _themes.Theme = value;
}
