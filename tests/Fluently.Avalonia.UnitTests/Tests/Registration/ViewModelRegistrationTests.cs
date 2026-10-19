using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Registration;

public sealed class ViewModelRegistrationTests : TestBase, IDisposable
{
    private readonly ServiceProvider _serviceProvider = Services.Create();

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void AddViewModels_Should_RegisterSingletonPageOnce()
    {
        _serviceProvider.GetRequiredService<HomeViewModel>()
            .Should().BeSameAs(_serviceProvider.GetRequiredService<HomeViewModel>());
    }

    [Fact]
    public void AddViewModels_Should_RegisterTransientPagesDialogsAndToasts()
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        services.GetRequiredService<GreetingViewModel>().Should()
            .NotBeSameAs(services.GetRequiredService<GreetingViewModel>());
        services.GetRequiredService<ConfirmDialogViewModel>().Should()
            .NotBeSameAs(services.GetRequiredService<ConfirmDialogViewModel>());
        services.GetRequiredService<UndoToastViewModel>().Should()
            .NotBeSameAs(services.GetRequiredService<UndoToastViewModel>());
    }

    [Fact]
    public void AddViewModels_Should_KeepRegistrationsMadeByHand()
    {
        _serviceProvider.GetRequiredService<GuardedDialogViewModel>()
            .Should().BeSameAs(_serviceProvider.GetRequiredService<GuardedDialogViewModel>());
    }
}
