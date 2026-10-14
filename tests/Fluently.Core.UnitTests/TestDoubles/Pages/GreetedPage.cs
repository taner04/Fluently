namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class GreetedPage(Greeting greeting) : INavigableViewModel
{
    public Greeting Greeting { get; } = greeting;
}
