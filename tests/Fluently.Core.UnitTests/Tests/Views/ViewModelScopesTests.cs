using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Core.UnitTests.Tests.Views;

public sealed class ViewModelScopesTests : IDisposable
{
    private readonly object _provider = new();
    private readonly ServiceProvider _root = new ServiceCollection().BuildServiceProvider();
    private readonly ViewModelScopes _scopes;
    private readonly object _viewModel = new();

    public ViewModelScopesTests()
    {
        _scopes = new ViewModelScopes(_root);
    }

    public void Dispose() => _root.Dispose();

    [Fact]
    public void GetView_Should_ReuseView_While_ScopeLives()
    {
        using var scope = _root.CreateScope();
        _scopes.Track(_viewModel, scope.ServiceProvider);

        var first = _scopes.GetView(_viewModel, _provider, static _ => new object());
        _scopes.Track(_viewModel, scope.ServiceProvider);

        _scopes.GetView(_viewModel, _provider, static _ => new object()).Should().BeSameAs(first);
    }

    [Fact]
    public void GetView_Should_CreateNewView_When_ViewModelGetsNewScope()
    {
        using var firstScope = _root.CreateScope();
        using var secondScope = _root.CreateScope();
        _scopes.Track(_viewModel, firstScope.ServiceProvider);
        var first = _scopes.GetView(_viewModel, _provider, static _ => new object());

        _scopes.Track(_viewModel, secondScope.ServiceProvider);

        _scopes.GetView(_viewModel, _provider, static _ => new object()).Should().NotBeSameAs(first);
    }

    [Fact]
    public void GetView_Should_CreateNewView_When_ScopeWasReleased()
    {
        using var scope = _root.CreateScope();
        _scopes.Track(_viewModel, scope.ServiceProvider);
        var first = _scopes.GetView(_viewModel, _provider, static _ => new object());

        _scopes.Untrack(_viewModel, scope.ServiceProvider);

        _scopes.GetView(_viewModel, _provider, static _ => new object()).Should().NotBeSameAs(first);
        _scopes.For(_viewModel).Should().BeSameAs(_root);
    }

    [Fact]
    public void GetView_Should_CreateNewView_When_ProviderDiffers()
    {
        var first = _scopes.GetView(_viewModel, _provider, static _ => new object());

        _scopes.GetView(_viewModel, new object(), static _ => new object()).Should().NotBeSameAs(first);
    }

    [Fact]
    public void GetView_Should_NotCacheView_When_ScopeChangedWhileCreating()
    {
        using var firstScope = _root.CreateScope();
        using var secondScope = _root.CreateScope();
        _scopes.Track(_viewModel, firstScope.ServiceProvider);

        var stale = _scopes.GetView(_viewModel, _provider, _ =>
        {
            _scopes.Track(_viewModel, secondScope.ServiceProvider);
            return new object();
        });

        _scopes.GetView(_viewModel, _provider, static _ => new object()).Should().NotBeSameAs(stale);
    }
}
