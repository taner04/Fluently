namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class GuardedPage : TestPage, ICanNavigateFrom
{
    internal Func<Task<bool>> Guard { get; set; } = () => Task.FromResult(true);

    internal int GuardCalls { get; private set; }

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
    {
        GuardCalls++;
        return Guard();
    }
}
