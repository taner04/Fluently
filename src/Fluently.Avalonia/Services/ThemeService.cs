using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using System.ComponentModel;

namespace Fluently;

/// <summary>
///     Switches the theme variant of an Avalonia <see cref="Application" />. <see cref="Theme" /> is set right away on any
///     thread and applied on the UI thread; changes made to the application directly are followed too.
/// </summary>
internal sealed class ThemeService : IThemeService, IDisposable
{
    private static readonly PropertyChangedEventArgs ThemeChanged = new(nameof(Theme));
    private static readonly PropertyChangedEventArgs ActualThemeChanged = new(nameof(ActualTheme));

    private readonly Application _application;
    private int _theme;
    private int _actualTheme;
    private bool _applying;

    public ThemeService(Application application)
    {
        _application = application;
        Dispatcher.UIThread.Invoke(() =>
        {
            _theme = (int)ToTheme(application.RequestedThemeVariant);
            _actualTheme = (int)ToActualTheme(application.ActualThemeVariant);
            application.PropertyChanged += OnApplicationPropertyChanged;
            application.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        });
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public ApplicationTheme Theme
    {
        get => (ApplicationTheme)Volatile.Read(ref _theme);
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value is not an ApplicationTheme.");
            }

            if (Interlocked.Exchange(ref _theme, (int)value) == (int)value)
            {
                return;
            }

            PropertyChanged?.Invoke(this, ThemeChanged);
            HostDispatcher.Run(Apply);
        }
    }

    /// <inheritdoc />
    public ApplicationTheme ActualTheme => (ApplicationTheme)Volatile.Read(ref _actualTheme);

    /// <inheritdoc />
    public void Dispose()
    {
        _application.PropertyChanged -= OnApplicationPropertyChanged;
        _application.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
    }

    private void Apply()
    {
        _applying = true;
        try
        {
            _application.RequestedThemeVariant = Theme switch
            {
                ApplicationTheme.Light => ThemeVariant.Light,
                ApplicationTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
        finally
        {
            _applying = false;
        }
    }

    private void OnApplicationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_applying && e.Property == Application.RequestedThemeVariantProperty)
        {
            Update(ref _theme, ToTheme(_application.RequestedThemeVariant), ThemeChanged);
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e) =>
        Update(ref _actualTheme, ToActualTheme(_application.ActualThemeVariant), ActualThemeChanged);

    private void Update(ref int field, ApplicationTheme value, PropertyChangedEventArgs change)
    {
        if (Interlocked.Exchange(ref field, (int)value) != (int)value)
        {
            PropertyChanged?.Invoke(this, change);
        }
    }

    private static ApplicationTheme ToTheme(ThemeVariant? variant) =>
        variant is null || variant == ThemeVariant.Default ? ApplicationTheme.System : ToActualTheme(variant);

    private static ApplicationTheme ToActualTheme(ThemeVariant? variant)
    {
        if (variant?.InheritVariant is { } inherited)
        {
            return ToActualTheme(inherited);
        }

        return variant == ThemeVariant.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }
}
