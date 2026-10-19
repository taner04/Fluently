namespace Fluently.Core.UnitTests.TestDoubles.Dialogs;

internal sealed class NameDialog(Greeting greeting) : IDialog<string>, INavigatedTo, INavigatedFrom, IDisposable
{
    internal List<string> Calls { get; } = [];

    internal Greeting Greeting { get; } = greeting;

    internal Func<CancellationToken, Task>? Entering { get; set; }

    internal Func<Task>? Leaving { get; set; }

    internal bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        Calls.Add("from");
        return Leaving?.Invoke() ?? Task.CompletedTask;
    }

    public Task OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        Calls.Add("to");
        return Entering?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }
}
