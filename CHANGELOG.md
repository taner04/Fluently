# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/taner04/Fluently/compare/v0.2.2...HEAD
[0.2.2]: https://github.com/taner04/Fluently/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/taner04/Fluently/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/taner04/Fluently/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/taner04/Fluently/releases/tag/v0.1.0
