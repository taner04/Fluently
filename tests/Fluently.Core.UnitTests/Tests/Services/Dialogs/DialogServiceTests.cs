using Fluently.Core.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluently.Core.UnitTests.Tests.Services.Dialogs;

public sealed class DialogServiceTests : TestBase, IDisposable
{
    private readonly List<string?> _changes = [];
    private readonly DialogService _dialogService;
    private readonly GuardedDialog _guarded = new();
    private readonly List<ServiceProvider> _providers = [];
    private readonly ServiceProvider _serviceProvider;
    private readonly SharedDialog _shared = new();

    public DialogServiceTests()
    {
        _serviceProvider = new ServiceCollection()
            .AddScoped<Greeting>()
            .AddTransient<NameDialog>()
            .AddTransient<ConfirmDialog>()
            .AddSingleton(_shared)
            .AddSingleton(_guarded)
            .BuildServiceProvider();
        _dialogService = CreateService(_serviceProvider);
        _dialogService.PropertyChanged += (_, e) => _changes.Add(e.PropertyName);
    }

    public void Dispose()
    {
        _dialogService.Dispose();
        _serviceProvider.Dispose();
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnResult_When_DialogIsClosed()
    {
        var showing = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);
        var dialog = (NameDialog)_dialogService.CurrentDialog!;

        var closed = await _dialogService.CloseAsync(dialog, "Ada");
        var result = await showing;

        closed.Should().BeTrue();
        result.IsCompleted.Should().BeTrue();
        result.Value.Should().Be("Ada");
        dialog.Calls.Should().Equal("to", "from");
        dialog.IsDisposed.Should().BeTrue();
        _dialogService.Dialogs.Should().BeEmpty();
        _dialogService.CurrentDialog.Should().BeNull();
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnDismissed_When_DialogIsDismissed()
    {
        var showing = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);

        var dismissed = await _dialogService.DismissAsync(_dialogService.CurrentDialog!);
        var result = await showing;

        dismissed.Should().BeTrue();
        result.Should().Be(DialogResult<string>.Dismissed);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ShowAsync_Should_PassParameter_When_DialogImplementsNavigatedTo()
    {
        var showing = _dialogService.ShowAsync<ConfirmDialog, bool, string>("Delete?", CurrentCancellationToken);
        var dialog = (ConfirmDialog)_dialogService.CurrentDialog!;

        await _dialogService.CloseAsync(dialog, true);

        dialog.Message.Should().Be("Delete?");
        (await showing).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnDismissed_When_DialogNeedsParameter()
    {
        var result = await _dialogService.ShowAsync<ConfirmDialog, bool>(CurrentCancellationToken);

        result.IsCompleted.Should().BeFalse();
        _dialogService.Dialogs.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_Should_StackDialogs_When_DialogShowsAnotherDialog()
    {
        var first = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);
        var bottom = _dialogService.CurrentDialog!;
        var second = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);
        var top = _dialogService.CurrentDialog!;

        _dialogService.Dialogs.Should().Equal(bottom, top);
        top.Should().NotBeSameAs(bottom);

        await _dialogService.CloseAsync((NameDialog)top, "top");

        _dialogService.CurrentDialog.Should().BeSameAs(bottom);
        (await second).Value.Should().Be("top");
        first.IsCompleted.Should().BeFalse();
        _changes.Should().Equal(
            nameof(IDialogService.Dialogs), nameof(IDialogService.CurrentDialog),
            nameof(IDialogService.Dialogs), nameof(IDialogService.CurrentDialog),
            nameof(IDialogService.Dialogs), nameof(IDialogService.CurrentDialog));
    }

    [Fact]
    public async Task ShowAsync_Should_DismissDialogAndThrow_When_Canceled()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
        var showing = _dialogService.ShowAsync<NameDialog, string>(cancellation.Token);
        var dialog = (NameDialog)_dialogService.CurrentDialog!;

        await cancellation.CancelAsync();

        await showing.Invoking(static task => task).Should().ThrowAsync<OperationCanceledException>();
        _dialogService.Dialogs.Should().BeEmpty();
        dialog.IsDisposed.Should().BeTrue();
        dialog.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnDismissed_When_OpeningThrows()
    {
        var dialog = new NameDialog(new Greeting())
        {
            Entering = static _ => throw new InvalidOperationException("broken")
        };
        var dialogService = CreateService(dialog);

        var result = await dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);

        result.IsCompleted.Should().BeFalse();
        dialogService.Dialogs.Should().BeEmpty();
        dialog.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnResult_When_DialogClosesItselfWhileOpening()
    {
        var dialog = new NameDialog(new Greeting());
        var dialogService = CreateService(dialog);
        dialog.Entering = async _ => await dialogService.CloseAsync(dialog, "early");

        var result = await dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), CurrentCancellationToken);

