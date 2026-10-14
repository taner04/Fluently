using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Fluently.Samples.ViewModels;
using Fluently.Samples.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently.Samples;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddFluently()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<HomeViewModel>()
                .AddTransient<DetailsViewModel>()
                .BuildServiceProvider();

            desktop.MainWindow = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            desktop.Exit += (_, _) => services.Dispose();

            await services.GetRequiredService<INavigationService>().NavigateToAsync<HomeViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
