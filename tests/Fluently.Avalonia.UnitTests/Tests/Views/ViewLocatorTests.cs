using Fluently.Avalonia.UnitTests.Infrastructure;
using Fluently.Avalonia.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Views;

public sealed class ViewLocatorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider = Services.Create();
    private readonly ViewLocator _viewLocator;

    public ViewLocatorTests() => _viewLocator = _serviceProvider.GetRequiredService<ViewLocator>();

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void Match_Should_ReturnTrue_When_ViewModelHasView() =>
        _viewLocator.Match(new HomeViewModel()).Should().BeTrue();

    [Fact]
    public void Match_Should_ReturnFalse_When_ViewModelHasNoView()
    {
        _viewLocator.Match(new ViewModelWithoutView()).Should().BeFalse();
        _viewLocator.Match(null).Should().BeFalse();
    }

    [Fact]
    public Task Build_Should_CreateViewWithViewModel() =>
        UiThread.RunAsync(() =>
        {
            var viewModel = new HomeViewModel();

            var view = _viewLocator.Build(viewModel);

            view.Should().BeOfType<HomeView>().Which.ViewModel.Should().BeSameAs(viewModel);
            return Task.CompletedTask;
        });

    [Fact]
    public Task Build_Should_ReuseView_When_SameViewModelInstance() =>
        UiThread.RunAsync(() =>
        {
            var viewModel = new HomeViewModel();

            var first = _viewLocator.Build(viewModel);
            var second = _viewLocator.Build(viewModel);
            var other = _viewLocator.Build(new HomeViewModel());

            second.Should().BeSameAs(first);
            other.Should().NotBeSameAs(first);
            return Task.CompletedTask;
        });

    [Fact]
    public void Add_Should_Throw_When_ViewModelAlreadyHasView()
    {
        var add = () => new ServiceCollection().AddFluentlyAvalonia(views => views
            .Add<HomeView, HomeViewModel>()
            .Add<HomeView, HomeViewModel>());

        add.Should().Throw<InvalidOperationException>();
    }
}
