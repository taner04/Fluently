using Fluently.Gallery.ViewModels;

namespace Fluently.Gallery.Views;

internal partial class SettingsView : ViewFor<SettingsViewModel>
{
    public SettingsView(SettingsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
