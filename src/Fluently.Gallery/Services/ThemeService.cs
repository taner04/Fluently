using Avalonia;
using Avalonia.Styling;

namespace Fluently.Gallery.Services;

public sealed class ThemeService
{
    public IReadOnlyList<string> Themes { get; } = ["System", "Light", "Dark"];

    public string Theme
    {
        get => (Application.Current?.RequestedThemeVariant?.Key as string) switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "System"
        };
        set
        {
            if (Application.Current is { } application)
            {
                application.RequestedThemeVariant = value switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
        }
    }
}
