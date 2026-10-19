namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class ClockToastView(ClockToastViewModel viewModel, Clock clock) : ViewFor<ClockToastViewModel>(viewModel)
{
    public Clock Clock { get; } = clock;
}
