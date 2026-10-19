namespace Fluently.Core.UnitTests.TestDoubles.Toasts;

internal sealed class SelfDismissingToast : ToastViewModel
{
    internal bool DismissSelf() => Dismiss();
}
