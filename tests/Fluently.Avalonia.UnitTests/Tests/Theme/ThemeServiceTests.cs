using Avalonia;
using Avalonia.Styling;
using Fluently.Avalonia.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Fluently.Avalonia.UnitTests.Tests.Theme;

public sealed class ThemeServiceTests : TestBase
{
    [Fact]
    public Task ThemeService_Should_StartAtSystem_When_ApplicationRequestsNoTheme() =>
        RunWithTheme(theme =>
        {
            theme.Theme.Should().Be(ApplicationTheme.System);
            theme.ActualTheme.Should().Be(ApplicationTheme.Light);
        });

    [Fact]
    public Task ThemeService_Should_StartAtApplicationTheme_When_ApplicationRequestsOne() =>
        RunWithTheme(theme =>
        {
            theme.Theme.Should().Be(ApplicationTheme.Dark);
            theme.ActualTheme.Should().Be(ApplicationTheme.Dark);
        }, ThemeVariant.Dark);

    [Fact]
    public Task Theme_Should_SwitchApplicationTheme_When_Set() =>
        RunWithTheme(theme =>
        {
            var changes = RecordChanges(theme);

            theme.Theme = ApplicationTheme.Dark;

            Application.Current!.RequestedThemeVariant.Should().Be(ThemeVariant.Dark);
            theme.ActualTheme.Should().Be(ApplicationTheme.Dark);
            changes.Should().Equal(nameof(IThemeService.Theme), nameof(IThemeService.ActualTheme));

            theme.Theme = ApplicationTheme.System;

            Application.Current.RequestedThemeVariant.Should().Be(ThemeVariant.Default);
            theme.ActualTheme.Should().Be(ApplicationTheme.Light);
        });

    [Fact]
    public Task Theme_Should_NotRaisePropertyChanged_When_SetToSameValue() =>
        RunWithTheme(theme =>
        {
            var changes = RecordChanges(theme);

            theme.Theme = ApplicationTheme.System;

            changes.Should().BeEmpty();
        });

    [Fact]
    public Task Theme_Should_ApplyOnUiThread_When_SetFromAnotherThread() =>
        RunWithThemeAsync(async theme =>
        {
            await Task.Run(() => theme.Theme = ApplicationTheme.Dark, CurrentCancellationToken);

            theme.Theme.Should().Be(ApplicationTheme.Dark);
            await WaitUntilAsync(() => theme.ActualTheme == ApplicationTheme.Dark);

            Application.Current!.RequestedThemeVariant.Should().Be(ThemeVariant.Dark);
            theme.ActualTheme.Should().Be(ApplicationTheme.Dark);
        });

    [Fact]
    public Task Theme_Should_KeepLastValue_When_SetTwiceFromAnotherThread() =>
        RunWithThemeAsync(async theme =>
        {
            await Task.Run(() =>
            {
                theme.Theme = ApplicationTheme.Dark;
                theme.Theme = ApplicationTheme.Light;
            }, CurrentCancellationToken);
            await WaitUntilAsync(() => Application.Current!.RequestedThemeVariant == ThemeVariant.Light);

            theme.Theme.Should().Be(ApplicationTheme.Light);
            Application.Current!.RequestedThemeVariant.Should().Be(ThemeVariant.Light);
        });

    [Fact]
    public Task Theme_Should_FollowApplication_When_ApplicationThemeIsSetDirectly() =>
        RunWithTheme(theme =>
        {
            var changes = RecordChanges(theme);

            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

            theme.Theme.Should().Be(ApplicationTheme.Dark);
            theme.ActualTheme.Should().Be(ApplicationTheme.Dark);
            changes.Should().BeEquivalentTo(nameof(IThemeService.Theme), nameof(IThemeService.ActualTheme));
        });

    [Fact]
    public Task ActualTheme_Should_FollowInheritedVariant_When_ApplicationUsesCustomVariant() =>
        RunWithTheme(theme =>
        {
            Application.Current!.RequestedThemeVariant = new ThemeVariant("Midnight", ThemeVariant.Dark);

            theme.Theme.Should().Be(ApplicationTheme.Dark);
            theme.ActualTheme.Should().Be(ApplicationTheme.Dark);
        });

    [Fact]
    public Task Theme_Should_Throw_When_SetToUndefinedValue() =>
        RunWithTheme(theme =>
        {
            var set = () => theme.Theme = (ApplicationTheme)42;

            set.Should().Throw<ArgumentOutOfRangeException>();
            theme.Theme.Should().Be(ApplicationTheme.System);
        });

    [Fact]
    public Task ThemeService_Should_StopFollowingApplication_When_Disposed() =>
        UiThread.Run(() =>
        {
            var services = new ServiceCollection().AddFluentlyAvalonia().BuildServiceProvider();
            var theme = services.GetRequiredService<IThemeService>();
            try
            {
                services.GetRequiredService<IThemeService>().Should().BeSameAs(theme);

                services.Dispose();
                Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

                theme.Theme.Should().Be(ApplicationTheme.System);
            }
            finally
            {
                Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
            }
        });

    private static List<string?> RecordChanges(INotifyPropertyChanged source)
    {
        var changes = new List<string?>();
        source.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        return changes;
    }

    private static Task RunWithTheme(Action<IThemeService> test, ThemeVariant? requested = null) =>
        RunWithThemeAsync(theme =>
        {
            test(theme);
            return Task.CompletedTask;
        }, requested);

    private static Task RunWithThemeAsync(Func<IThemeService, Task> test, ThemeVariant? requested = null) =>
        UiThread.RunAsync(async () =>
        {
            var application = Application.Current!;
            application.RequestedThemeVariant = requested ?? ThemeVariant.Default;
            using var theme = new ThemeService(application);
            try
            {
                await test(theme);
            }
            finally
            {
                application.RequestedThemeVariant = ThemeVariant.Default;
            }
        });
}
