namespace Fluently.Avalonia.UnitTests.TestDoubles.ViewModels;

public sealed class GreetingViewModel(Greeting greeting) : INavigableViewModel
{
    public Greeting Greeting { get; } = greeting;
}
