# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0-preview.1] - 2026-10-19

### Breaking changes

- `ViewRegistry.Add` takes a factory that returns an `IViewFor<TViewModel>` instead of an `object`, so every view implements `IViewFor<TViewModel>`. Generated registrations are unaffected
- `NavigateToAsync<TPage>()` and `NavigateToAsync<TPage, TParameter>()` moved from `INavigationService` to the new `NavigationServiceExtensions`. Calls compile unchanged; classes that implement `INavigationService`, and mocks that set up these overloads, use the `Type` members instead

### Added

- `IViewProvider` decides how views are created. `AddFluently()` registers the default `ViewProvider`; register your own to change view creation, and inject `ViewProvider` to fall back to it. A view for the built-in `Toast` replaces the `ToastHost`'s built-in card
- `AddFluently(options => …)` configures Fluently. `options.Toasts` sets where toasts appear at first (`Position`), how long they stay (`Duration`) and how many are shown at once (`MaxCount`); values that are not allowed throw at startup
- `[SingletonPage]`, `[TransientPage]`, `[ScopedPage]`, `[Dialog]` and `[Toast]` mark view models for the source generator, which writes an `AddViewModels()` method into each project that registers them with that lifetime. Registrations made by hand are kept. The compiler reports a mismatched attribute (`FLUENTLY005`), several attributes on one class (`FLUENTLY006`) and classes the container cannot create (`FLUENTLY007`)
- `INavigationService.NavigateToAsync<TParameter>(Type, TParameter)` navigates to a page known at run time with a typed parameter. Every service now has a generic and a `Type` overload for each operation: the `Type` overloads are the interface members, and the generic ones are extension methods
- `ViewFor<TViewModel>`, a `UserControl` base class for Avalonia views that keeps the view model in `ViewModel` and sets it as the `DataContext`
- `IDialogService`, registered by `AddFluently()`, shows `IDialog<TResult>` view models as a stack of dialogs and returns a `DialogResult<TResult>`. Dialogs get their own service scope and the same `INavigatedTo`, `INavigatedTo<TParameter>` and `INavigatedFrom` calls as pages. `RequestDismissAsync` closes a dialog on the user's behalf unless its `ICanNavigateFrom` guard refuses; the guard may show another dialog to ask first
- `DialogViewModel<TResult>` and `ToastViewModel`, optional base classes for dialogs and toasts that close themselves with `CloseAsync`, `DismissAsync`, `RequestDismissAsync` and `Dismiss`, without injecting the service. They do not implement `INotifyPropertyChanged`, so Fluently stays free of MVVM libraries; with CommunityToolkit.Mvvm, add `[ObservableObject]`
- `IToastService`, registered by `AddFluently()`, shows short notifications that close by themselves: the built-in `Toast` with a title, message and `ToastSeverity`, or any `IToast` view model with its own view. `ShowInformation`, `ShowSuccess`, `ShowWarning` and `ShowError` show the built-in toast with that severity. `Position` sets where toasts appear: top or bottom, left, center or right; top right by default. The newest comes first, at most five are shown, and toasts can be shown from any thread
- `ToastHost`, an Avalonia control that shows the toasts of an `IToastService` above its content at the service's `Position`, and slides them in and out with Fluent motion
- `FluentWindow`, a base class for Avalonia windows whose content extends into the title bar, with Mica on Windows 11. Its `Navigation` is passed on to the `TitleBar` and `NavigationPane` inside it, and the mouse's back and forward buttons navigate it
- `TitleBar`, which shows a back button, an icon, the window's title and your content in a `FluentWindow`, leaves room for the window buttons, and moves and maximizes the window
- `NavigationPane`, a side pane of `NavigationPaneItem`s that navigates an `INavigationService` and selects the item of the current page, also after going back or when a guard refuses. It has a back button, collapses to its icons, finds items with an optional search box that Ctrl+L, Cmd+L or a search button in the collapsed pane focuses, separates its sections with thin lines, and has room for footer items and your own content. It is built from Avalonia's standard controls, so it takes the look of your theme
- `DialogHost`, an Avalonia control that shows the open dialogs of an `IDialogService` above its content, with a backdrop, focus handling, and dismissal by Escape or, with `DismissOnBackdropClick`, by clicking the backdrop

