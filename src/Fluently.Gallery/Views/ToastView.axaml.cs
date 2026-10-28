using Avalonia.Interactivity;

namespace Fluently.Gallery.Views;

internal partial class ToastView : ViewFor<Toast>
{
    private readonly IToastService _toasts;

    public ToastView(Toast viewModel, IToastService toasts) : base(viewModel)
    {
        _toasts = toasts;
        InitializeComponent();
        Card.Classes.Add(viewModel.Severity.ToString().ToLowerInvariant());
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => _toasts.Dismiss(ViewModel);
}
