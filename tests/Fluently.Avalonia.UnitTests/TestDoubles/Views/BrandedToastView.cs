using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class BrandedToastView(object toast) : UserControl, IViewFor<object>
{
    public object ViewModel { get; } = toast;
}
