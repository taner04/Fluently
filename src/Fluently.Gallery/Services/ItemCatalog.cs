using Fluently.Gallery.Models;
using System.Collections.ObjectModel;

namespace Fluently.Gallery.Services;

public sealed class ItemCatalog
{
    public ObservableCollection<SampleItem> Items { get; } = [];

    public SampleItem? Find(int id) => Items.FirstOrDefault(item => item.Id == id);

    public void Reset()
    {
        Items.Clear();
        Items.Add(new SampleItem(1, "Typed parameters",
            "Opened with NavigateToAsync<TPage, TParameter>, checked at compile time."));
        Items.Add(new SampleItem(2, "History", "Back and forward keep every page and its state."));
        Items.Add(new SampleItem(3, "Scoped pages", "Every history entry resolves from its own service scope."));
        Items.Add(new SampleItem(4, "Rollback", "A page that throws while entering is left again."));
        Items.Add(new SampleItem(5, "Dialogs", "Delete an item to see a dialog that returns a result."));
    }
}