        result.Value.Should().Be("early");
        dialogService.Dialogs.Should().BeEmpty();
        dialog.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task CloseAsync_Should_LetDialogShowAnotherDialog_When_Leaving()
    {
        var dialog = new NameDialog(new Greeting());
        var dialogService = CreateService(dialog, static services => services.AddTransient<ConfirmDialog>());
        Task<DialogResult<bool>>? followUp = null;
        dialog.Leaving = () =>
        {
            followUp = dialogService.ShowAsync<ConfirmDialog, bool, string>("Saved. Close?", CurrentCancellationToken);
            return Task.CompletedTask;
        };
        var showing = dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);

        await dialogService.CloseAsync(dialog, "done").WaitAsync(TimeSpan.FromSeconds(5), CurrentCancellationToken);

        (await showing).Value.Should().Be("done");
        dialogService.CurrentDialog.Should().BeOfType<ConfirmDialog>();
        await dialogService.CloseAsync((ConfirmDialog)dialogService.CurrentDialog!, true);
        (await followUp!).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnResult_When_CanceledWhileDialogIsClosing()
    {
        var leaving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = new NameDialog(new Greeting()) { Leaving = () => leaving.Task };
        var dialogService = CreateService(dialog);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
        var showing = dialogService.ShowAsync<NameDialog, string>(cancellation.Token);

        var closing = dialogService.CloseAsync(dialog, "kept");
        await cancellation.CancelAsync();
        leaving.SetResult();
        await closing;

        var result = await showing.WaitAsync(TimeSpan.FromSeconds(5), CurrentCancellationToken);
        result.Value.Should().Be("kept");
    }

