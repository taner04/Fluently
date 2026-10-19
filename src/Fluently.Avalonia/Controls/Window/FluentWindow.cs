using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Fluently;

/// <summary>
///     A window in the style of Windows 11 apps: its content extends into the title bar, so a <see cref="TitleBar" />, a
///     <see cref="NavigationPane" /> and your pages reach the top edge, and on Windows 11 it shows the Mica material
///     behind them. Its <see cref="Navigation" /> is used by the <see cref="TitleBar" /> and <see cref="NavigationPane" />
///     inside it, and the mouse's back and forward buttons navigate it. Derive your main window from it.
///     <code>
/// &lt;fluently:FluentWindow x:Class="MyApp.MainWindow" Title="My App" Navigation="{Binding Navigation}"&gt;
///     &lt;Grid RowDefinitions="Auto,*"&gt;
///         &lt;fluently:TitleBar /&gt;
///         &lt;fluently:NavigationPane Grid.Row="1" IsBackButtonVisible="False" /&gt;
///     &lt;/Grid&gt;
/// &lt;/fluently:FluentWindow&gt;
/// </code>
///     While Mica is shown, the window's <see cref="Avalonia.Controls.Primitives.TemplatedControl.Background" /> is
///     transparent, so give the areas
///     above it translucent backgrounds.
/// </summary>
public abstract class FluentWindow : Window
{
    /// <summary>
    ///     Defines the <see cref="ExtendsContentIntoTitleBar" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> ExtendsContentIntoTitleBarProperty =
        AvaloniaProperty.Register<FluentWindow, bool>(nameof(ExtendsContentIntoTitleBar), true);

    /// <summary>
    ///     Defines the <see cref="TitleBarHeight" /> property.
    /// </summary>
    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<FluentWindow, double>(nameof(TitleBarHeight), 48);

    /// <summary>
    ///     Defines the <see cref="Navigation" /> property. It is inherited, so the <see cref="TitleBar" /> and
    ///     <see cref="NavigationPane" /> in the window use this navigation service unless they set their own.
    /// </summary>
    public static readonly StyledProperty<INavigationService?> NavigationProperty =
        AvaloniaProperty.Register<FluentWindow, INavigationService?>(nameof(Navigation), inherits: true);

    /// <summary>
    ///     Defines the <see cref="IsMouseNavigationEnabled" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsMouseNavigationEnabledProperty =
        AvaloniaProperty.Register<FluentWindow, bool>(nameof(IsMouseNavigationEnabled), true);

    /// <summary>
    ///     Defines the <see cref="IsMicaEnabled" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsMicaEnabledProperty =
        AvaloniaProperty.Register<FluentWindow, bool>(nameof(IsMicaEnabled), true);

    private IDisposable? _transparentBackground;

    /// <summary>
    ///     Creates a fluent window.
    /// </summary>
    protected FluentWindow()
    {
        ApplyTitleBar();
        ApplyBackdrop();
        AddHandler(PointerReleasedEvent, OnWindowPointerReleased, RoutingStrategies.Tunnel, true);
    }

    /// <summary>
    ///     The main navigation service of the window. The <see cref="TitleBar" /> and <see cref="NavigationPane" /> in the
    ///     window use it unless they set their own <c>Navigation</c>, and the mouse's back and forward buttons navigate it.
    /// </summary>
    public INavigationService? Navigation
    {
        get => GetValue(NavigationProperty);
        set => SetValue(NavigationProperty, value);
    }

    /// <summary>
    ///     Whether the mouse's back and forward buttons go back and forward in <see cref="Navigation" />, anywhere in the
    ///     window, unless a dialog is open. <see langword="true" /> by default.
    /// </summary>
    public bool IsMouseNavigationEnabled
    {
        get => GetValue(IsMouseNavigationEnabledProperty);
        set => SetValue(IsMouseNavigationEnabledProperty, value);
    }

    /// <summary>
    ///     Whether the content extends into the title bar. <see langword="true" /> by default.
    /// </summary>
    public bool ExtendsContentIntoTitleBar
    {
        get => GetValue(ExtendsContentIntoTitleBarProperty);
        set => SetValue(ExtendsContentIntoTitleBarProperty, value);
    }

    /// <summary>
    ///     The height of the title bar area the window asks the system for, so it places its window buttons there.
    ///     48 by default; the <see cref="TitleBar" /> has the same height.
    /// </summary>
    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    /// <summary>
    ///     Whether the window shows Mica on Windows 11. Other systems keep the window's background.
    ///     <see langword="true" /> by default.
    /// </summary>
    public bool IsMicaEnabled
    {
        get => GetValue(IsMicaEnabledProperty);
        set => SetValue(IsMicaEnabledProperty, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Window);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ExtendsContentIntoTitleBarProperty || change.Property == TitleBarHeightProperty)
        {
            ApplyTitleBar();
        }
        else if (change.Property == IsMicaEnabledProperty)
        {
            ApplyBackdrop();
        }
        else if (change.Property == ActualTransparencyLevelProperty)
        {
            UpdateBackground();
        }
    }

    private void ApplyTitleBar()
    {
        ExtendClientAreaToDecorationsHint = ExtendsContentIntoTitleBar;
        ExtendClientAreaTitleBarHeightHint = TitleBarHeight;
    }

    private void ApplyBackdrop() =>
        TransparencyLevelHint = IsMicaEnabled && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            ? [WindowTransparencyLevel.Mica]
            : [];

    /// <inheritdoc />
    private async void OnWindowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton is not (MouseButton.XButton1 or MouseButton.XButton2)
            || !IsMouseNavigationEnabled
            || Navigation is not { } navigation)
        {
            return;
        }

        var goesBack = e.InitialPressMouseButton == MouseButton.XButton1;
        if ((goesBack ? !navigation.CanGoBack : !navigation.CanGoForward) || HasOpenDialog())
        {
            return;
        }

        e.Handled = true;
        if (goesBack)
        {
            await navigation.GoBackAsync();
        }
        else
        {
            await navigation.GoForwardAsync();
        }
    }

    private bool HasOpenDialog() =>
        this.GetVisualDescendants().OfType<DialogHost>().Any(static host => host.Dialogs?.CurrentDialog is not null);

    private void UpdateBackground()
    {
        if (ActualTransparencyLevel == WindowTransparencyLevel.Mica)
        {
            _transparentBackground ??= SetValue(BackgroundProperty, Brushes.Transparent, BindingPriority.Animation);
        }
        else
        {
            _transparentBackground?.Dispose();
            _transparentBackground = null;
        }
    }
}
