using Avalonia;
using Avalonia.Headless;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

internal static class UiThread
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(UiThread)));

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    internal static Task RunAsync(Func<Task> test) =>
        Session.Value.Dispatch(async () =>
        {
            await test();
            return true;
        }, TestContext.Current.CancellationToken);
}