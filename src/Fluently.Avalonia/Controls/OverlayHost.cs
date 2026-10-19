using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using System.ComponentModel;

namespace Fluently;

/// <summary>
///     The base of <see cref="DialogHost" /> and <see cref="ToastHost" />: shows its <see cref="Content" /> with an
///     overlay
///     above it, follows a service while it is attached to the visual tree, and handles the service's changes on the UI
///     thread. Views of view models nested inside it come from the container of the service it follows.
/// </summary>
public abstract class OverlayHost : Control
{
    /// <summary>
    ///     Defines the <see cref="Content" /> property.
    /// </summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<OverlayHost, Control?>(nameof(Content));

    private readonly ServiceFollower<INotifyPropertyChanged> _follower;
    private readonly ViewLocator _views = new();

    private protected OverlayHost()
    {
        _follower = new ServiceFollower<INotifyPropertyChanged>(
            service => service.PropertyChanged += OnServiceChanged,
            service => service.PropertyChanged -= OnServiceChanged,
            Followed);
        VisualChildren.Add(Root);
        LogicalChildren.Add(Root);
        DataTemplates.Add(_views);
    }

    /// <summary>
    ///     The content shown below the overlay.
    /// </summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private protected Panel Root { get; } = new();

    private protected INotifyPropertyChanged? Subscribed => _follower.Current;

    private protected abstract INotifyPropertyChanged? Service { get; }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _follower.Attach(Service);
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

        if (change.Property != ContentProperty)
        {
            return;
        }

        if (change.GetOldValue<Control?>() is { } oldContent)
        {
            Root.Children.Remove(oldContent);
        }

        if (change.GetNewValue<Control?>() is { } newContent)
        {
            Root.Children.Insert(0, newContent);
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        Root.Measure(availableSize);
        return Root.DesiredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        Root.Arrange(new Rect(finalSize));
        return finalSize;
    }

    private protected void OnServicePropertyChanged() => _follower.Change(Service);

    private protected abstract void OnFollowed();

    private protected abstract void OnServiceChanged(string? propertyName);

    private void Followed(INotifyPropertyChanged? service)
    {
        _views.Scopes = (service as IViewSource)?.Scopes;
        OnFollowed();
    }

    private void OnServiceChanged(object? sender, PropertyChangedEventArgs e) =>
        HostDispatcher.Run(() => OnServiceChanged(e.PropertyName));
}
