using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Fluently.Core.UnitTests.Infrastructure;

namespace Fluently.Core.UnitTests.Tests.Services.Navigation;

public sealed class NavigationServiceTests : TestBase, IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<DisposablePage> _created = [];
    private readonly EnterOnlyPage _enterOnly = new();
    private readonly FirstPage _first = new();
    private readonly GuardedPage _guarded = new();
    private readonly LeaveOnlyPage _leaveOnly = new();
    private readonly List<INavigableViewModel> _navigated = [];
    private readonly NavigationService _navigationService;
    private readonly SecondPage _second = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly TypedPage _typed = new();

    public NavigationServiceTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(_first)
            .AddSingleton(_second)
            .AddSingleton<EmptyPage>()
            .AddSingleton(_enterOnly)
            .AddSingleton(_leaveOnly)
            .AddSingleton(_guarded)
            .AddSingleton(_typed)
            .AddTransient(_ => Track(new DisposablePage()))
            .AddTransient(_ => Track(new FailingDisposablePage()))
            .AddScoped<AsyncOnlyResource>()
            .AddTransient<AsyncResourcePage>()
            .BuildServiceProvider();
        _navigationService = new NavigationService(_serviceProvider, NullLogger<NavigationService>.Instance);
        _navigationService.Navigated += (_, e) => _navigated.Add(e.Page);
    }

    public void Dispose()
    {
        _navigationService.Dispose();
        _serviceProvider.Dispose();
        _cancellation.Dispose();
    }

    [Fact]
    public async Task NavigateToAsync_Should_EnterPageAndRaiseNavigated_When_PageIsNew()
    {
        var navigated = await _navigationService.NavigateToAsync<FirstPage, int>(42, CurrentCancellationToken);

        navigated.Should().BeTrue();
        _navigationService.CurrentPage.Should().BeSameAs(_first);
        _first.Calls.Should().Equal("to");
        _first.Parameter.Should().Be(42);
        _navigated.Should().Equal(_first);
    }

    [Fact]
    public async Task NavigateToAsync_Should_CallOnlyImplementedLifecycle_When_PageImplementsOneInterface()
    {
        await _navigationService.NavigateToAsync<EnterOnlyPage, int>(7, CurrentCancellationToken);
        await _navigationService.NavigateToAsync<LeaveOnlyPage>(CurrentCancellationToken);
        await _navigationService.GoBackAsync(CurrentCancellationToken);

        _navigationService.CurrentPage.Should().BeSameAs(_enterOnly);
        _enterOnly.Parameter.Should().Be(7);
        _leaveOnly.LeaveCount.Should().Be(1);
    }

    [Fact]
    public async Task NavigateToAsync_Should_NavigateInAndOut_When_PageImplementsNoLifecycle()
    {
        await _navigationService.NavigateToAsync<EmptyPage>(CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeTrue();
        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeTrue();
        _navigationService.CurrentPage.Should().BeOfType<EmptyPage>();
    }

    [Fact]
    public async Task NavigateToAsync_Should_ReturnFalse_When_AlreadyOnPage()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _first.Calls.Should().Equal("to");
    }

    [Fact]
    public async Task GoBackAndGoForward_Should_MoveThroughHistory()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeTrue();
        _navigationService.CurrentPage.Should().BeSameAs(_first);

        (await _navigationService.GoForwardAsync(CurrentCancellationToken)).Should().BeTrue();
        _navigationService.CurrentPage.Should().BeSameAs(_second);
        (await _navigationService.GoForwardAsync(CurrentCancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task NavigateToAsync_Should_ClearForwardHistory_When_NavigatingToNewPage()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        await _navigationService.GoBackAsync(CurrentCancellationToken);

        await _navigationService.NavigateToAsync<FirstPage, int>(1, CurrentCancellationToken);

        (await _navigationService.GoForwardAsync(CurrentCancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task NavigateToAsync_Should_ReplaceCurrentEntry_When_SamePageWithNewParameter()
    {
        await _navigationService.NavigateToAsync<FirstPage, int>(1, CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<FirstPage, int>(2, CurrentCancellationToken);

        navigated.Should().BeTrue();
        _first.Parameter.Should().Be(2);
        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task NavigateToAsync_Should_LeaveTargetAndRestorePrevious_When_TargetFails()
    {
        await _navigationService.NavigateToAsync<FirstPage, int>(1, CurrentCancellationToken);
        _second.Entering = _ => Task.FromException(new InvalidOperationException());

        var navigated = await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _navigationService.CurrentPage.Should().BeSameAs(_first);
        _second.Calls.Should().Equal("to", "from");
        _first.Calls.Should().Equal("to", "from", "to");
        _first.Parameter.Should().Be(1);
        _navigated.Should().Equal(_first);
        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task NavigateToAsync_Should_RestorePreviousAndThrow_When_Canceled()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        _second.Entering = _ =>
        {
            _cancellation.Cancel();
            return Task.FromCanceled(_cancellation.Token);
        };

        var navigate = () => _navigationService.NavigateToAsync<SecondPage>(_cancellation.Token);

        await navigate.Should().ThrowAsync<OperationCanceledException>();
        _navigationService.CurrentPage.Should().BeSameAs(_first);
        _first.Calls.Should().Equal("to", "from", "to");
    }

    [Fact]
    public async Task NavigateToAsync_Should_KeepHistoryConsistent_When_NavigatedHandlerThrows()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        EventHandler<NavigatedEventArgs> throwingHandler = (_, _) => throw new InvalidOperationException();
        _navigationService.Navigated += throwingHandler;

        var navigate = () => _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        await navigate.Should().ThrowAsync<InvalidOperationException>();
        _navigationService.Navigated -= throwingHandler;
        _navigationService.CurrentPage.Should().BeSameAs(_second);
        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeTrue();
        _navigationService.CurrentPage.Should().BeSameAs(_first);
    }

    [Fact]
    public async Task CanGoBackAndCanGoForward_Should_FollowHistory()
    {
        _navigationService.CanGoBack.Should().BeFalse();
        _navigationService.CanGoForward.Should().BeFalse();

        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        _navigationService.CanGoBack.Should().BeTrue();
        _navigationService.CanGoForward.Should().BeFalse();

        await _navigationService.GoBackAsync(CurrentCancellationToken);
        _navigationService.CanGoBack.Should().BeFalse();
        _navigationService.CanGoForward.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateToAsync_Should_DisposeForwardPages_When_ForwardHistoryIsCleared()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<DisposablePage>(CurrentCancellationToken);
        await _navigationService.GoBackAsync(CurrentCancellationToken);
        _created.Should().ContainSingle().Which.IsDisposed.Should().BeFalse();

        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        _created.Single().IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateToAsync_Should_DisposeReplacedPage_When_SamePageWithNewParameter()
    {
        await _navigationService.NavigateToAsync<DisposablePage, int>(1, CurrentCancellationToken);

        await _navigationService.NavigateToAsync<DisposablePage, int>(2, CurrentCancellationToken);

        _created.Select(page => page.IsDisposed).Should().Equal(true, false);
        _navigationService.CurrentPage.Should().BeSameAs(_created[1]);
    }

    [Fact]
    public async Task NavigateToAsync_Should_DisposeTargetPage_When_TargetFails()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<FailingDisposablePage>(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _created.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_Should_DisposeAllPagesAndRejectNavigation()
    {
        await _navigationService.NavigateToAsync<DisposablePage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        _navigationService.Dispose();

        _created.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
        _navigationService.CurrentPage.Should().BeNull();
        var navigate = () => _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        await navigate.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task NavigateToAsync_Should_StayOnPage_When_GuardBlocks()
    {
        await _navigationService.NavigateToAsync<GuardedPage>(CurrentCancellationToken);
        _guarded.Guard = () => Task.FromResult(false);

        var navigated = await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _navigationService.CurrentPage.Should().BeSameAs(_guarded);
        _guarded.Calls.Should().Equal("to");
        _first.Calls.Should().BeEmpty();
        _navigated.Should().Equal(_guarded);
    }

    [Fact]
    public async Task GoBackAsync_Should_StayOnPage_When_GuardBlocks()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<GuardedPage>(CurrentCancellationToken);
        _guarded.Guard = () => Task.FromResult(false);

        var navigated = await _navigationService.GoBackAsync(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _navigationService.CurrentPage.Should().BeSameAs(_guarded);
        _navigationService.CanGoBack.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateToAsync_Should_LeavePage_When_GuardAllows()
    {
        await _navigationService.NavigateToAsync<GuardedPage>(CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeTrue();
        _guarded.GuardCalls.Should().Be(1);
        _guarded.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task NavigateToAsync_Should_StayOnPage_When_GuardThrows()
    {
        await _navigationService.NavigateToAsync<GuardedPage>(CurrentCancellationToken);
        _guarded.Guard = () => Task.FromException<bool>(new InvalidOperationException());

        var navigated = await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        navigated.Should().BeFalse();
        _navigationService.CurrentPage.Should().BeSameAs(_guarded);
    }

    [Fact]
    public async Task NavigateToAsync_Should_PassTypedParameter_When_PageExpectsParameter()
    {
        await _navigationService.NavigateToAsync<TypedPage, int>(5, CurrentCancellationToken);
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        await _navigationService.GoBackAsync(CurrentCancellationToken);

        _navigationService.CurrentPage.Should().BeSameAs(_typed);
        _typed.Parameters.Should().Equal(5, 5);
    }

    [Fact]
    public async Task NavigateToAsync_Should_ReturnFalse_When_PageNeedsParameterButGetsNone()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        var generic = await _navigationService.NavigateToAsync<TypedPage>(CurrentCancellationToken);
        var byType = await _navigationService.NavigateToAsync(typeof(TypedPage), CurrentCancellationToken);

        generic.Should().BeFalse();
        byType.Should().BeFalse();
        _typed.Parameters.Should().BeEmpty();
        _navigationService.CurrentPage.Should().BeSameAs(_first);
    }

    [Fact]
    public async Task NavigateToAsync_Should_PassEachParameterType_When_PageAcceptsSeveral()
    {
        await _navigationService.NavigateToAsync<TypedPage, int>(5, CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<TypedPage, string>("chat", CurrentCancellationToken);

        navigated.Should().BeTrue();
        _typed.Parameters.Should().Equal(5, "chat");
    }

    [Fact]
    public async Task NavigateToAsync_Should_ReturnFalse_When_SamePageWithEqualParameter()
    {
        await _navigationService.NavigateToAsync<TypedPage, string>("chat", CurrentCancellationToken);

        var navigated = await _navigationService.NavigateToAsync<TypedPage, string>(
            new string("chat".ToCharArray()),
            CurrentCancellationToken);

        navigated.Should().BeFalse();
        _typed.Parameters.Should().Equal("chat");
    }

    [Fact]
    public async Task NavigateToAsync_Should_EnterWithoutParameter_When_PageAcceptsBoth()
    {
        var navigated = await _navigationService.NavigateToAsync(typeof(FirstPage), CurrentCancellationToken);

        navigated.Should().BeTrue();
        _first.Calls.Should().Equal("to");
        _first.Parameter.Should().BeNull();
    }

    [Fact]
    public async Task NavigateToAsync_Should_RunOneAfterAnother_When_NavigationsOverlap()
    {
        var gate = new TaskCompletionSource();
        _first.Entering = _ => gate.Task;

        var first = _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        var second = _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        _first.Calls.Should().Equal("to");
        _second.Calls.Should().BeEmpty();
        gate.SetResult();
        (await Task.WhenAll(first, second)).Should().Equal(true, true);
        _first.Calls.Should().Equal("to", "from");
        _second.Calls.Should().Equal("to");
        _navigationService.CurrentPage.Should().BeSameAs(_second);
        _navigated.Should().Equal(_first, _second);
    }

    [Fact]
    public async Task NavigateToAsync_Should_ThrowWithoutTouchingPages_When_CanceledWhileWaiting()
    {
        var gate = new TaskCompletionSource();
        _first.Entering = _ => gate.Task;
        var first = _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        var second = _navigationService.NavigateToAsync<SecondPage>(_cancellation.Token);

        await _cancellation.CancelAsync();
        gate.SetResult();

        await second.Invoking(static task => task).Should().ThrowAsync<OperationCanceledException>();
        (await first).Should().BeTrue();
        _second.Calls.Should().BeEmpty();
        _first.Calls.Should().Equal("to");
        _navigationService.CurrentPage.Should().BeSameAs(_first);
    }

    [Fact]
    public async Task NavigateToAsync_Should_DisposeAsyncOnlyServices_When_PageLeavesHistory()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<AsyncResourcePage>(CurrentCancellationToken);
        var resource = ((AsyncResourcePage)_navigationService.CurrentPage!).Resource;
        await _navigationService.GoBackAsync(CurrentCancellationToken);

        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);

        resource.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeAsyncOnlyServicesOfAllEntries()
    {
        await _navigationService.NavigateToAsync<AsyncResourcePage>(CurrentCancellationToken);
        var resource = ((AsyncResourcePage)_navigationService.CurrentPage!).Resource;

        await _navigationService.DisposeAsync();

        resource.IsDisposed.Should().BeTrue();
        _navigationService.CurrentPage.Should().BeNull();
    }

    [Fact]
    public async Task Dispose_Should_NotThrow_When_ServiceCanOnlyBeDisposedAsynchronously()
    {
        await _navigationService.NavigateToAsync<AsyncResourcePage>(CurrentCancellationToken);

        var dispose = () => _navigationService.Dispose();

        dispose.Should().NotThrow();
        _navigationService.CurrentPage.Should().BeNull();
    }

    [Fact]
    public async Task ClearHistoryAsync_Should_RemoveOtherPagesAndKeepCurrent()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<DisposablePage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<GuardedPage>(CurrentCancellationToken);
        await _navigationService.GoBackAsync(CurrentCancellationToken);

        await _navigationService.ClearHistoryAsync(CurrentCancellationToken);

        _navigationService.CurrentPage.Should().BeSameAs(_second);
        _navigationService.CanGoBack.Should().BeFalse();
        _navigationService.CanGoForward.Should().BeFalse();
        _created.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
        _first.Calls.Should().Equal("to", "from");
        _guarded.GuardCalls.Should().Be(1);
        (await _navigationService.GoBackAsync(CurrentCancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task ClearHistoryAsync_Should_RaiseOnlyChangedHistoryProperties()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        var changed = RecordPropertyChanges();

        await _navigationService.ClearHistoryAsync(CurrentCancellationToken);

        changed.Should().Equal(nameof(INavigationService.CanGoBack));
    }

    [Fact]
    public async Task ClearHistoryAsync_Should_Throw_When_Disposed()
    {
        _navigationService.Dispose();

        var clear = () => _navigationService.ClearHistoryAsync(CurrentCancellationToken);

        await clear.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task NavigateToAsync_Should_RaisePropertyChangesBeforeNavigated()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        var changed = RecordPropertyChanges();
        _navigationService.Navigated += (_, _) => changed.Add(nameof(INavigationService.Navigated));

        await _navigationService.NavigateToAsync<SecondPage>(CurrentCancellationToken);
        await _navigationService.GoBackAsync(CurrentCancellationToken);

        changed.Should().Equal(
            nameof(INavigationService.CurrentPage),
            nameof(INavigationService.CanGoBack),
            nameof(INavigationService.Navigated),
            nameof(INavigationService.CurrentPage),
            nameof(INavigationService.CanGoBack),
            nameof(INavigationService.CanGoForward),
            nameof(INavigationService.Navigated));
    }

    [Fact]
    public async Task NavigateToAsync_Should_RaiseNothing_When_NavigationIsRefused()
    {
        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);
        var changed = RecordPropertyChanges();

        await _navigationService.NavigateToAsync<FirstPage>(CurrentCancellationToken);

        changed.Should().BeEmpty();
    }

    private List<string?> RecordPropertyChanges()
    {
        var changed = new List<string?>();
        _navigationService.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        return changed;
    }

    private T Track<T>(T page)
        where T : DisposablePage
    {
        _created.Add(page);
        return page;
    }
}
