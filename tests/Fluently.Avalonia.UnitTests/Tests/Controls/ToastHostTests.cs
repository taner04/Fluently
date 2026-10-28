using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Avalonia.UnitTests.Tests.Controls;

public sealed class ToastHostTests : TestBase, IDisposable
{
    private readonly ServiceProvider _serviceProvider =
        Services.Create(static services => services.AddSingleton<IViewProvider, BrandedViewProvider>());
    private readonly IToastService _toasts;

    public ToastHostTests()
    {
        _toasts = _serviceProvider.GetRequiredService<IToastService>();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public Task ToastHost_Should_ShowToastAboveContent_When_ToastIsShown() =>
        UiThread.Run(() =>
        {
            var content = new TextBlock();
            var host = new ToastHost { Toasts = _toasts, Content = content };
            TestWindow.Show(host);

            _toasts.Show("Saved", "Your changes were saved.", ToastSeverity.Success);
            Dispatcher.UIThread.RunJobs();

            content.GetVisualParent().Should().NotBeNull();
            ToastViews(host).Should().ContainSingle()
                .Which.ViewModel.Should().BeOfType<Toast>()
                .Which.Should().BeEquivalentTo(new { Title = "Saved", Severity = ToastSeverity.Success });
        });

    [Fact]
    public Task ToastHost_Should_RemoveOnlyDismissedToast_When_ToastsAreEqualRecords() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var kept = new RecordToastViewModel();
            var dismissed = new RecordToastViewModel();
            _toasts.Show(kept);
            _toasts.Show(dismissed);
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(dismissed);
            await WaitUntilAsync(() => host.GetVisualDescendants().OfType<RecordToastView>().Count() == 1);

            host.GetVisualDescendants().OfType<RecordToastView>().Should().ContainSingle()
                .Which.ViewModel.Should().BeSameAs(kept);
        });

    [Fact]
    public Task ToastHost_Should_KeepServiceOrder_When_LeavingToastReturnsBehindAnother() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var older = new Toast("Older", "");
            var leaving = new Toast("Leaving", "");
            _toasts.Show(older);
            _toasts.Show(leaving);
            Dispatcher.UIThread.RunJobs();
            _toasts.Dismiss(leaving);
            Dispatcher.UIThread.RunJobs();

            await Task.Run(() =>
            {
                _toasts.Show(leaving);
                _toasts.Show(older);
            }, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            Panel(host).Children.OfType<ToastItem>().Where(static item => !item.IsLeaving)
                .Select(static item => ((Toast)item.Toast).Title)
                .Should().Equal("Older", "Leaving");
        });

    [Fact]
    public Task ToastHost_Should_ShowNewestToastFirst() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            _toasts.Show("First", "");
            _toasts.Show("Second", "");
            Dispatcher.UIThread.RunJobs();

            ToastViews(host).Select(static view => ((Toast)view.ViewModel).Title).Should().Equal("Second", "First");
        });

    [Fact]
    public Task ToastHost_Should_SlideToastOutBeforeRemovingIt() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var toast = _toasts.Show("Saved", "");
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(toast);

            var item = host.GetVisualDescendants().OfType<ToastItem>().Single();
            item.IsLeaving.Should().BeTrue();
            item.IsHitTestVisible.Should().BeFalse();

            await WaitUntilAsync(() => item.GetVisualParent() is null);
            item.GetVisualParent().Should().BeNull();
        });

    [Fact]
    public Task ToastHost_Should_KeepNewToastsInOrder_When_OthersAreLeaving() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var leaving = _toasts.Show("Leaving", "");
            _toasts.Show("First", "");
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(leaving);
            _toasts.Show("Second", "");

            host.GetVisualDescendants().OfType<ToastItem>()
                .Where(static item => !item.IsLeaving)
                .Select(static item => ((Toast)item.Toast).Title)
                .Should().Equal("Second", "First");
        });

    [Fact]
    public Task ToastHost_Should_ShowCustomToastWithItsView() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            _toasts.Show(new UndoToastViewModel());
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<UndoToastView>().Should().ContainSingle();
        });

    [Fact]
    public Task ToastHost_Should_ShowToast_When_ShownFromAnotherThread() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            await Task.Run(() => _toasts.Show("Background", ""), CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            ToastViews(host).Should().ContainSingle();
        });

    [Fact]
    public Task ToastHost_Should_StopFollowing_When_Detached() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            var window = TestWindow.Show(host);
            window.Close();

            _toasts.Show("Saved", "");
            Dispatcher.UIThread.RunJobs();

            ToastViews(host).Should().BeEmpty();
        });

    [Fact]
    public Task ToastHost_Should_BringToastBack_When_ShownAgainWhileLeaving() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var toast = new UndoToastViewModel();
            _toasts.Show(toast);
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(toast);
            _toasts.Show(toast);
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<ToastItem>().Should().ContainSingle()
                .Which.IsLeaving.Should().BeFalse();
            host.GetVisualDescendants().OfType<UndoToastView>().Should().ContainSingle();
        });

    [Fact]
    public Task ToastHost_Should_FinishSecondExit_When_ToastLeftReturnedAndLeftAgain() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var toast = _toasts.Show("Saved", "");
            Dispatcher.UIThread.RunJobs();
            var item = host.GetVisualDescendants().OfType<ToastItem>().Single();

            _toasts.Dismiss(toast);
            await Task.Delay(120, CurrentCancellationToken);
            _toasts.Show(toast);
            _toasts.Dismiss(toast);
            await Task.Delay(80, CurrentCancellationToken);
            Dispatcher.UIThread.RunJobs();

            item.GetVisualParent().Should().NotBeNull();
            await WaitUntilAsync(() => item.GetVisualParent() is null);
            item.GetVisualParent().Should().BeNull();
        });

    [Fact]
    public Task ToastHost_Should_ShowCustomToastAgain_After_ItLeft() =>
        UiThread.RunAsync(async () =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            var toast = new UndoToastViewModel();
            _toasts.Show(toast);
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(toast);
            await WaitUntilAsync(() => !host.GetVisualDescendants().OfType<ToastItem>().Any());
            _toasts.Show(toast);
            Dispatcher.UIThread.RunJobs();

            host.GetVisualDescendants().OfType<UndoToastView>().Should().ContainSingle();
        });

    [Fact]
    public Task ToastHost_Should_ShowToastsTopRight_ByDefault() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            var panel = Panel(host);

            _toasts.Position.Should().Be(ToastPosition.TopRight);
            panel.HorizontalAlignment.Should().Be(HorizontalAlignment.Right);
            panel.VerticalAlignment.Should().Be(VerticalAlignment.Top);
        });

    [Fact]
    public Task ToastHost_Should_PutNewestToastAtTheBottom_When_PositionIsAtTheBottom() =>
        UiThread.Run(() =>
        {
            _toasts.Position = ToastPosition.BottomLeft;
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            _toasts.Show("First", "");
            _toasts.Show("Second", "");
            Dispatcher.UIThread.RunJobs();

            var panel = Panel(host);
            panel.HorizontalAlignment.Should().Be(HorizontalAlignment.Left);
            panel.VerticalAlignment.Should().Be(VerticalAlignment.Bottom);
            var items = host.GetVisualDescendants().OfType<ToastItem>().ToList();
            var newest = items.Single(static item => ((Toast)item.Toast).Title == "Second");
            var oldest = items.Single(static item => ((Toast)item.Toast).Title == "First");
            newest.Bounds.Y.Should().BeGreaterThan(oldest.Bounds.Y);
        });

    [Fact]
    public Task ToastHost_Should_CloseGap_When_ToastStartsLeaving() =>
        UiThread.Run(() =>
        {
            _toasts.Position = ToastPosition.BottomRight;
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);
            _toasts.Show("First", "");
            var second = _toasts.Show("Second", "");
            Dispatcher.UIThread.RunJobs();

            _toasts.Dismiss(second);
            Dispatcher.UIThread.RunJobs();

            var items = host.GetVisualDescendants().OfType<ToastItem>().ToList();
            var leaving = items.Single(static item => ((Toast)item.Toast).Title == "Second");
            var first = items.Single(static item => ((Toast)item.Toast).Title == "First");
            leaving.IsLeaving.Should().BeTrue();
            first.Bounds.Bottom.Should().BeApproximately(Panel(host).Bounds.Height, 0.5);
            Panel(host).Bounds.Height.Should().BeApproximately(first.Bounds.Height, 0.5);
        });

    [Fact]
    public Task ToastHost_Should_MoveToasts_When_PositionChanges() =>
        UiThread.Run(() =>
        {
            var host = new ToastHost { Toasts = _toasts };
            TestWindow.Show(host);

            _toasts.Position = ToastPosition.BottomCenter;

            var panel = Panel(host);
            panel.HorizontalAlignment.Should().Be(HorizontalAlignment.Center);
            panel.VerticalAlignment.Should().Be(VerticalAlignment.Bottom);
        });

    private static ToastPanel Panel(ToastHost host) => host.GetVisualDescendants().OfType<ToastPanel>().Single();

    private static List<BrandedToastView> ToastViews(ToastHost host) =>
        [.. host.GetVisualDescendants().OfType<BrandedToastView>()];
}
