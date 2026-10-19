using CommunityToolkit.Mvvm.Input;
using Fluently.Gallery.Models;
using Fluently.Gallery.Services;

namespace Fluently.Gallery.ViewModels;

public sealed partial class UndoToastViewModel(ItemCatalog catalog, SampleItem item, int index) : ToastViewModel
{
    public string Message { get; } = $"\"{item.Name}\" was deleted.";

    [RelayCommand]
    private void Undo()
    {
        if (!catalog.Items.Contains(item))
        {
            catalog.Items.Insert(Math.Min(index, catalog.Items.Count), item);
        }

        Dismiss();
    }
}
