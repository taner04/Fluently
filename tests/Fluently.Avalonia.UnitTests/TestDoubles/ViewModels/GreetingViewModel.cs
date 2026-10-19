namespace Fluently.Avalonia.UnitTests.TestDoubles.ViewModels;

[TransientPage]
public sealed class GreetingViewModel(Greeting greeting) : INavigableViewModel
{
    public Greeting Greeting { get; } = greeting;
}
