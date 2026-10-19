namespace Fluently;

internal static class SemaphoreSlimExtensions
{
    extension(SemaphoreSlim semaphore)
    {
        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                semaphore.Release();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
