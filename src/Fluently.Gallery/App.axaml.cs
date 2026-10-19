using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Fluently.Gallery.Services;
using Fluently.Gallery.ViewModels;
using Fluently.Gallery.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fluently.Gallery;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection()
                .AddLogging(logging => logging.AddConsole())
                .AddFluently(options =>
                {
                    options.Toasts.Position = ToastPosition.BottomRight;
                    options.Toasts.Duration = TimeSpan.FromSeconds(6);
                })
                .AddViewModels()
                .AddSingleton<ItemCatalog>()
                .AddSingleton<ThemeService>()
                .AddSingleton<MainWindowViewModel>()
                .BuildServiceProvider();

            Dispatcher.UIThread.UnhandledException += (_, e) => OnUnhandledException(services, e);
            desktop.MainWindow = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            desktop.Exit += (_, _) => services.Dispose();

            await services.GetRequiredService<INavigationService>().NavigateToAsync<HomeViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OnUnhandledException(IServiceProvider services, DispatcherUnhandledExceptionEventArgs e)
    {
        services.GetRequiredService<ILogger<App>>().LogError(e.Exception, "Unhandled exception on the UI thread");
        services.GetRequiredService<IToastService>().ShowError("Something went wrong", e.Exception.Message);
        e.Handled = true;
    }
}
