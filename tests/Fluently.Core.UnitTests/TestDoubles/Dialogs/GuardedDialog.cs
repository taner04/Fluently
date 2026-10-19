namespace Fluently.Core.UnitTests.TestDoubles.Dialogs;

internal sealed class GuardedDialog : IDialog<string>, ICanNavigateFrom
{
    internal Func<CancellationToken, Task<bool>> Guard { get; set; } = static _ => Task.FromResult(true);

    internal int GuardCalls { get; private set; }

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
    {
        GuardCalls++;
        return Guard(cancellationToken);
    }
}
