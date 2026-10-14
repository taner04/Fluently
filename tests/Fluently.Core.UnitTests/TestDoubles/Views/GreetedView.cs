namespace Fluently.Core.UnitTests.TestDoubles.Views;

internal sealed class GreetedView(GreetedPage viewModel, Greeting greeting) : IViewFor<GreetedPage>
{
    public GreetedPage ViewModel { get; } = viewModel;

    public Greeting Greeting { get; } = greeting;
}
