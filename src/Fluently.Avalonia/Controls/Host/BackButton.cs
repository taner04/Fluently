using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;

namespace Fluently;

/// <summary>
///     The back button of <see cref="TitleBar" /> and <see cref="NavigationPane" />: enabled while its navigation service
///     can go back, and goes back when clicked.
/// </summary>
internal sealed class BackButton : Button
{
    private readonly ServiceFollower<INavigationService> _follower;
    private INavigationService? _navigation;

    public BackButton(string styleClass)
    {
        _follower = new ServiceFollower<INavigationService>(
            navigation => navigation.PropertyChanged += OnNavigationChanged,
            navigation => navigation.PropertyChanged -= OnNavigationChanged,
            _ => Update());
        SubtleButton.Apply(this, Glyphs.Back, "Back", styleClass);
        IsEnabled = false;
        Click += OnClicked;
    }

    public INavigationService? Navigation
    {
        get => _navigation;
        set
        {
            _navigation = value;
            _follower.Change(value);
        }
    }

    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _follower.Attach(_navigation);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _follower.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    private async void OnClicked(object? sender, RoutedEventArgs e)
    {
        if (_follower.Current is { CanGoBack: true } navigation)
        {
            await navigation.GoBackAsync();
        }
    }

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CanGoBack))
        {
            HostDispatcher.Run(Update);
        }
    }

    private void Update() => IsEnabled = _follower.Current?.CanGoBack == true;
}
