using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using System.ComponentModel;

namespace Fluently;

/// <summary>
///     Shows its <see cref="OverlayHost.Content" /> and, on top of it, the open dialogs of an
///     <see cref="IDialogService" />.
///     Every dialog gets a layer with a <see cref="Backdrop" />, transparent when it is null, that covers everything below
///     it and the dialog centered above; each dialog's view comes from the container of the dialog service.
///     Pressing Escape asks the top dialog to close with <see cref="IDialogService.RequestDismissAsync" />, and so does
///     clicking the backdrop when <see cref="DismissOnBackdropClick" /> is set.
///     The backdrop has the style class <c>dialog-backdrop</c> and the dialog's presenter the class <c>dialog</c>.
///     <code>
/// &lt;fluently:DialogHost Dialogs="{Binding Dialogs}"&gt;
///     &lt;fluently:NavigationHost Navigation="{Binding Navigation}" /&gt;
/// &lt;/fluently:DialogHost&gt;
/// </code>
/// </summary>
public class DialogHost : OverlayHost
{
    /// <summary>
    ///     Defines the <see cref="Dialogs" /> property.
    /// </summary>
    public static readonly StyledProperty<IDialogService?> DialogsProperty =
        AvaloniaProperty.Register<DialogHost, IDialogService?>(nameof(Dialogs));

    /// <summary>
    ///     Defines the <see cref="Backdrop" /> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackdropProperty =
        AvaloniaProperty.Register<DialogHost, IBrush?>(
            nameof(Backdrop),
            new ImmutableSolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)));

    /// <summary>
    ///     Defines the <see cref="DismissOnBackdropClick" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> DismissOnBackdropClickProperty =
        AvaloniaProperty.Register<DialogHost, bool>(nameof(DismissOnBackdropClick));

    private readonly List<DialogLayer> _layers = [];

    /// <summary>
    ///     Creates a dialog host.
    /// </summary>
    public DialogHost()
    {
        AddHandler(KeyDownEvent, OnKeyDown);
    }

    /// <summary>
    ///     The dialog service whose open dialogs are shown.
    /// </summary>
    public IDialogService? Dialogs
    {
        get => GetValue(DialogsProperty);
        set => SetValue(DialogsProperty, value);
    }

    /// <summary>
    ///     The brush that covers everything below a dialog. Defaults to a translucent black.
    /// </summary>
    public IBrush? Backdrop
    {
        get => GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    /// <summary>
    ///     Whether clicking the backdrop asks the top dialog to close. Defaults to <see langword="false" />.
    /// </summary>
    public bool DismissOnBackdropClick
    {
        get => GetValue(DismissOnBackdropClickProperty);
        set => SetValue(DismissOnBackdropClickProperty, value);
    }

    private protected override INotifyPropertyChanged? Service => Dialogs;

    private IDialogService? Following => (IDialogService?)Subscribed;

    private IBrush BackdropOrTransparent => Backdrop ?? Brushes.Transparent;

    private DialogLayer? TopLayer => _layers.Count > 0 ? _layers[^1] : null;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DialogsProperty)
        {
            OnServicePropertyChanged();
        }
        else if (change.Property == BackdropProperty)
        {
            foreach (var layer in _layers)
            {
                layer.Backdrop.Background = BackdropOrTransparent;
            }
        }
    }

    private protected override void OnFollowed() => Synchronize();

    private protected override void OnServiceChanged(string? propertyName)
    {
        if (propertyName == nameof(IDialogService.Dialogs))
        {
            Synchronize();
        }
    }

    private void Synchronize()
    {
        var dialogs = Following?.Dialogs ?? [];

        for (var index = _layers.Count - 1; index >= 0; index--)
        {
            if (!dialogs.Contains(_layers[index].Dialog, ReferenceEqualityComparer.Instance))
            {
                Close(_layers[index]);
            }
        }

        foreach (var dialog in dialogs)
        {
            if (!_layers.Exists(layer => ReferenceEquals(layer.Dialog, dialog)))
            {
                Open(dialog);
            }
        }
    }

    private void Open(IDialog dialog)
    {
        var layer = new DialogLayer(
            dialog,
            HostViews.For(Following, dialog),
            TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement());
        layer.Backdrop.Background = BackdropOrTransparent;
        layer.Backdrop.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            if (DismissOnBackdropClick && ReferenceEquals(TopLayer, layer))
            {
                RequestDismiss(layer);
            }
        };

        _layers.Add(layer);
        Root.Children.Add(layer);
        Dispatcher.UIThread.Post(() => layer.Focus(), DispatcherPriority.Loaded);
    }

    private void Close(DialogLayer layer)
    {
        var wasOnTop = ReferenceEquals(TopLayer, layer);
        _layers.Remove(layer);
        Root.Children.Remove(layer);

        layer.Presenter.Content = null;

        if (!wasOnTop)
        {
            return;
        }

        if (layer.PreviousFocus is InputElement previous && TopLevel.GetTopLevel(previous) is not null)
        {
            previous.Focus();
        }
        else
        {
            TopLayer?.Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && TopLayer is { } layer)
        {
            e.Handled = true;
            RequestDismiss(layer);
        }
    }

    private void RequestDismiss(DialogLayer layer) => _ = Following?.RequestDismissAsync(layer.Dialog);
}
