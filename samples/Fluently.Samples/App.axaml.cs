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

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddFluently()
                .AddFluentlyAvalonia(typeof(App).Assembly)
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<HomeViewModel>()
                .AddTransient<DetailsViewModel>()
                .BuildServiceProvider();

            this.UseFluently(services);
            desktop.MainWindow = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
            desktop.Exit += (_, _) => services.Dispose();

            await services.GetRequiredService<INavigationService>().NavigateToAsync<HomeViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