    [Fact]
    public async Task DisposeAsync_Should_DismissDialog_When_ItIsStillOpening()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = new NameDialog(new Greeting()) { Entering = _ => entered.Task };
        var dialogService = CreateService(dialog);
        var showing = dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);

        await dialogService.DisposeAsync();
        entered.SetResult();

        (await showing.WaitAsync(TimeSpan.FromSeconds(5), CurrentCancellationToken)).IsCompleted.Should().BeFalse();
        dialogService.Dialogs.Should().BeEmpty();
        dialog.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnDismissed_When_SameInstanceIsAlreadyOpen()
    {
        var first = _dialogService.ShowAsync<SharedDialog, string>(CurrentCancellationToken);

        var second = await _dialogService.ShowAsync<SharedDialog, string>(CurrentCancellationToken);

        second.IsCompleted.Should().BeFalse();
        _dialogService.Dialogs.Should().Equal(_shared);
        first.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CloseAsync_Should_Throw_When_ResultTypeDiffersFromShownType()
    {
        _ = _dialogService.ShowAsync<SharedDialog, string>(CurrentCancellationToken);

        var close = () => _dialogService.CloseAsync(_shared, 1);

        await close.Should().ThrowAsync<InvalidOperationException>();
        _dialogService.CurrentDialog.Should().BeSameAs(_shared);
    }

    [Fact]
    public async Task CloseAsync_Should_ReturnFalse_When_DialogIsNotOpen()
    {
        (await _dialogService.CloseAsync<string>(_shared, "x")).Should().BeFalse();
        (await _dialogService.DismissAsync(_shared)).Should().BeFalse();
    }

    [Fact]
    public async Task ShowAsync_Should_ResolveViewServicesFromDialogScope_When_DialogIsOpen()
    {
        _ = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);
        var dialog = (NameDialog)_dialogService.CurrentDialog!;

        var services = _dialogService.Scopes.For(dialog);

        services!.GetRequiredService<Greeting>().Should().BeSameAs(dialog.Greeting);
        await _dialogService.DismissAsync(dialog);
    }

    [Fact]
    public async Task RequestDismissAsync_Should_DismissDialog_When_DialogHasNoGuard()
    {
        var showing = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);

        var dismissed =
            await _dialogService.RequestDismissAsync(_dialogService.CurrentDialog!, CurrentCancellationToken);

        dismissed.Should().BeTrue();
        (await showing).Should().Be(DialogResult<string>.Dismissed);
        _dialogService.Dialogs.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestDismissAsync_Should_KeepDialog_When_GuardRefuses()
    {
        _guarded.Guard = static _ => Task.FromResult(false);
        var showing = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);

        var dismissed = await _dialogService.RequestDismissAsync(_guarded, CurrentCancellationToken);

        dismissed.Should().BeFalse();
        _dialogService.CurrentDialog.Should().BeSameAs(_guarded);
        showing.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task RequestDismissAsync_Should_KeepDialog_When_GuardThrows()
    {
        _guarded.Guard = static _ => throw new InvalidOperationException("broken");
        _ = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);

        var dismissed = await _dialogService.RequestDismissAsync(_guarded, CurrentCancellationToken);

        dismissed.Should().BeFalse();
        _dialogService.CurrentDialog.Should().BeSameAs(_guarded);
    }

    [Fact]
    public async Task RequestDismissAsync_Should_FollowConfirmDialog_When_GuardShowsDialog()
    {
        _guarded.Guard = async cancellationToken =>
            (await _dialogService.ShowAsync<ConfirmDialog, bool, string>("Discard?", cancellationToken)).Value;
        var showing = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);

        var request = _dialogService.RequestDismissAsync(_guarded, CurrentCancellationToken);
        var confirm = (ConfirmDialog)_dialogService.CurrentDialog!;
        await _dialogService.CloseAsync(confirm, true);

        (await request).Should().BeTrue();
        confirm.Message.Should().Be("Discard?");
        (await showing).IsCompleted.Should().BeFalse();
        _dialogService.Dialogs.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_Should_ReturnDismissed_When_DialogDismissesItselfWhileOpening()
    {
        var dialog = new NameDialog(new Greeting());
        var dialogService = CreateService(dialog);
        dialog.Entering = async _ => await dialogService.DismissAsync(dialog);

        var result = await dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), CurrentCancellationToken);

        result.IsCompleted.Should().BeFalse();
        dialogService.Dialogs.Should().BeEmpty();
        dialog.Calls.Should().Equal("to", "from");
    }

    [Fact]
    public async Task RequestDismissAsync_Should_ThrowAndKeepDialog_When_Canceled()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
        _guarded.Guard = async cancellationToken =>
        {
            await cancellation.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return true;
        };
        var showing = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);

        var request = () => _dialogService.RequestDismissAsync(_guarded, cancellation.Token);

        await request.Should().ThrowAsync<OperationCanceledException>();
        _dialogService.CurrentDialog.Should().BeSameAs(_guarded);
        showing.IsCompleted.Should().BeFalse();
        await _dialogService.DismissAsync(_guarded);
    }

    [Fact]
    public async Task ShowAsync_Should_Throw_When_ServiceIsDisposed()
    {
        await _dialogService.DisposeAsync();

        var show = () => _dialogService.ShowAsync<ConfirmDialog, bool, string>("Delete?", CurrentCancellationToken);

        await show.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task RequestDismissAsync_Should_AskGuardOnce_When_RequestedTwice()
    {
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _guarded.Guard = _ => answer.Task;
        _ = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);

        var first = _dialogService.RequestDismissAsync(_guarded, CurrentCancellationToken);
        var second = await _dialogService.RequestDismissAsync(_guarded, CurrentCancellationToken);
        answer.SetResult(true);

        second.Should().BeFalse();
        (await first).Should().BeTrue();
        _guarded.GuardCalls.Should().Be(1);
    }

    [Fact]
    public async Task DismissAsyncAndCloseAsync_Should_IgnoreGuard()
    {
        _guarded.Guard = static _ => Task.FromResult(false);

        _ = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);
        var dismissed = await _dialogService.DismissAsync(_guarded);
        var showing = _dialogService.ShowAsync<GuardedDialog, string>(CurrentCancellationToken);
        var closed = await _dialogService.CloseAsync(_guarded, "done");

        dismissed.Should().BeTrue();
        closed.Should().BeTrue();
        (await showing).Value.Should().Be("done");
        _guarded.GuardCalls.Should().Be(0);
    }

    [Fact]
    public async Task DialogViewModel_Should_CloseItselfWithResult()
    {
        var (dialogService, dialog) = CreateSelfClosing();
        var showing = dialogService.ShowAsync<SelfClosingDialog, string>(CurrentCancellationToken);

        (await dialog.CloseWithAsync("done")).Should().BeTrue();

        (await showing).Value.Should().Be("done");
        dialogService.Dialogs.Should().BeEmpty();
    }

    [Fact]
    public async Task DialogViewModel_Should_DismissItself()
    {
        var (dialogService, dialog) = CreateSelfClosing();
        var showing = dialogService.ShowAsync<SelfClosingDialog, string>(CurrentCancellationToken);

        (await dialog.DismissSelfAsync()).Should().BeTrue();

        (await showing).IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task DialogViewModel_Should_RequestDismissItself()
    {
        var (dialogService, dialog) = CreateSelfClosing();
        var showing = dialogService.ShowAsync<SelfClosingDialog, string>(CurrentCancellationToken);

        (await dialog.RequestDismissSelfAsync()).Should().BeTrue();

        (await showing).IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task DialogViewModel_Should_ReturnFalse_When_NotShown()
    {
        var dialog = new SelfClosingDialog();

        (await dialog.CloseWithAsync("never")).Should().BeFalse();
        (await dialog.DismissSelfAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_Should_DismissOpenDialogs()
    {
        var showing = _dialogService.ShowAsync<NameDialog, string>(CurrentCancellationToken);
        var dialog = (NameDialog)_dialogService.CurrentDialog!;

        _changes.Clear();

        await _dialogService.DisposeAsync();

        (await showing).IsCompleted.Should().BeFalse();
        dialog.IsDisposed.Should().BeTrue();
        _dialogService.Dialogs.Should().BeEmpty();
        _changes.Should().Equal(nameof(IDialogService.Dialogs), nameof(IDialogService.CurrentDialog));
    }

    [Fact]
    public async Task ShowAsyncType_Should_ShowDialogWithTypedParameter()
    {
        var showing =
            _dialogService.ShowAsync<bool, string>(typeof(ConfirmDialog), "Delete?", CurrentCancellationToken);
        var dialog = (ConfirmDialog)_dialogService.CurrentDialog!;

        await _dialogService.CloseAsync(dialog, true);

        dialog.Message.Should().Be("Delete?");
        (await showing).Value.Should().BeTrue();
    }

    [Fact]
    public async Task ShowAsyncType_Should_ShowDialogWithoutParameter()
    {
        var showing = _dialogService.ShowAsync<string>(typeof(NameDialog), CurrentCancellationToken);

        await _dialogService.CloseAsync((NameDialog)_dialogService.CurrentDialog!, "Ada");

        (await showing).Value.Should().Be("Ada");
    }

    [Fact]
    public async Task ShowAsyncType_Should_ReturnDismissed_When_DialogDoesNotAcceptParameterType()
    {
        var result = await _dialogService.ShowAsync<bool, int>(typeof(ConfirmDialog), 1, CurrentCancellationToken);

        result.IsCompleted.Should().BeFalse();
        _dialogService.Dialogs.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsyncType_Should_Throw_When_TypeIsNotADialogOfThatResult()
    {
        var show = () => _dialogService.ShowAsync<int>(typeof(NameDialog), CurrentCancellationToken);

        await show.Should().ThrowAsync<ArgumentException>();
        _dialogService.Dialogs.Should().BeEmpty();
    }

    private DialogService CreateService(NameDialog dialog, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection().AddSingleton(dialog);
        configure?.Invoke(services);
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return CreateService(provider);
    }

    private static DialogService CreateService(IServiceProvider provider) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ViewModelScopes(provider),
            NullLogger<DialogService>.Instance);

    private (DialogService, SelfClosingDialog) CreateSelfClosing()
    {
        var dialog = new SelfClosingDialog();
        var provider = new ServiceCollection().AddSingleton(dialog).BuildServiceProvider();
        _providers.Add(provider);
        return (CreateService(provider), dialog);
    }
}
