using Fluently.Avalonia.UnitTests.Infrastructure;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class ViewForTests : TestBase
{
    [Fact]
    public Task Constructor_Should_Throw_When_ViewModelIsNull() =>
        UiThread.Run(() =>
        {
            var create = () => new HomeView(null!);

            create.Should().Throw<ArgumentNullException>().WithParameterName("viewModel");
        });
}
