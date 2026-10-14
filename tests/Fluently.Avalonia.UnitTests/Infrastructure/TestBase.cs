namespace Fluently.Avalonia.UnitTests.Infrastructure;

public abstract class TestBase
{
    protected static CancellationToken CurrentCancellationToken => TestContext.Current.CancellationToken;
}