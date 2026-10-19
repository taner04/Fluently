using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class DialogHostTests : TestBase, IDisposable
{
    private readonly IDialogService _dialogs;
    private readonly ServiceProvider _serviceProvider = Services.Create();

    public DialogHostTests()
    {
        _dialogs = _serviceProvider.GetRequiredService<IDialogService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task DialogHost_Should_ShowDialogAboveContent_When_DialogOpens() =>
        UiThread.Run(() =>
        {
            var content = new TextBlock();
            var host = new DialogHost { Dialogs = _dialogs, Content = content };
            TestWindow.Show(host, 400, 300);

            _ = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            content.GetVisualParent().Should().NotBeNull();
            Presenters(host).Should().ContainSingle()
                .Which.Content.Should().BeOfType<ConfirmDialogView>().Which.ViewModel.Should()
                .BeSameAs(_dialogs.CurrentDialog);
            host.GetVisualDescendants().OfType<ConfirmDialogView>().Should().ContainSingle();
        });

    [Fact]
    public Task DialogHost_Should_RemoveOnlyClosedDialog_When_DialogsAreEqualRecords() =>
        UiThread.RunAsync(async () =>
        {
            var host = new DialogHost { Dialogs = _dialogs, Content = new TextBlock() };
            TestWindow.Show(host, 400, 300);
            _ = _dialogs.ShowAsync<RecordDialogViewModel, bool>(CurrentCancellationToken);
            await WaitUntilAsync(() => _dialogs.Dialogs.Count == 1);
            _ = _dialogs.ShowAsync<RecordDialogViewModel, bool>(CurrentCancellationToken);
            await WaitUntilAsync(() => _dialogs.Dialogs.Count == 2);
            var top = (RecordDialogViewModel)_dialogs.CurrentDialog!;

            await _dialogs.CloseAsync(top, true);
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<RecordDialogView>().Should().ContainSingle()
                .Which.ViewModel.Should().NotBeSameAs(top);
        });

    [Fact]
    public Task DialogHost_Should_RemoveDialog_When_DialogCloses() =>
        UiThread.RunAsync(async () =>
        {
            var host = new DialogHost { Dialogs = _dialogs };
            TestWindow.Show(host, 400, 300);
            var showing = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);

            await _dialogs.CloseAsync((ConfirmDialogViewModel)_dialogs.CurrentDialog!, true);

            (await showing).Value.Should().BeTrue();
            Presenters(host).Should().BeEmpty();
        });

    [Fact]
    public Task DialogHost_Should_StackDialogs_When_SeveralAreOpen() =>
        UiThread.Run(() =>
        {
            var host = new DialogHost { Dialogs = _dialogs };
            TestWindow.Show(host, 400, 300);

            _ = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            _ = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);

            Presenters(host).Select(static presenter => ((IViewFor<object>)presenter.Content!).ViewModel)
                .Should().Equal(_dialogs.Dialogs);
        });

    [Fact]
    public Task DialogHost_Should_DismissTopDialog_When_EscapeIsPressed() =>
        UiThread.RunAsync(async () =>
        {
            var host = new DialogHost { Dialogs = _dialogs };
            TestWindow.Show(host, 400, 300);
            var bottom = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);
            var top = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);

            PressEscape(host);

            (await top).IsCompleted.Should().BeFalse();
            bottom.IsCompleted.Should().BeFalse();
            Presenters(host).Should().ContainSingle()
                .Which.Content.Should().BeOfType<GuardedDialogView>();
        });

    [Fact]
    public Task DialogHost_Should_KeepDialog_When_EscapeIsPressedAndGuardRefuses() =>
        UiThread.Run(() =>
        {
            _serviceProvider.GetRequiredService<GuardedDialogViewModel>().CanClose = false;
            var host = new DialogHost { Dialogs = _dialogs };
            TestWindow.Show(host, 400, 300);
            var showing = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);

            PressEscape(host);
            Dispatcher.UIThread.RunJobs();

            showing.IsCompleted.Should().BeFalse();
            Presenters(host).Should().ContainSingle();
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DialogHost_Should_DismissOnBackdropClick_OnlyWhenEnabled(bool dismissOnBackdropClick) =>
        UiThread.RunAsync(async () =>
        {
            var host = new DialogHost { Dialogs = _dialogs, DismissOnBackdropClick = dismissOnBackdropClick };
            var window = TestWindow.Show(host, 400, 300);
            var showing = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            window.MouseDown(new Point(1, 1), MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            await WaitUntilAsync(() => showing.IsCompleted == dismissOnBackdropClick);

            showing.IsCompleted.Should().Be(dismissOnBackdropClick);
            Presenters(host).Should().HaveCount(dismissOnBackdropClick ? 0 : 1);
        });

    [Fact]
    public Task DialogHost_Should_StayModal_When_BackdropIsNull() =>
        UiThread.Run(() =>
        {
            var host = new DialogHost { Dialogs = _dialogs, Backdrop = null, DismissOnBackdropClick = true };
            var window = TestWindow.Show(host, 400, 300);
            var showing = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            window.MouseDown(new Point(1, 1), MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            showing.IsCompleted.Should().BeTrue();
        });

    [Fact]
    public Task DialogHost_Should_MoveFocusIntoDialogAndBack() =>
        UiThread.RunAsync(async () =>
        {
            var button = new Button();
            var host = new DialogHost { Dialogs = _dialogs, Content = button };
            var window = TestWindow.Show(host, 400, 300);
            button.Focus();
            var showing = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            var focused = window.FocusManager.GetFocusedElement();
            (focused as Visual).FindAncestorOfType<DialogLayer>(true).Should().NotBeNull();

            await _dialogs.DismissAsync(_dialogs.CurrentDialog!);
            await showing;

            window.FocusManager.GetFocusedElement().Should().BeSameAs(button);
        });

    [Fact]
    public Task DialogHost_Should_StopFollowing_When_Detached() =>
        UiThread.Run(() =>
        {
            var host = new DialogHost { Dialogs = _dialogs };
            var window = TestWindow.Show(host, 400, 300);
            window.Close();

            _ = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);

            Presenters(host).Should().BeEmpty();
        });

    [Fact]
    public Task DialogHost_Should_ShowSingletonDialogAgain_After_ItWasClosed() =>
        UiThread.RunAsync(async () =>
        {
            var host = new DialogHost { Dialogs = _dialogs };
            TestWindow.Show(host, 400, 300);

            var first = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();
            await _dialogs.DismissAsync(_dialogs.CurrentDialog!);
            await first;
            _ = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<GuardedDialogView>().Should().ContainSingle();
        });

    [Fact]
    public Task DialogHost_Should_KeepFocusInTopDialog_When_DialogBelowCloses() =>
        UiThread.RunAsync(async () =>
        {
            var button = new Button();
            var host = new DialogHost { Dialogs = _dialogs, Content = button };
            var window = TestWindow.Show(host, 400, 300);
            button.Focus();
            _ = _dialogs.ShowAsync<ConfirmDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();
            var bottom = _dialogs.CurrentDialog!;
            _ = _dialogs.ShowAsync<GuardedDialogViewModel, bool>(CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            await _dialogs.DismissAsync(bottom);

            var focused = (Visual)window.FocusManager.GetFocusedElement()!;
            focused.FindAncestorOfType<DialogLayer>(true)!.Dialog.Should().BeOfType<GuardedDialogViewModel>();
        });

    private static List<ContentControl> Presenters(DialogHost host) =>
    [
        .. host.GetLogicalDescendants().OfType<ContentControl>()
            .Where(static presenter => presenter.Classes.Contains("dialog"))
    ];

    private static void PressEscape(DialogHost host) =>
        host.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
}
