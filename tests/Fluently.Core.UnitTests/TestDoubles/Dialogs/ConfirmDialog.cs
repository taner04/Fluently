namespace Fluently.Core.UnitTests.TestDoubles.Dialogs;

internal sealed class ConfirmDialog : IDialog<bool>, INavigatedTo<string>
{
    internal string? Message { get; private set; }

    public Task OnNavigatedToAsync(string parameter, CancellationToken cancellationToken)
    {
        Message = parameter;
        return Task.CompletedTask;
    }
}
