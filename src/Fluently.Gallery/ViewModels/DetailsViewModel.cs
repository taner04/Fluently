using CommunityToolkit.Mvvm.ComponentModel;
using Fluently.Gallery.Models;
using Fluently.Gallery.Services;

namespace Fluently.Gallery.ViewModels;

[TransientPage]
public sealed partial class DetailsViewModel(ItemCatalog catalog) : ObservableObject, INavigatedTo<int>
{
    [ObservableProperty] public partial SampleItem? Item { get; private set; }

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
    {
        Item = catalog.Find(parameter);
        return Task.CompletedTask;
    }
}
