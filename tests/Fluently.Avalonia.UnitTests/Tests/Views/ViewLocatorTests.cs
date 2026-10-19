using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Views;

public sealed class ViewLocatorTests : TestBase, IDisposable
{
    private readonly INavigationService _navigation;
    private readonly ServiceProvider _serviceProvider = Services.Create();
    private readonly ViewLocator _viewLocator;

    public ViewLocatorTests()
    {
        _navigation = _serviceProvider.GetRequiredService<INavigationService>();
        _viewLocator = new ViewLocator { Provider = _serviceProvider.GetRequiredService<ViewProvider>() };
    }

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
        UiThread.Run(() =>
        {
            var viewModel = new HomeViewModel();

            var view = _viewLocator.Build(viewModel);

            view.Should().BeOfType<HomeView>().Which.ViewModel.Should().BeSameAs(viewModel);
        });

    [Fact]
    public Task Build_Should_UseViewModelAsDataContext_When_ViewDerivesFromViewFor() =>
        UiThread.Run(() =>
        {
            var viewModel = new HomeViewModel();

            var view = _viewLocator.Build(viewModel);

            view.Should().BeAssignableTo<ViewFor<HomeViewModel>>()
                .Which.DataContext.Should().BeSameAs(viewModel);
        });

    [Fact]
    public Task Build_Should_ReuseView_When_SameViewModelInstance() =>
        UiThread.Run(() =>
        {
            var viewModel = new HomeViewModel();

            var first = _viewLocator.Build(viewModel);
            var second = _viewLocator.Build(viewModel);
            var other = _viewLocator.Build(new HomeViewModel());

            second.Should().BeSameAs(first);
            other.Should().NotBeSameAs(first);
        });

    [Fact]
    public Task Build_Should_ResolveViewServicesFromPageScope_When_PageWasNavigatedTo() =>
        UiThread.RunAsync(async () =>
        {
            await _navigation.NavigateToAsync<GreetingViewModel>(CurrentCancellationToken);
            var page = (GreetingViewModel)_navigation.CurrentPage!;

            var view = _viewLocator.Build(page);

            view.Should().BeOfType<GreetingView>().Which.Greeting.Should().BeSameAs(page.Greeting);
        });

    [Fact]
    public Task Build_Should_ResolveViewServicesFromRoot_When_FluentlyDidNotCreateViewModel() =>
        UiThread.Run(() =>
        {
            var view = _viewLocator.Build(new ClockViewModel());

            view.Should().BeOfType<ClockView>()
                .Which.Clock.Should().BeSameAs(_serviceProvider.GetRequiredService<Clock>());
        });
}
