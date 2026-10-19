namespace Fluently.Avalonia.UnitTests.TestDoubles.ViewModels;

[SingletonPage]
public sealed class GuardedPageViewModel : INavigableViewModel, ICanNavigateFrom
{
    public bool CanLeave { get; set; }

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken) => Task.FromResult(CanLeave);
}
