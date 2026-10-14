using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class ClockView(ClockViewModel viewModel, Clock clock) : UserControl, IViewFor<ClockViewModel>
{
    public ClockViewModel ViewModel { get; } = viewModel;

    public Clock Clock { get; } = clock;
}