### Changed

- `Fluently.Core` and `Fluently.Avalonia` have their own package README and a package icon
- The sample app is now the gallery in `src/Fluently.Gallery`, modeled on the WPF UI gallery: a `FluentWindow` with a navigation pane, live examples with their C# and XAML, and a settings page

### Fixed

- A singleton page shown again with a new scope gets a new view instead of a view holding services of the disposed scope
- Views of view models nested inside a `NavigationHost` come from the container of the host's navigation service, not the most recently created container
- Several containers in one process no longer share scopes: every container keeps its own. Before, views of view models that Fluently did not create could get services from the most recently created container
- The view generator registers views that implement `IViewFor<TViewModel>` through a base class

## [0.2.2] - 2026-10-14

### Fixed

- The `Fluently.Avalonia` package description no longer mentions `AddFluentlyViews()`, which does not exist
- The package README describes `Navigated`, `ClearHistoryAsync` disposal and more navigation results

## [0.2.1] - 2026-10-14

### Fixed

- A navigation canceled while waiting for another navigation could still run when the cancellation and the end of the other navigation happened at almost the same time
- `Fluently.Core` ships its symbol package again; it was missing from 0.2.0

## [0.2.0] - 2026-10-14

First release of `Fluently.Avalonia`.

### Breaking changes

- `INavigatedTo.OnNavigatedToAsync` no longer has a parameter. Pages that need one implement `INavigatedTo<TParameter>`
- `INavigatedTo<TParameter>` no longer derives from `INavigatedTo` and has no hidden default implementation, so a page can implement it for several parameter types
- `NavigateToAsync<TPage>()` and `NavigateToAsync<TPage, TParameter>()` are members of `INavigationService` instead of extension methods; `NavigationExtensions` is removed
- `NavigateToAsync(Type, object)` is removed; navigate to pages with a parameter with `NavigateToAsync<TPage, TParameter>()`

### Added

- Support for .NET 8: both packages target `net8.0` and `net10.0`, and the source generator runs on the .NET 8 SDK
- `IViewFor<TViewModel>` and `ViewRegistry` in `Fluently.Core`, with a source generator that registers every view of a project when the assembly loads, independent of the UI framework and without reflection, with the diagnostics `FLUENTLY001` to `FLUENTLY004`
- Views can take services as further constructor parameters; a page's view gets them from the page's scope
- `ClearHistoryAsync()` to remove the previous and next pages, for example after signing in or out
- `INavigationService` raises `PropertyChanged` for `CurrentPage`, `CanGoBack` and `CanGoForward`, so views can bind to them
- `INavigationService` is `IAsyncDisposable`, and history entries are disposed asynchronously, so services that only implement `IAsyncDisposable` are disposed too
- `ViewRegistry.Add<TViewModel>()` to register views by hand
- `Fluently.Avalonia` package with a `ViewLocator` that is added in XAML and a `NavigationHost`
- Sample app in `samples/Fluently.Samples`

### Changed

- `Fluently.Core` depends on the 8.0 versions of `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`, so apps are not forced onto newer versions

## [0.1.0] - 2026-10-14

First release of `Fluently.Core`.

### Added

- `INavigationService` with async, view-model-first navigation, back and forward history, rollback on failed navigation and cancellation
- `CanGoBack` and `CanGoForward`
- A service scope per history entry; transient pages and their dependencies are disposed when the entry leaves the history
- `INavigationServiceFactory` for independent navigation services, which are disposable
- `INavigableViewModel` to mark pages, and the opt-in lifecycle interfaces `INavigatedTo` and `INavigatedFrom`
- `INavigatedTo<TParameter>` and `NavigateToAsync<TPage, TParameter>()` for typed navigation parameters
- `ICanNavigateFrom` navigation guard
- `AddFluently()` to register the services
- Generic `NavigateToAsync<T>()` overloads

[Unreleased]: https://github.com/taner04/Fluently/compare/v0.3.0-preview.1...HEAD
[0.3.0-preview.1]: https://github.com/taner04/Fluently/compare/v0.2.2...v0.3.0-preview.1
[0.2.2]: https://github.com/taner04/Fluently/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/taner04/Fluently/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/taner04/Fluently/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/taner04/Fluently/releases/tag/v0.1.0
