namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class EnterOnlyPage : INavigatedTo<int>
{
    internal int? Parameter { get; private set; }

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
    {
        Parameter = parameter;
        return Task.CompletedTask;
    }
}
