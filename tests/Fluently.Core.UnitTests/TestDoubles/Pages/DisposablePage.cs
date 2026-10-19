namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal class DisposablePage : INavigatedTo, INavigatedTo<int>, IDisposable
{
    internal bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

    public virtual Task OnNavigatedToAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken) => Task.CompletedTask;
}
