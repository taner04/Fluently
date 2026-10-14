using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Fluently;

/// <summary>
/// Shows the current page of an <see cref="INavigationService"/> and follows its navigation.
/// Pages are turned into views by the <see cref="ViewLocator"/> in your application's data templates;
/// set <see cref="TransitioningContentControl.PageTransition"/> to animate page changes.
/// </summary>
public class NavigationHost : TransitioningContentControl
{
    /// <summary>
    /// Defines the <see cref="Navigation"/> property.
    /// </summary>
    public static readonly StyledProperty<INavigationService?> NavigationProperty =
        AvaloniaProperty.Register<NavigationHost, INavigationService?>(nameof(Navigation));

    private bool _isAttached;
    private INavigationService? _subscribed;

    /// <summary>
    /// The navigation service whose current page is shown.
    /// </summary>
    public INavigationService? Navigation
    {
        get => GetValue(NavigationProperty);
        set => SetValue(NavigationProperty, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(TransitioningContentControl);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        Follow(Navigation);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Follow(null);
        _isAttached = false;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NavigationProperty && _isAttached)
        {
            Follow(Navigation);
        }
    }

    private void Follow(INavigationService? navigation)
    {
        if (_subscribed is not null)
        {
            _subscribed.Navigated -= OnNavigated;
        }

        _subscribed = navigation;
        if (navigation is not null)
        {
            navigation.Navigated += OnNavigated;
        }

        Content = navigation?.CurrentPage;
    }

    private void OnNavigated(object? sender, NavigatedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Content = e.Page;
        }
        else
        {
            Dispatcher.UIThread.Post(() => Content = _subscribed?.CurrentPage);
        }
    }
}
