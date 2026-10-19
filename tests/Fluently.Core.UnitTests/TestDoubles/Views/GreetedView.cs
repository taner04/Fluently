namespace Fluently.Core.UnitTests.TestDoubles.Views;

internal sealed class GreetedView(GreetedPage viewModel, Greeting greeting) : IViewFor<GreetedPage>
{
    public Greeting Greeting { get; } = greeting;
    public GreetedPage ViewModel { get; } = viewModel;
}
