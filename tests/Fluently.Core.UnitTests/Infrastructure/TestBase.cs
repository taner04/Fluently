namespace Fluently.Core.UnitTests.Infrastructure;

public abstract class TestBase
{
    protected static CancellationToken CurrentCancellationToken => TestContext.Current.CancellationToken;

    protected static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(10, CurrentCancellationToken);
        }
    }
}
