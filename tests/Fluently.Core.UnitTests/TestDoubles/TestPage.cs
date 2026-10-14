namespace Fluently.Core.UnitTests.TestDoubles;

internal abstract class TestPage : INavigatedTo, INavigatedFrom
{
    internal List<string> Calls { get; } = [];

    internal object? Parameter { get; private set; }

    internal Func<CancellationToken, Task>? Entering { get; set; }

    public Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        Calls.Add("to");
        Parameter = parameter;
        return Entering?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        Calls.Add("from");
        return Task.CompletedTask;
    }
}

internal sealed class FirstPage : TestPage;

internal sealed class SecondPage : TestPage;

internal sealed class EmptyPage : INavigableViewModel;

internal sealed class EnterOnlyPage : INavigatedTo
{
    internal object? Parameter { get; private set; }

    public Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        Parameter = parameter;
        return Task.CompletedTask;
    }
}

internal sealed class LeaveOnlyPage : INavigatedFrom
{
    internal int LeaveCount { get; private set; }

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        LeaveCount++;
        return Task.CompletedTask;
    }
}

internal class DisposablePage : INavigableViewModel, IDisposable
{
    internal bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

internal sealed class FailingDisposablePage : DisposablePage, INavigatedTo
{
    public Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException());
}

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

internal sealed class TypedPage : INavigatedTo<int>
{
    internal List<int> Parameters { get; } = [];

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
    {
        Parameters.Add(parameter);
        return Task.CompletedTask;
    }
}
