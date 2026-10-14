namespace Fluently.Core.UnitTests.TestDoubles.Views;

internal sealed class CreatedView(CreatedViewModel viewModel) : IViewFor<CreatedViewModel>
{
    public CreatedViewModel ViewModel { get; } = viewModel;
}
