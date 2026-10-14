using CommunityToolkit.Mvvm.ComponentModel;

namespace Fluently.Samples.ViewModels;

public sealed partial class DetailsViewModel : ObservableObject, INavigatedTo<int>
{
    [ObservableProperty] public partial int Item { get; private set; }

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
    {
        Item = parameter;
        return Task.CompletedTask;
    }
}
