# Fluently.SourceGenerator

The source generators behind Fluently. They write the registration code for your views and view models while you
build, so there is none to write by hand, and they report mistakes as compiler errors instead of failures at run
time.

This project is not a package of its own: it ships inside [`Fluently.Core`](../Fluently.Core/README.md) as an analyzer,
and [`Fluently.Avalonia`](../Fluently.Avalonia/README.md) passes it on, so every project that references either package
runs it. The generated code needs no reflection, so it works with trimming and Native AOT.

## View registration

Every non-abstract class of your project that implements `IViewFor<TViewModel>`, directly or through a base class such
as Avalonia's `ViewFor<TViewModel>`, is registered with `ViewRegistry` when the assembly loads, through a
[module initializer](https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/general#moduleinitializer-attribute):

```csharp
internal partial class ChatView : ViewFor<ChatViewModel>
{
    public ChatView(ChatViewModel viewModel, IAudioPlayer audio, ILogger<ChatView>? logger)
        : base(viewModel) => InitializeComponent();
}
```

becomes, shortened:

```csharp
namespace Fluently.Generated;

internal static class ViewRegistrations_MyApp
{
    [global::System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register()
    {
        global::Fluently.ViewRegistry.Add<global::MyApp.ChatViewModel>(static (services, viewModel) =>
            new global::MyApp.Views.ChatView(
                viewModel,
                global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<global::MyApp.IAudioPlayer>(services),
                global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetService<global::Microsoft.Extensions.Logging.ILogger<global::MyApp.Views.ChatView>>(services)));
    }
}
```

- The constructor used is the public or internal one whose first parameter is the view model; with several, the one
  with the most parameters.
- Further parameters come from the services of the view model's scope. A nullable reference type parameter is optional
  and gets `null` when the service is not registered; any other parameter is required.
- A view implements `IViewFor<TViewModel>` for the view model it shows; interfaces it only inherits from a concrete
  base view do not count, so a view derived from another view does not register a second time.
- A view must derive from the UI element type of the UI framework the project uses: Avalonia's `Control`, WPF's or
  WinUI's `FrameworkElement`, or MAUI's `VisualElement`. Only the frameworks the project references are checked.

## View model registration

Mark your view models with an attribute that says how long they live, and call the generated `AddViewModels()`:

```csharp
[TransientPage]
public sealed class HomeViewModel : INavigableViewModel;

[Dialog]
public sealed class ConfirmDialogViewModel : IDialog<bool>;
```

```csharp
services.AddFluently();
services.AddViewModels();
```

| Attribute         | Registers                                                                                 | On                            |
|-------------------|-------------------------------------------------------------------------------------------|-------------------------------|
| `[SingletonPage]` | One instance for the application; the page keeps its state                                | Pages (`INavigableViewModel`) |
| `[TransientPage]` | A new instance on every navigation                                                        | Pages                         |
| `[ScopedPage]`    | One instance per history entry, which the other services of that entry's scope can inject | Pages                         |
| `[Dialog]`        | A new instance every time it is shown                                                     | Dialogs (`IDialog<TResult>`)  |
| `[Toast]`         | A new instance every time it is shown by its type                                         | Toasts (`IToast`)             |

The generated method, shortened:

```csharp
namespace MyApp
{
    internal static class ViewModelRegistrations
    {
        internal static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            ServiceCollectionDescriptorExtensions.TryAddTransient<global::MyApp.HomeViewModel>(services);
            ServiceCollectionDescriptorExtensions.TryAddTransient<global::MyApp.ConfirmDialogViewModel>(services);
            return services;
        }
    }
}
```

- `AddViewModels()` is written into the project's root namespace and is `internal`, so every project registers its own
  view models, even when it can see another project's internals. A class library exposes them through a public method of
  its own that calls it. A root namespace that contains a C# keyword is escaped, so it always compiles.
- It uses `TryAdd`, so a view model you registered yourself keeps your registration. Registering by hand without any
  attribute works as well.
- Every project that references Fluently gets `AddViewModels()`, also before it has an attributed view model, so the
  call compiles from the start.

## Diagnostics

All diagnostics are errors, reported at the class they concern.

| Id            | Reported when                                                                                                                            | Fix                                                                                                           |
|---------------|------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `FLUENTLY001` | A view implements `IViewFor<TViewModel>` but does not derive from the UI element type of the UI framework, such as Avalonia's `Control`. | Derive the view from a control, for example `ViewFor<TViewModel>` or `UserControl`.                           |
| `FLUENTLY002` | A view is private, or nested in a private type.                                                                                          | Make the view, and every type it is nested in, `public` or `internal`.                                        |
| `FLUENTLY003` | A view model already has a view, so a second view of it is not registered.                                                               | Keep one view per view model, or register the other one yourself with `ViewRegistry.Add`.                     |
| `FLUENTLY004` | A view has no public or internal constructor whose first parameter is its view model.                                                    | Add a constructor whose first parameter is the view model; further parameters are resolved from the services. |
| `FLUENTLY005` | A view model's attribute does not match it, for example `[Dialog]` on a class that does not implement `IDialog<TResult>`.                | Use the attribute for what the class is, or implement the interface the attribute needs.                      |
| `FLUENTLY006` | A class has more than one of `[SingletonPage]`, `[TransientPage]`, `[ScopedPage]`, `[Dialog]` and `[Toast]`, so it is not registered.    | Keep one of them.                                                                                             |
| `FLUENTLY007` | An attributed class cannot be created by the container: it is abstract, static, generic, or not public or internal.                      | Make it a public or internal, non-generic class that can be instantiated, or register it yourself.            |

## How it is built

- Two incremental generators: `ViewRegistrationGenerator` for views and `ViewModelRegistrationGenerator` for view
  models, in `Generators/Views` and `Generators/ViewModels`; helpers they share are in `Generators/Common`.
- It targets `netstandard2.0`, as Roslyn requires, so it runs on the .NET 8 and .NET 10 SDKs.
- Its tests compile small projects in memory and check the generated source and the diagnostics:
  `tests/Fluently.SourceGenerator.UnitTests`.
