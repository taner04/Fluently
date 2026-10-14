# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
