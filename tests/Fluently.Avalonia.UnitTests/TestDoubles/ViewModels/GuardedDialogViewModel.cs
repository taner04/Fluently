namespace Fluently.Avalonia.UnitTests.TestDoubles.ViewModels;

[Dialog]
public sealed class GuardedDialogViewModel : IDialog<bool>, ICanNavigateFrom
{
    public bool CanClose { get; set; } = true;

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken) => Task.FromResult(CanClose);
}
