using Fluently.Core.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Fluently.Core.UnitTests.Tests.Services.Toasts;

public sealed class ToastServiceTests : TestBase, IDisposable
{
    private readonly List<string?> _changes = [];

    private readonly ToastOptions _options = new();

    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddScoped<Greeting>()
        .AddTransient<ScopedToast>()
        .BuildServiceProvider();

    private readonly FakeTimeProvider _time = new();
    private readonly ToastService _toastService;

    public ToastServiceTests()
    {
        _toastService = CreateService(_options);
        _toastService.PropertyChanged += (_, e) => _changes.Add(e.PropertyName);
    }

    public static TheoryData<ToastSeverity, Func<IToastService, Toast>> SeverityShortcuts => new()
    {
        { ToastSeverity.Information, static toasts => toasts.ShowInformation("Title", "Message") },
        { ToastSeverity.Success, static toasts => toasts.ShowSuccess("Title", "Message") },
        { ToastSeverity.Warning, static toasts => toasts.ShowWarning("Title", "Message") },
        { ToastSeverity.Error, static toasts => toasts.ShowError("Title", "Message") }
    };

    public void Dispose()
    {
        _toastService.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public void Show_Should_ShowNewestToastFirst()
    {
        var first = _toastService.Show("First", "Message");
        var second = _toastService.Show("Second", "Message");

        _toastService.Toasts.Should().Equal(second, first);
        _changes.Should().Equal(nameof(IToastService.Toasts), nameof(IToastService.Toasts));
    }

    [Fact]
    public void Show_Should_RemoveOldestToast_When_LimitIsReached()
    {
        var toasts = Enumerable.Range(0, _options.MaxCount + 1)
            .Select(index => _toastService.Show($"Toast {index}", "Message"))
            .ToList();

        _toastService.Toasts.Should().HaveCount(_options.MaxCount).And.NotContain(toasts[0]);
    }

    [Fact]
    public void Show_Should_CreateToastWithTitleMessageAndSeverity()
    {
        var toast = _toastService.Show("Saved", "Your changes were saved.", ToastSeverity.Success);

        toast.Title.Should().Be("Saved");
        toast.Message.Should().Be("Your changes were saved.");
        toast.Severity.Should().Be(ToastSeverity.Success);
    }

    [Theory]
    [MemberData(nameof(SeverityShortcuts))]
    public void ShowSeverity_Should_ShowToastWithThatSeverity(ToastSeverity severity, Func<IToastService, Toast> show)
    {
        var toast = show(_toastService);

        toast.Severity.Should().Be(severity);
        _toastService.Toasts.Should().Equal(toast);
    }

    [Fact]
    public void Show_Should_ShowCustomToast()
    {
        var toast = new UndoToast();

        _toastService.Show(toast);

        _toastService.Toasts.Should().Equal(toast);
    }

    [Fact]
    public void ShowOfT_Should_CreateToastFromItsOwnScope()
    {
        var toast = _toastService.Show<ScopedToast>();

        _toastService.Toasts.Should().Equal(toast);
        _toastService.Scopes.For(toast)!.GetRequiredService<Greeting>().Should().BeSameAs(toast.Greeting);
    }

    [Fact]
    public async Task ShowOfT_Should_DisposeScope_When_ToastCloses()
    {
        var toast = _toastService.Show<ScopedToast>();

        _toastService.Dismiss(toast);
        await WaitUntilAsync(() => toast.IsDisposed);

        toast.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void ShowType_Should_ShowToastLikeGenericShow()
    {
        var toast = _toastService.Show(typeof(ScopedToast));

        toast.Should().BeOfType<ScopedToast>();
        _toastService.Toasts.Should().Equal(toast);
    }

    [Fact]
    public void ShowType_Should_Throw_When_TypeIsNotAToast()
    {
        var show = () => _toastService.Show(typeof(Greeting));

        show.Should().Throw<ArgumentException>();
        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void Position_Should_BeTopRight_ByDefault() =>
        _toastService.Position.Should().Be(ToastPosition.TopRight);

    [Fact]
    public void Position_Should_RaisePropertyChangedOnce_When_ItChanges()
    {
        _toastService.Position = ToastPosition.BottomLeft;
        _toastService.Position = ToastPosition.BottomLeft;

        _toastService.Position.Should().Be(ToastPosition.BottomLeft);
        _changes.Should().Equal(nameof(IToastService.Position));
    }

    [Fact]
    public void Show_Should_UseConfiguredDuration_When_NoneIsGiven()
    {
        using var toastService = CreateService(new ToastOptions { Duration = TimeSpan.FromSeconds(2) });
        toastService.ShowInformation("Title", "Message");

        _time.Advance(TimeSpan.FromSeconds(2));

        toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void Show_Should_PreferGivenDuration_Over_ConfiguredDuration()
    {
        using var toastService = CreateService(new ToastOptions { Duration = TimeSpan.FromSeconds(2) });
        toastService.ShowInformation("Title", "Message", TimeSpan.FromSeconds(10));

        _time.Advance(TimeSpan.FromSeconds(2));

        toastService.Toasts.Should().HaveCount(1);
    }

    [Fact]
    public void Show_Should_KeepConfiguredMaxCount()
    {
        using var toastService = CreateService(new ToastOptions { MaxCount = 2 });

        var first = toastService.ShowInformation("First", "");
        toastService.ShowInformation("Second", "");
        toastService.ShowInformation("Third", "");

        toastService.Toasts.Should().HaveCount(2).And.NotContain(first);
    }

    [Fact]
    public void Position_Should_StartAtConfiguredPosition()
    {
        using var toastService = CreateService(new ToastOptions { Position = ToastPosition.BottomLeft });

        toastService.Position.Should().Be(ToastPosition.BottomLeft);
    }

    [Fact]
    public void Dismiss_Should_RemoveToast()
    {
        var toast = _toastService.Show("Title", "Message");

        var dismissed = _toastService.Dismiss(toast);

        dismissed.Should().BeTrue();
        _toastService.Toasts.Should().BeEmpty();
        _toastService.Dismiss(toast).Should().BeFalse();
    }

    [Fact]
    public void Show_Should_DismissToast_When_DefaultDurationPassed()
    {
        _toastService.Show("Title", "Message");

        _time.Advance(_options.Duration - TimeSpan.FromMilliseconds(1));
        _toastService.Toasts.Should().HaveCount(1);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void Show_Should_UseGivenDuration()
    {
        _toastService.Show("Title", "Message", duration: TimeSpan.FromSeconds(1));

        _time.Advance(TimeSpan.FromSeconds(1));

        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void Show_Should_KeepToast_When_DurationIsInfinite()
    {
        _toastService.Show("Title", "Message", duration: Timeout.InfiniteTimeSpan);

        _time.Advance(TimeSpan.FromDays(1));

        _toastService.Toasts.Should().HaveCount(1);
    }

    [Fact]
    public void Show_Should_MoveToTopAndRestartDuration_When_ToastIsShownAgain()
    {
        var toast = _toastService.Show("Again", "Message");
        var other = _toastService.Show("Other", "Message");
        _time.Advance(TimeSpan.FromSeconds(4));

        _toastService.Show(toast);
        _time.Advance(TimeSpan.FromSeconds(4));

        _toastService.Toasts.Should().Equal(toast);
        other.Should().NotBeNull();
    }

    [Fact]
    public void Show_Should_Throw_When_DurationIsNotPositive()
    {
        var show = () => _toastService.Show(new UndoToast(), TimeSpan.Zero);

        show.Should().Throw<ArgumentOutOfRangeException>();
        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void Show_Should_Throw_When_DurationIsLongerThanTimersAllow()
    {
        var show = () => _toastService.Show(new UndoToast(), TimeSpan.FromDays(60));

        show.Should().Throw<ArgumentOutOfRangeException>();
        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void ToastViewModel_Should_DismissItself()
    {
        var toast = new SelfDismissingToast();
        _toastService.Show(toast);

        toast.DismissSelf().Should().BeTrue();

        _toastService.Toasts.Should().BeEmpty();
    }

    [Fact]
    public void ToastViewModel_Should_ReturnFalse_When_NotShown() =>
        new SelfDismissingToast().DismissSelf().Should().BeFalse();

    [Fact]
    public void Dispose_Should_ClearToastsAndStopTimers()
    {
        _toastService.Show("Title", "Message");
        _changes.Clear();

        _toastService.Dispose();
        _time.Advance(_options.Duration);

        _toastService.Toasts.Should().BeEmpty();
        _changes.Should().Equal(nameof(IToastService.Toasts));
        _toastService.Invoking(static service => service.Show(new UndoToast()))
            .Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Show_Should_KeepConsistentState_When_CalledFromManyThreads()
    {
        var toasts = Enumerable.Range(0, 200).Select(static _ => new UndoToast()).ToList();

        await Parallel.ForEachAsync(toasts, CurrentCancellationToken, (toast, _) =>
        {
            _toastService.Show(toast);
            _toastService.Dismiss(toast);
            return ValueTask.CompletedTask;
        });

        _toastService.Toasts.Should().BeEmpty();
    }


    private ToastService CreateService(ToastOptions options) =>
        new(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _time,
            options,
            new ViewModelScopes(_serviceProvider),
            NullLogger<ToastService>.Instance);
}
