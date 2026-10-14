# Fluently

A view-model-first toolkit for .NET desktop apps.

| Package | Description |
|---|---|
| `Fluently.Core` | Async navigation with back and forward history, guards, typed parameters and rollback when a navigation fails. No UI dependency, so it works with Avalonia, WPF or any other MVVM setup. |

## Features

- **View-model-first** — navigate to a view model type; your UI framework maps it to a view
- **Opt-in async lifecycle** — implement `INavigatedTo` or `INavigatedFrom` only where a page needs them
- **Typed parameters** — `INavigatedTo<TParameter>` and `NavigateToAsync<TPage, TParameter>` check the parameter type at compile time
- **Navigation guards** — `ICanNavigateFrom` lets a page block leaving it, for example with unsaved changes
- **History** — `GoBackAsync` and `GoForwardAsync` with `CanGoBack` and `CanGoForward`; navigating to a new page clears the forward history
- **Rollback** — if entering a page throws, Fluently leaves it again and restores the previous page
- **Cancellation** — every navigation takes a `CancellationToken`; a canceled navigation also restores the previous page
- **Serialized** — overlapping navigations run one after another
- **Scoped pages** — every history entry gets its own service scope, which is disposed when the entry leaves the history
- **Nested navigation** — `INavigationServiceFactory` creates independent services with their own history

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

Every history entry resolves its page from its own service scope. Transient pages are created on every navigation and disposed, together with their scoped and transient dependencies, when the entry leaves the history (forward history cleared, page replaced, navigation failed or the service disposed). Singleton pages keep their state and are never disposed by Fluently.

Mark your pages. A page implements `INavigableViewModel`, or one or both lifecycle interfaces when it needs to react to navigation:

| Interface | Use it when the page |
|---|---|
| `INavigableViewModel` | only needs to be navigable |
| `INavigatedTo` | loads data when it is shown |
| `INavigatedTo<TParameter>` | needs a parameter of a specific type when it is shown |
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
    public Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) => LoadDraftAsync(cancellationToken);

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken) => SaveDraftAsync(cancellationToken);

    public Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken) =>
        HasUnsavedChanges ? dialogs.ConfirmAsync("Discard your changes?") : Task.FromResult(true);
}
```

Navigate:

```csharp
await navigationService.NavigateToAsync<ChatViewModel, ChatId>(chatId);
await navigationService.NavigateToAsync<AboutViewModel>();
await navigationService.GoBackAsync();
```

Bind your content area to `CurrentPage` and update it from the `Navigated` event. In Avalonia, a `ViewLocator` turns the view model into its view.

`CanGoBack` and `CanGoForward` only change with a navigation, so re-evaluate your back and forward commands in the `Navigated` handler:

```csharp
navigationService.Navigated += (_, _) => GoBackCommand.NotifyCanExecuteChanged();

[RelayCommand(CanExecute = nameof(CanGoBack))]
private Task GoBackAsync() => navigationService.GoBackAsync();

private bool CanGoBack() => navigationService.CanGoBack;
```

A service from `INavigationServiceFactory` belongs to you: dispose it when its navigation is no longer needed.

## Behavior

| Call | Result |
|---|---|
| Navigate to the current page without a parameter, or with the same parameter | Returns `false`, nothing happens |
| Navigate to the current page with a different parameter | Re-enters the page and replaces the current history entry |
| `CanNavigateFromAsync` returns `false` or throws | Returns `false`, the current page stays and nothing else is called |
| `OnNavigatedToAsync` throws | Returns `false`, the failed page is left and the previous page is entered again |
| A typed page gets no parameter or one of the wrong type | Handled like `OnNavigatedToAsync` throwing |
| Navigation is canceled | The previous page is entered again, `OperationCanceledException` is thrown |

## License

Fluently is licensed under the [MIT License](LICENSE).
