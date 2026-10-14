namespace Fluently.Core.UnitTests.TestDoubles.Pages;

internal sealed class AsyncResourcePage(AsyncOnlyResource resource) : INavigableViewModel
{
    internal AsyncOnlyResource Resource { get; } = resource;
}
