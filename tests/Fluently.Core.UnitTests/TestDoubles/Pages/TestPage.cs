namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal abstract class TestPage : INavigatedTo, INavigatedTo<int>, INavigatedFrom
{
    internal List<string> Calls { get; } = [];

    internal int? Parameter { get; private set; }

    internal Func<CancellationToken, Task>? Entering { get; set; }

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        Calls.Add("from");
        return Task.CompletedTask;
    }

    public Task OnNavigatedToAsync(CancellationToken cancellationToken) => Enter(null, cancellationToken);

    public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken) =>
        Enter(parameter, cancellationToken);

    private Task Enter(int? parameter, CancellationToken cancellationToken)
    {
        Calls.Add("to");
        Parameter = parameter;
        return Entering?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }
}
