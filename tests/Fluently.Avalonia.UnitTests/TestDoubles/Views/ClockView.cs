namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class ClockView(ClockViewModel viewModel, Clock clock) : ViewFor<ClockViewModel>(viewModel)
{
    public Clock Clock { get; } = clock;
}
