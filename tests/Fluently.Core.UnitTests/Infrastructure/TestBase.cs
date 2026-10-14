namespace Fluently.Core.UnitTests.Infrastructure;

public abstract class TestBase
{
    protected static CancellationToken CurrentCancellationToken => TestContext.Current.CancellationToken;
}