# Fluently

[![CI](https://github.com/taner04/Fluently/actions/workflows/ci.yml/badge.svg)](https://github.com/taner04/Fluently/actions/workflows/ci.yml)
[![Fluently.Core](https://img.shields.io/nuget/v/Fluently.Core?label=Fluently.Core)](https://www.nuget.org/packages/Fluently.Core)
[![Fluently.Avalonia](https://img.shields.io/nuget/v/Fluently.Avalonia?label=Fluently.Avalonia)](https://www.nuget.org/packages/Fluently.Avalonia)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A view-model-first toolkit for .NET desktop apps.

| Package | Description |
|---|---|
| `Fluently.Core` | Async navigation with back and forward history, guards, typed parameters and rollback when a navigation fails, plus source-generated view registration. No UI dependency, so view model projects only need this package. |
| `Fluently.Avalonia` | Avalonia integration: a `ViewLocator` you add in XAML and a `NavigationHost` control that shows the current page. |

## Features

- **View-model-first** — navigate to a view model type; your UI framework maps it to a view
- **Opt-in async lifecycle** — implement `INavigatedTo` or `INavigatedFrom` only where a page needs them
- **Typed parameters** — `INavigatedTo<TParameter>` and `NavigateToAsync<TPage, TParameter>` check the parameter type at compile time
- **Navigation guards** — `ICanNavigateFrom` lets a page block leaving it, for example with unsaved changes
- **History** — `GoBackAsync` and `GoForwardAsync`, bindable `CanGoBack` and `CanGoForward`, and `ClearHistoryAsync` after signing in or out; navigating to a new page clears the forward history
- **Rollback** — if entering a page throws, Fluently leaves it again and restores the previous page
- **Cancellation** — every navigation takes a `CancellationToken`; a canceled navigation also restores the previous page
- **Serialized** — overlapping navigations run one after another
- **Scoped pages** — every history entry gets its own service scope, which is disposed, asynchronously where needed, when the entry leaves the history
- **Nested navigation** — `INavigationServiceFactory` creates independent services with their own history

## Requirements

- .NET 8 or later (the packages target `net8.0` and `net10.0`)
- `Fluently.Avalonia`: Avalonia 12.1 or later

## Getting Started

```bash
dotnet add package Fluently.Core
```

Register Fluently and your pages:

```csharp
services.AddLogging();
services.AddFluently();
services.AddTransient<HomeViewModel>();
services.AddSingleton<SettingsViewModel>();
```

Every history entry resolves its page from its own service scope. Transient pages are created on every navigation and disposed, together with their scoped and transient dependencies, when the entry leaves the history (forward history cleared, history cleared with `ClearHistoryAsync`, page replaced, navigation failed or the service disposed). Singleton pages keep their state and are never disposed by Fluently.

Mark your pages. A page implements `INavigableViewModel`, or one or both lifecycle interfaces when it needs to react to navigation:

| Interface | Use it when the page |
|---|---|
| `INavigableViewModel` | only needs to be navigable |
| `INavigatedTo` | loads data when it is shown without a parameter |
| `INavigatedTo<TParameter>` | is shown with a parameter of type `TParameter`; a page can implement it for several types, and together with `INavigatedTo` when it can also be shown without one |
| `INavigatedFrom` | saves state or stops work when it is left |
| `ICanNavigateFrom` | may refuse to be left, for example with unsaved changes |

```csharp
public sealed class AboutViewModel : INavigableViewModel;

public sealed class ChatViewModel : INavigatedTo<ChatId>
{
    public Task OnNavigatedToAsync(ChatId chatId, CancellationToken cancellationToken) =>
        LoadMessagesAsync(chatId, cancellationToken);
}

public sealed class EditorViewModel : INavigatedTo, INavigatedFrom, ICanNavigateFrom
{
    public Task OnNavigatedToAsync(CancellationToken cancellationToken) => LoadDraftAsync(cancellationToken);

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken) => SaveDraftAsync(cancellationToken);

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken) =>
        HasUnsavedChanges ? dialogs.ConfirmAsync("Discard your changes?") : Task.FromResult(true);
}
```

Navigate:

```csharp
await navigationService.NavigateToAsync<ChatViewModel, ChatId>(chatId);
await navigationService.NavigateToAsync<AboutViewModel>();
await navigationService.NavigateToAsync(menuItem.ViewModelType); // when the type is only known at run time
await navigationService.GoBackAsync();
```

`CurrentPage`, `CanGoBack` and `CanGoForward` raise `PropertyChanged`, so you can bind to them directly. After every completed navigation, `Navigated` is raised with the new page. In Avalonia, the `NavigationHost` shows `CurrentPage` as its view (see [Avalonia](#avalonia)).

```xml
<Button Content="Back" Command="{Binding GoBackCommand}" IsEnabled="{Binding Navigation.CanGoBack}" />
```

After signing in or out, clear the history so going back cannot return to the previous session:

```csharp
await navigationService.NavigateToAsync<LoginViewModel>();
await navigationService.ClearHistoryAsync();
```

A service from `INavigationServiceFactory` belongs to you: dispose it when its navigation is no longer needed, preferably with `DisposeAsync`, which also disposes services that can only be disposed asynchronously.

## Avalonia

```bash
dotnet add package Fluently.Avalonia
```

`Fluently.Avalonia` brings `Fluently.Core` and its view generator with it, so an Avalonia app only needs this package.

Mark each view with `IViewFor<TViewModel>`. The first constructor parameter is the view model; any further parameters are services:

```csharp
internal partial class ChatView : UserControl, IViewFor<ChatViewModel>
{
    public ChatView(ChatViewModel viewModel, IAudioPlayer audio)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public ChatViewModel ViewModel { get; }
}
```

Add the view locator to your application's data templates. That is all the setup there is:

```xml
<!-- App.axaml -->
<Application xmlns:fluently="https://github.com/taner04/fluently">
    <Application.DataTemplates>
        <fluently:ViewLocator />
    </Application.DataTemplates>
</Application>
```

Show the current page with a `NavigationHost`:

```xml
<Window xmlns:fluently="https://github.com/taner04/fluently">
    <fluently:NavigationHost Navigation="{Binding Navigation}">
        <fluently:NavigationHost.PageTransition>
            <CrossFade Duration="0:0:0.15" />
        </fluently:NavigationHost.PageTransition>
    </fluently:NavigationHost>
</Window>
```

### How views are found and created

- A source generator that ships with `Fluently.Core` registers every non-abstract `IViewFor<TViewModel>` view of your project when the assembly loads, so there is no registration code. It writes the constructor call itself, without reflection, so it works with trimming and Native AOT.
- If a view has several constructors, the one with the view model as its first parameter and the most parameters is used. Further parameters come from the services; nullable ones are optional.
- The services of a page's view come from the same scope as the page itself, so a view and its page share scoped services, and both are disposed when the page leaves the history.
- Views of view models that Fluently did not create, for example items you create yourself, get their services from the application's root provider. That provider is known once the container has created the `INavigationService`.
- The view locator creates one view per view model instance and reuses it, so a singleton page keeps its view, including scroll position and input, when you navigate back to it.
- To register a view the generator cannot see, for example from another assembly, call `ViewRegistry.Add<ChatViewModel>((services, viewModel) => new ChatView(viewModel))`.

The compiler reports an error when a view does not derive from the UI element type of the UI framework you use, for example Avalonia's `Control` (`FLUENTLY001`), when a view is private (`FLUENTLY002`), when a view model has two views (`FLUENTLY003`), or when a view has no constructor whose first parameter is its view model (`FLUENTLY004`).

`samples/Fluently.Samples` is a small runnable app that shows all of this.

## Behavior

| Call | Result |
|---|---|
| Navigate to the current page without a parameter, or with an equal parameter (compared with `Equals`) | Returns `false`, nothing happens |
| Navigate to the current page with a different parameter | Re-enters the page and replaces the current history entry |
| `CanNavigateFromAsync` returns `false` or throws | Returns `false`, the current page stays and nothing else is called |
| `OnNavigatedToAsync` throws | Returns `false`, the failed page is left and the previous page is entered again |
| A page that only implements `INavigatedTo<TParameter>` is navigated to without a parameter | Returns `false` and logs that the page needs a parameter |
| `NavigateToAsync(Type)` with a type that does not implement `INavigableViewModel` | Throws `ArgumentException` |
| `GoBackAsync` or `GoForwardAsync` without a previous or next page | Returns `false` |
| Navigation is canceled | The previous page is entered again, `OperationCanceledException` is thrown |
| Several navigations at once | They run one after another; one canceled while waiting throws without touching any page |
| `ClearHistoryAsync` | Previous and next pages are released without guards or `OnNavigatedFromAsync`; the current page stays |

## Building from source

```bash
dotnet build Fluently.slnx
dotnet test --solution Fluently.slnx
dotnet run --project samples/Fluently.Samples
```

The tests run on .NET 8 and .NET 10, so both SDKs or runtimes need to be installed. Releases are published to NuGet from GitHub Actions when a `v*` tag is pushed; the version comes from the tag.

## License

Fluently is licensed under the [MIT License](LICENSE).
