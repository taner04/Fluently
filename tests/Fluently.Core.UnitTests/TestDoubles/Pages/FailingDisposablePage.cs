namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class FailingDisposablePage : DisposablePage
{
    public override Task OnNavigatedToAsync(CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException());
}
