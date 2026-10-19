using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using System.ComponentModel;

namespace Fluently;

/// <summary>
///     Shows its <see cref="OverlayHost.Content" /> and, above it at the service's <see cref="IToastService.Position" />,
///     the
///     toasts of an <see cref="IToastService" />, the newest nearest the edge. A built-in <see cref="Toast" /> gets a card
///     with its title, message and a close button, unless the toast service's container has a view for it; any other
///     <see cref="IToast" /> gets its view from that container. Toasts do not block the content: only the toasts
///     themselves take input.
///     Place it outside a <see cref="DialogHost" /> to show toasts above open dialogs.
///     <code>
/// &lt;fluently:ToastHost Toasts="{Binding Toasts}"&gt;
///     &lt;fluently:DialogHost Dialogs="{Binding Dialogs}"&gt;
///         &lt;fluently:NavigationHost Navigation="{Binding Navigation}" /&gt;
///     &lt;/fluently:DialogHost&gt;
/// &lt;/fluently:ToastHost&gt;
/// </code>
/// </summary>
public class ToastHost : OverlayHost
{
    private const double EdgeDistance = 20;
    private const double SlideDistance = 64;

    /// <summary>
    ///     Defines the <see cref="Toasts" /> property.
    /// </summary>
    public static readonly StyledProperty<IToastService?> ToastsProperty =
        AvaloniaProperty.Register<ToastHost, IToastService?>(nameof(Toasts));

    private readonly Dictionary<IToast, ToastItem> _items = new(ReferenceEqualityComparer.Instance);
    private readonly ToastPanel _panel = new();

    /// <summary>
    ///     Creates a toast host.
    /// </summary>
    public ToastHost()
    {
        _panel.Classes.Add("toasts");
        ApplyPosition();
        Root.Children.Add(_panel);
    }

    /// <summary>
    ///     The toast service whose toasts are shown.
    /// </summary>
    public IToastService? Toasts
    {
        get => GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    private protected override INotifyPropertyChanged? Service => Toasts;

    private IToastService? Following => (IToastService?)Subscribed;

    private ToastPosition Position => Following?.Position ?? ToastPosition.TopRight;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ToastsProperty)
        {
            OnServicePropertyChanged();
        }
    }

    private protected override void OnFollowed()
    {
        ApplyPosition();
        Synchronize();
    }

    private protected override void OnServiceChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(IToastService.Toasts):
                Synchronize();
                break;
            case nameof(IToastService.Position):
                ApplyPosition();
                break;
        }
    }

    private void Synchronize()
    {
        var toasts = Following?.Toasts ?? [];

        foreach (var toast in _items.Keys.Where(toast => !toasts.Contains(toast, ReferenceEqualityComparer.Instance)).ToList())
        {
            var item = _items[toast];
            _items.Remove(toast);
            _ = RemoveAfterExitAsync(item);
        }

        for (var index = 0; index < toasts.Count; index++)
        {
            if (!_items.TryGetValue(toasts[index], out var item))
            {
                item = _panel.Children.OfType<ToastItem>()
                           .FirstOrDefault(leaving =>
                               leaving.IsLeaving && ReferenceEquals(leaving.Toast, toasts[index]))
                       ?? new ToastItem(toasts[index], CreateView(toasts[index]), SlideFrom());
                if (item.IsLeaving)
                {
                    item.Return();
                }

                _items.Add(toasts[index], item);
            }

            var current = _panel.Children.IndexOf(item);
            var target = PanelIndexOf(index, item);
            if (current < 0)
            {
                _panel.Children.Insert(target, item);
            }
            else if (current != target)
            {
                _panel.Children.Move(current, target);
            }
        }
    }

    private int PanelIndexOf(int index, ToastItem placed)
    {
        var shown = 0;
        var position = 0;
        foreach (var child in _panel.Children)
        {
            if (ReferenceEquals(child, placed))
            {
                continue;
            }

            if (child is not ToastItem { IsLeaving: true })
            {
                if (shown == index)
                {
                    return position;
                }

                shown++;
            }

            position++;
        }

        return position;
    }

    private void ApplyPosition()
    {
        var position = Position;
        var isTop = position is ToastPosition.TopRight or ToastPosition.TopLeft or ToastPosition.TopCenter;
        var horizontal = position switch
        {
            ToastPosition.TopLeft or ToastPosition.BottomLeft => HorizontalAlignment.Left,
            ToastPosition.TopCenter or ToastPosition.BottomCenter => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Right
        };

        _panel.HorizontalAlignment = horizontal;
        _panel.VerticalAlignment = isTop ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        _panel.Margin = new Thickness(
            horizontal == HorizontalAlignment.Left ? EdgeDistance : 0,
            isTop ? EdgeDistance : 0,
            horizontal == HorizontalAlignment.Right ? EdgeDistance : 0,
            isTop ? 0 : EdgeDistance);
        _panel.StacksFromBottom = !isTop;
        _panel.InvalidateArrange();
    }

    private Vector SlideFrom() => Position switch
    {
        ToastPosition.TopLeft or ToastPosition.BottomLeft => new Vector(-SlideDistance, 0),
        ToastPosition.TopCenter => new Vector(0, -SlideDistance),
        ToastPosition.BottomCenter => new Vector(0, SlideDistance),
        _ => new Vector(SlideDistance, 0)
    };

    private async Task RemoveAfterExitAsync(ToastItem item)
    {
        if (!await item.LeaveAsync())
        {
            return;
        }

        _panel.Children.Remove(item);

        if (item.Child is ContentPresenter presenter)
        {
            presenter.Content = null;
        }
    }

    private Control CreateView(IToast toast) =>
        toast is Toast builtIn && !ViewProviders.For((Following as IViewSource)?.Scopes).CanProvide(builtIn)
            ? new ToastView(builtIn, () => Dismiss(builtIn))
            : new ContentPresenter { Content = HostViews.For(Following, toast) };

    private void Dismiss(IToast toast) => Following?.Dismiss(toast);
}
