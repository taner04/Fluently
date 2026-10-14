namespace Fluently.Core.UnitTests.TestDoubles.Services;

internal sealed class AsyncOnlyResource : IAsyncDisposable
{
    internal bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
