namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class LeaveOnlyPage : INavigatedFrom
{
    internal int LeaveCount { get; private set; }

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        LeaveCount++;
        return Task.CompletedTask;
    }
}
