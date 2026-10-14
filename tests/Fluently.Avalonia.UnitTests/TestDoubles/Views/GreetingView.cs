using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.TestDoubles.Views;

public sealed class GreetingView(GreetingViewModel viewModel, Greeting greeting) : UserControl, IViewFor<GreetingViewModel>
{
    public GreetingViewModel ViewModel { get; } = viewModel;

    public Greeting Greeting { get; } = greeting;
}
