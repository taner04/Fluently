using Avalonia;
using Avalonia.Headless;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

internal static class UiThread
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(UiThread)));

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    internal static Task Run(Action test) =>
        RunAsync(() =>
        {
            test();
            return Task.CompletedTask;
        });

    internal static Task RunAsync(Func<Task> test) =>
        Session.Value.Dispatch(async () =>
        {
            var windows = TestWindow.BeginTest();
            try
            {
                await test();
            }
            finally
            {
                foreach (var window in windows)
                {
                    window.Close();
                }
            }

            return true;
        }, TestContext.Current.CancellationToken);
}
