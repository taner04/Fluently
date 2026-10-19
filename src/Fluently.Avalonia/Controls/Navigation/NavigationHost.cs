using Avalonia;
using Avalonia.Controls;

namespace Fluently;

/// <summary>
///     Shows the current page of an <see cref="INavigationService" /> and follows its navigation.
///     Each page's view, and the views of view models nested inside it, come from the container of the navigation service;
///     set <see cref="TransitioningContentControl.PageTransition" /> to animate page changes.
/// </summary>
public class NavigationHost : TransitioningContentControl
{
    /// <summary>
    ///     Defines the <see cref="Navigation" /> property.
    /// </summary>
    public static readonly StyledProperty<INavigationService?> NavigationProperty =
        AvaloniaProperty.Register<NavigationHost, INavigationService?>(nameof(Navigation));

    private readonly ServiceFollower<INavigationService> _follower;
    private readonly ViewLocator _views = new();

    /// <summary>
    ///     Creates a navigation host.
    /// </summary>
    public NavigationHost()
    {
        _follower = new ServiceFollower<INavigationService>(
            navigation => navigation.Navigated += OnNavigated,
            navigation => navigation.Navigated -= OnNavigated,
            Followed);
        DataTemplates.Add(_views);
    }

    /// <summary>
    ///     The navigation service whose current page is shown.
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
        _follower.Attach(Navigation);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _follower.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NavigationProperty)
        {
            _follower.Change(Navigation);
        }
    }

    private void Followed(INavigationService? navigation)
    {
        _views.Scopes = (navigation as IViewSource)?.Scopes;
        Content = HostViews.For(navigation, navigation?.CurrentPage);
    }

    private void OnNavigated(object? sender, NavigatedEventArgs e) =>
        HostDispatcher.Run(() => Content = HostViews.For(_follower.Current, _follower.Current?.CurrentPage));
}
