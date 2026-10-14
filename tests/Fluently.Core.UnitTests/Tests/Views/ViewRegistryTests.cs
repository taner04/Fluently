using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluently.Core.UnitTests.Tests.Views;

// The registry is static, so every test uses view models no other test registers.
public sealed class ViewRegistryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddScoped<Greeting>()
        .AddTransient<GreetedPage>()
        .BuildServiceProvider();

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void TryCreateView_Should_CreateRegisteredView()
    {
        ViewRegistry.Add<CreatedViewModel>(static (_, viewModel) => new CreatedView(viewModel));
        var viewModel = new CreatedViewModel();

        var created = ViewRegistry.TryCreateView(viewModel, out var view);

        created.Should().BeTrue();
        ViewRegistry.Contains(typeof(CreatedViewModel)).Should().BeTrue();
        view.Should().BeOfType<CreatedView>().Which.ViewModel.Should().BeSameAs(viewModel);
    }

    [Fact]
    public void TryCreateView_Should_ReturnFalse_When_ViewModelHasNoView()
    {
        var created = ViewRegistry.TryCreateView(new UnregisteredViewModel(), out var view);

        created.Should().BeFalse();
        view.Should().BeNull();
    }

    [Fact]
    public void Add_Should_Throw_When_ViewModelAlreadyHasView()
    {
        ViewRegistry.Add<DuplicateViewModel>(static (_, viewModel) => viewModel);

        var add = () => ViewRegistry.Add<DuplicateViewModel>(static (_, viewModel) => viewModel);

        add.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task TryCreateView_Should_UsePageScope_When_PageWasNavigatedTo()
    {
        ViewRegistry.Add<GreetedPage>(static (services, page) => new GreetedView(page, services.GetRequiredService<Greeting>()));
        using var navigation = new NavigationService(_serviceProvider, NullLogger<NavigationService>.Instance);
        await navigation.NavigateToAsync<GreetedPage>(TestContext.Current.CancellationToken);
        var page = (GreetedPage)navigation.CurrentPage!;

        ViewRegistry.TryCreateView(page, out var view);

        view.Should().BeOfType<GreetedView>().Which.Greeting.Should().BeSameAs(page.Greeting);
    }

    [Fact]
    public async Task For_Should_ForgetPageScope_When_PageLeftHistory()
    {
        using var navigation = new NavigationService(_serviceProvider, NullLogger<NavigationService>.Instance);
        await navigation.NavigateToAsync<GreetedPage>(TestContext.Current.CancellationToken);
        var page = navigation.CurrentPage!;
        var scope = ViewModelServices.For(page);

        navigation.Dispose();

        scope.Should().NotBeNull().And.NotBeSameAs(_serviceProvider);
        ViewModelServices.For(page).Should().NotBeSameAs(scope);
    }
}
