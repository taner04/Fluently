namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class TypedPage : INavigatedTo<int>, INavigatedTo<string>
{
    internal List<object> Parameters { get; } = [];

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
    {
        Parameters.Add(parameter);
        return Task.CompletedTask;
    }

    public Task OnNavigatedToAsync(string parameter, CancellationToken cancellationToken)
    {
        Parameters.Add(parameter);
        return Task.CompletedTask;
    }
}
