using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class GreetingView(GreetingViewModel viewModel, Greeting greeting)
    : UserControl, IViewFor<GreetingViewModel>
{
    public Greeting Greeting { get; } = greeting;
    public GreetingViewModel ViewModel { get; } = viewModel;
}
