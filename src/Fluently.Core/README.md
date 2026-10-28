# Fluently.Core

[![NuGet](https://img.shields.io/nuget/v/Fluently.Core)](https://www.nuget.org/packages/Fluently.Core)

The UI-neutral core of [Fluently](https://github.com/taner04/Fluently): async, view-model-first navigation with back
and forward history, typed parameters, guards and rollback, dialogs that return a result, and toasts. It has no UI
dependency, so view model projects only need this package. For Avalonia apps, take
[`Fluently.Avalonia`](https://github.com/taner04/Fluently/blob/main/src/Fluently.Avalonia/README.md), which brings this
package with it.

```bash
dotnet add package Fluently.Core
```

Requires .NET 8 or later; the package targets `net8.0` and `net10.0`.

## Features

- **View-model-first**: navigate to a view model type; your UI framework maps it to a view
- **Opt-in async lifecycle**: implement `INavigatedTo` or `INavigatedFrom` only where a page needs them
- **Typed parameters**: `INavigatedTo<TParameter>` and `NavigateToAsync<TPage, TParameter>` check the parameter type at
  compile time
- **Navigation guards**: `ICanNavigateFrom` lets a page refuse to be left, for example with unsaved changes
- **History**: `GoBackAsync` and `GoForwardAsync`, bindable `CanGoBack` and `CanGoForward`, and `ClearHistoryAsync`
  after signing in or out
- **Rollback**: if entering a page throws, it is left again and the previous page is restored
- **Cancellation**: every navigation takes a `CancellationToken`; a canceled navigation also restores the previous page
- **Serialized**: overlapping navigations run one after another
- **Scoped pages**: every history entry gets its own service scope, disposed, asynchronously where needed, when the
  entry leaves the history
- **Nested navigation**: `INavigationServiceFactory` creates independent services with their own history
- **Dialogs**: `IDialogService` shows `IDialog<TResult>` view models and returns their result; dialogs stack, get their
  own scope, and can refuse being dismissed
- **Toasts**: `IToastService` shows short notifications that close by themselves, built-in or with a view of your own
- **Theme switching**: `IThemeService` switches between light, dark and the system theme, bindable and from any thread
- **Generated registration**: views and view models are registered by a source generator, without reflection

## Registration

Mark your pages with an attribute that says how long they live, and register Fluently:

```csharp
[TransientPage]
public sealed class HomeViewModel : INavigableViewModel;

[SingletonPage]
public sealed class SettingsViewModel : INavigableViewModel;
```

```csharp
services.AddLogging();
services.AddFluently();
services.AddViewModels();
```

`AddViewModels()` is generated into your project and registers every view model with `[SingletonPage]`,
`[TransientPage]`, `[ScopedPage]`, `[Dialog]` or `[Toast]`. The
[source generator README](https://github.com/taner04/Fluently/blob/main/src/Fluently.SourceGenerator/README.md)
describes the attributes, the generated code and the compiler errors it reports. Registering view models by hand works
as
well.

`AddFluently()` registers a singleton `INavigationService`, an `INavigationServiceFactory`, a singleton
`IDialogService`, a singleton `IToastService`, the default `IViewProvider` and, unless you registered one,
`TimeProvider.System` to time the toasts. It needs logging to be registered. Configure it with options; every value is
checked right away, and one that is not allowed throws an `ArgumentOutOfRangeException` naming it:

```csharp
services.AddFluently(options =>
{
    options.Toasts.Position = ToastPosition.BottomRight;
    options.Toasts.Duration = TimeSpan.FromSeconds(8);
    options.Toasts.MaxCount = 3;
});
```

| Option            | Default    | Allowed                                                                                                   |
|-------------------|------------|-----------------------------------------------------------------------------------------------------------|
| `Toasts.Position` | `TopRight` | any `ToastPosition`                                                                                       |
| `Toasts.Duration` | 5 seconds  | more than zero and at most 49 days, or `Timeout.InfiniteTimeSpan` to keep toasts until they are dismissed |
| `Toasts.MaxCount` | 5          | 1 or more                                                                                                 |

## Pages

A page implements `INavigableViewModel`, or one or more lifecycle interfaces when it needs to react to navigation:

| Interface                  | Use it when the page                                                                                                                                              |
|----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `INavigableViewModel`      | only needs to be navigable                                                                                                                                        |
| `INavigatedTo`             | loads data when it is shown without a parameter                                                                                                                   |
| `INavigatedTo<TParameter>` | is shown with a parameter of type `TParameter`; a page can implement it for several types, and together with `INavigatedTo` when it can also be shown without one |
| `INavigatedFrom`           | saves state or stops work when it is left                                                                                                                         |
| `ICanNavigateFrom`         | may refuse to be left, for example with unsaved changes                                                                                                           |

```csharp
public sealed class ChatViewModel : INavigatedTo<ChatId>
{
    public Task OnNavigatedToAsync(ChatId chatId, CancellationToken cancellationToken) =>
        LoadMessagesAsync(chatId, cancellationToken);
}

public sealed class EditorViewModel(IDialogService dialogs) : INavigatedTo, INavigatedFrom, ICanNavigateFrom
{
    public Task OnNavigatedToAsync(CancellationToken cancellationToken) => LoadDraftAsync(cancellationToken);

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken) => SaveDraftAsync(cancellationToken);

    public async Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken) =>
        !HasUnsavedChanges ||
        (await dialogs.ShowAsync<DiscardChangesViewModel, bool>(cancellationToken)) is { IsCompleted: true, Value: true };
}
```

Every history entry resolves its page from its own service scope. A transient page is created on every navigation and
disposed, with its scoped and transient dependencies, when its entry leaves the history: when the forward history is
cleared, the history is cleared with `ClearHistoryAsync`, the page is replaced, the navigation failed or the service is
disposed. A singleton page keeps its state and is never disposed by Fluently.

## Navigation

```csharp
await navigation.NavigateToAsync<ChatViewModel, ChatId>(chatId);
await navigation.NavigateToAsync<SettingsViewModel>();
await navigation.NavigateToAsync(menuItem.ViewModelType);
await navigation.NavigateToAsync(menuItem.ViewModelType, chatId);
await navigation.GoBackAsync();
```

- `CurrentPage`, `CanGoBack` and `CanGoForward` raise `PropertyChanged`, so views can bind to them. After every
  completed navigation, `Navigated` is raised with the new page.
- After signing in or out, `ClearHistoryAsync()` removes the previous and next pages, so going back cannot return to the
  previous session.
- A service from `INavigationServiceFactory` belongs to you: dispose it when its navigation is no longer needed,
  preferably with `DisposeAsync`, which also disposes services that can only be disposed asynchronously.

### Behavior

| Call                                                                                                  | Result                                                                                                |
|-------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Navigate to the current page without a parameter, or with an equal parameter (compared with `Equals`) | Returns `false`, nothing happens                                                                      |
| Navigate to the current page with a different parameter                                               | Re-enters the page and replaces the current history entry                                             |
| `CanNavigateFromAsync` returns `false` or throws                                                      | Returns `false`, the current page stays and nothing else is called                                    |
| `OnNavigatedToAsync` throws                                                                           | Returns `false`, the failed page is left and the previous page is entered again                       |
| A page that only implements `INavigatedTo<TParameter>` is navigated to without a parameter            | Returns `false` and logs that the page needs a parameter                                              |
| `NavigateToAsync(Type)` with a type that does not implement `INavigableViewModel`                     | Throws `ArgumentException`                                                                            |
| `GoBackAsync` or `GoForwardAsync` without a previous or next page                                     | Returns `false`                                                                                       |
| Navigation is canceled                                                                                | The previous page is entered again, `OperationCanceledException` is thrown                            |
| Several navigations at once                                                                           | They run one after another; one canceled while waiting throws without touching any page               |
| `ClearHistoryAsync`                                                                                   | Previous and next pages are released without guards or `OnNavigatedFromAsync`; the current page stays |

## Dialogs

A dialog is a view model that derives from `DialogViewModel<TResult>`, registered with `[Dialog]`. It can receive a
parameter through `INavigatedTo<TParameter>` and closes itself with `CloseAsync`, `DismissAsync` or
`RequestDismissAsync`:

```csharp
[Dialog]
[ObservableObject]
public sealed partial class ConfirmDeleteViewModel : DialogViewModel<bool>, INavigatedTo<string>
{
    [ObservableProperty] public partial string Name { get; private set; } = "";

    public Task OnNavigatedToAsync(string parameter, CancellationToken cancellationToken)
    {
        Name = parameter;
        return Task.CompletedTask;
    }

    [RelayCommand] private Task DeleteAsync() => CloseAsync(true);

    [RelayCommand] private Task CancelAsync() => DismissAsync();
}
```

`DialogViewModel<TResult>` does not implement `INotifyPropertyChanged`, so Fluently does not depend on an MVVM
library. With CommunityToolkit.Mvvm, `[ObservableObject]` adds it to your class, as above. A dialog that needs a
different base class implements `IDialog<TResult>` instead and closes itself through `IDialogService`:
`dialogs.CloseAsync(this, true)`.

Show it and await the result. `IsCompleted` is `false` when the dialog was dismissed:

```csharp
var result = await dialogs.ShowAsync<ConfirmDeleteViewModel, bool, string>(chat.Name);
if (result is { IsCompleted: true, Value: true })
{
    await DeleteChatAsync(chat);
}
```

- Dialogs form a stack, so a dialog can show another one. `Dialogs` and `CurrentDialog` raise `PropertyChanged`.
- `RequestDismissAsync` closes a dialog on the user's behalf, for example when Escape is pressed. A dialog that
  implements `ICanNavigateFrom` can refuse, or show another dialog to ask first. `CloseAsync`, `DismissAsync`,
  cancellation and disposing the service always close it.
- Every dialog resolves from its own service scope, which its view shares and which is disposed when the dialog closes.
  If entering a dialog throws, it is left again and `ShowAsync` returns a dismissed result.
- Canceling the token passed to `ShowAsync` dismisses the dialog and throws `OperationCanceledException`, unless the
  dialog was already being closed: then the result it closes with is returned.

## Toasts

Show a toast with a title and a message. It closes by itself after its duration:

```csharp
toasts.ShowSuccess("Saved", "Your changes were saved.");
toasts.ShowError("Upload failed", "The server could not be reached.");
toasts.ShowWarning("Offline", "Reconnecting…", Timeout.InfiniteTimeSpan);
toasts.Show("Saved", "Your changes were saved.", ToastSeverity.Success);
```

For a toast with its own content, such as an Undo button, derive from `ToastViewModel`; your UI framework shows it
with its view, and `Dismiss()` closes it early:

```csharp
public sealed partial class UndoToastViewModel(Action undo) : ToastViewModel
{
    [RelayCommand]
    private void Undo()
    {
        undo();
        Dismiss();
    }
}

toasts.Show(new UndoToastViewModel(() => RestoreItem(item)));
```

Like `DialogViewModel<TResult>`, it does not implement `INotifyPropertyChanged`; any other `IToast` works as well and
closes itself with `toasts.Dismiss(this)`.

- `Toasts` lists the shown toasts, newest first, and raises `PropertyChanged`. At most `MaxCount` are shown; showing
  another removes the oldest. Showing a toast that is already shown makes it the newest again and restarts its
  duration.
- `Position` can be changed at any time, from a view model, settings or startup code.
- `Show<TToast>()` creates a toast from its own service scope, which is disposed when the toast closes.
- Toasts can be shown from any thread. They are timed with the container's `TimeProvider`, so tests can use a fake
  time.

## Theme

`IThemeService` switches the app between the light and the dark theme, or lets it follow the operating system. Its
implementation comes from the UI framework package, such as `AddFluentlyAvalonia()` in `Fluently.Avalonia`:

```csharp
public sealed class SettingsViewModel(IThemeService theme)
{
    public IThemeService Theme { get; } = theme;

    public IReadOnlyList<ApplicationTheme> Themes { get; } = Enum.GetValues<ApplicationTheme>();
}
```

```xml
<ComboBox ItemsSource="{Binding Themes}" SelectedItem="{Binding Theme.Theme}" />
```

- `Theme` is `System`, `Light` or `Dark`, and starts at the theme the app sets at startup. Set it from any thread.
- `ActualTheme` is the theme the app is shown in, `Light` or `Dark`; it follows the operating system while `Theme` is
  `System`.
- Both raise `PropertyChanged`. The service only switches: the colors come from your theme, and saving the choice
  between runs is up to your app.

## Views

Fluently finds the view of a view model through `IViewFor<TViewModel>`. The source generator registers every view of
your project with `ViewRegistry`; a UI package such as `Fluently.Avalonia` shows them.

- A view's further constructor parameters are services. A page's view gets them from the page's scope, so a view and
  its page share scoped services.
- To register a view the generator cannot see, for example from another assembly, call
  `ViewRegistry.Add<ChatViewModel>((services, viewModel) => new ChatView(viewModel))`.
- Views are created by the `IViewProvider` in your container. `AddFluently()` registers `ViewProvider`, which creates
  the registered views. Register your own provider to decide how views are created, and inject `ViewProvider` into it
  to fall back to the default:

  ```csharp
  public sealed class MyViewProvider(ViewProvider defaults) : IViewProvider
  {
      public bool CanProvide(object viewModel) => viewModel is Toast || defaults.CanProvide(viewModel);

      public IViewFor<object> GetView(object viewModel) =>
          viewModel is Toast toast ? new MyToastView(toast) : defaults.GetView(viewModel);
  }

  services.AddSingleton<IViewProvider, MyViewProvider>();
  ```

## Generic and `Type` overloads

Every service can be called with the view model type as a generic argument, checked at compile time, or with a `Type`
known only at run time, for example from a menu:

|                                       | Generic                                              | `Type`                                            |
|---------------------------------------|------------------------------------------------------|---------------------------------------------------|
| Navigate                              | `NavigateToAsync<TPage>()`                           | `NavigateToAsync(type)`                           |
| Navigate with a parameter             | `NavigateToAsync<TPage, TParameter>(parameter)`      | `NavigateToAsync(type, parameter)`                |
| Show a dialog                         | `ShowAsync<TDialog, TResult>()`                      | `ShowAsync<TResult>(type)`                        |
| Show a dialog with a parameter        | `ShowAsync<TDialog, TResult, TParameter>(parameter)` | `ShowAsync<TResult, TParameter>(type, parameter)` |
| Show a toast created by the container | `Show<TToast>()`                                     | `Show(type)`                                      |

The `Type` overloads are the members of `INavigationService`, `IDialogService` and `IToastService`; the generic ones
are extension methods that call them, so a test double only implements the `Type` members. The parameter type is known
at compile time in both styles, so neither needs reflection. With a `Type`, a page or dialog that does not implement
`INavigatedTo<TParameter>` for that parameter is not shown, and the call returns `false` or a dismissed result.
