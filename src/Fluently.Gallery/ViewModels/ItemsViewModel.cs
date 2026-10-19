using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;
using Fluently.Gallery.Services;
using System.Collections.ObjectModel;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class ItemsViewModel : INavigableViewModel
{
    private readonly ItemCatalog _catalog;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IToastService _toasts;

    public ItemsViewModel(INavigationService navigation, IDialogService dialogs, IToastService toasts,
        ItemCatalog catalog)
    {
        _navigation = navigation;
        _dialogs = dialogs;
        _toasts = toasts;
        _catalog = catalog;
        _catalog.Reset();
    }

    public ObservableCollection<SampleItem> Items => _catalog.Items;

    [RelayCommand]
    private Task OpenAsync(SampleItem item) => _navigation.NavigateToAsync<DetailsViewModel, int>(item.Id);

    [RelayCommand]
    private async Task DeleteAsync(SampleItem item)
    {
        var request = new ConfirmRequest("Delete item?", $"\"{item.Name}\" will be removed from the list.", "Delete");
        var result = await _dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request);
        if (result.Value)
        {
            var index = _catalog.Items.IndexOf(item);
            _catalog.Items.Remove(item);
            _toasts.Show(new UndoToastViewModel(_catalog, item, index));
        }
    }
}
