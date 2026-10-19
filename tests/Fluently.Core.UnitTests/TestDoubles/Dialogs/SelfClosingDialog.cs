namespace Fluently.Core.UnitTests.TestDoubles.Dialogs;

internal sealed class SelfClosingDialog : DialogViewModel<string>
{
    internal Task<bool> CloseWithAsync(string result) => CloseAsync(result);

    internal Task<bool> DismissSelfAsync() => DismissAsync();

    internal Task<bool> RequestDismissSelfAsync() => RequestDismissAsync();
}
