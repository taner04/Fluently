using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluently.Core.UnitTests.Tests.Views;

// The registry is static, so every test uses view models no other test registers.
public sealed class ViewRegistryTests : IDisposable
{
    private readonly ViewProvider _provider = new(new ViewModelScopes(null));

    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddScoped<Greeting>()
        .AddTransient<GreetedPage>()
        .BuildServiceProvider();

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void GetView_Should_CreateRegisteredView()
    {
        ViewRegistry.Add<CreatedViewModel>(static (_, viewModel) => new CreatedView(viewModel));
        var viewModel = new CreatedViewModel();

        var view = _provider.GetView(viewModel);

        _provider.CanProvide(viewModel).Should().BeTrue();
        view.Should().BeOfType<CreatedView>().Which.ViewModel.Should().BeSameAs(viewModel);
    }

    [Fact]
    public void GetView_Should_Throw_When_ViewModelHasNoView()
    {
        var viewModel = new UnregisteredViewModel();

        var get = () => _provider.GetView(viewModel);

        _provider.CanProvide(viewModel).Should().BeFalse();
        get.Should().Throw<InvalidOperationException>().WithMessage("*UnregisteredViewModel*");
    }

    [Fact]
    public void Add_Should_Throw_When_ViewModelAlreadyHasView()
    {
        ViewRegistry.Add<DuplicateViewModel>(static (_, viewModel) => new DuplicateView(viewModel));

        var add = () => ViewRegistry.Add<DuplicateViewModel>(static (_, viewModel) => new DuplicateView(viewModel));

        add.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task GetView_Should_UsePageScope_When_PageWasNavigatedTo()
    {
        ViewRegistry.Add<GreetedPage>(static (services, page) =>
            new GreetedView(page, services.GetRequiredService<Greeting>()));
        await using var navigation = new NavigationService(_serviceProvider, NullLogger<NavigationService>.Instance);
        await navigation.NavigateToAsync<GreetedPage>(TestContext.Current.CancellationToken);
        var page = (GreetedPage)navigation.CurrentPage!;

        var view = new ViewProvider(navigation.Scopes).GetView(page);

        view.Should().BeOfType<GreetedView>().Which.Greeting.Should().BeSameAs(page.Greeting);
    }

    [Fact]
    public async Task For_Should_ForgetPageScope_When_PageLeftHistory()
    {
        var navigation = new NavigationService(_serviceProvider, NullLogger<NavigationService>.Instance);
        await navigation.NavigateToAsync<GreetedPage>(TestContext.Current.CancellationToken);
        var page = navigation.CurrentPage!;
        var scope = navigation.Scopes.For(page);

        await navigation.DisposeAsync();

        scope.Should().NotBeNull().And.NotBeSameAs(_serviceProvider);
        navigation.Scopes.For(page).Should().BeSameAs(_serviceProvider);
    }
}
