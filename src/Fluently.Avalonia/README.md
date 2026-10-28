# Fluently.Avalonia

[![NuGet](https://img.shields.io/nuget/v/Fluently.Avalonia)](https://www.nuget.org/packages/Fluently.Avalonia)

Avalonia integration for [Fluently](https://github.com/taner04/Fluently): a view locator, hosts that show the current
page, open dialogs and toasts, a base class for views, and a Windows 11 style app window with a title bar and a
navigation pane. Navigation, dialogs and toasts themselves come from
[`Fluently.Core`](https://github.com/taner04/Fluently/blob/main/src/Fluently.Core/README.md), which this package brings
with it together with its source generator, so an Avalonia app only needs this package.

```bash
dotnet add package Fluently.Avalonia
```

Requires .NET 8 or later and Avalonia 12.1 or later.

Fluently has no theme of its own. Its controls are built from Avalonia's standard controls and take the look of the
theme you use, such as Avalonia's Fluent theme or [FluentAvalonia](https://github.com/amwx/FluentAvalonia); every value
they set is a default your styles override.

## Registration

Register Fluently and the services that need Avalonia, such as `IThemeService`:

```csharp
services.AddFluently().AddFluentlyAvalonia();
```

Resolve these services after Avalonia has created the application, for example in `OnFrameworkInitializationCompleted`.

## Views

Derive each view from `ViewFor<TViewModel>`, which keeps the view model in `ViewModel` and sets it as the
`DataContext`. The first constructor parameter is the view model; any further parameters are services:

```csharp
internal partial class ChatView : ViewFor<ChatViewModel>
{
    public ChatView(ChatViewModel viewModel, IAudioPlayer audio) : base(viewModel) => InitializeComponent();
}
```

```xml
<UserControl xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.ChatView"
             x:DataType="vm:ChatViewModel">
```

The source generator registers the view, so there is no registration code. A view that is not a `UserControl`
implements `IViewFor<TViewModel>` itself.

Add the view locator to your application's data templates:

```xml
<Application xmlns:fluently="https://github.com/taner04/fluently">
    <Application.DataTemplates>
        <fluently:ViewLocator />
    </Application.DataTemplates>
</Application>
```

- The view locator creates one view per view model instance and reuses it, so a singleton page keeps its view,
  including scroll position and input, when you navigate back to it. A view model shown again with a new scope, such
  as a singleton dialog opened again, gets a new view.
- A page's view gets its services from the page's scope. Views of view models Fluently did not create, for example a
  toast you create yourself, get them from the root provider of the container whose host shows them.
- Several containers in one process work side by side, for example a second window with its own services: every
  `NavigationHost`, `DialogHost` and `ToastHost` creates its views, and the views of view models nested inside them,
  from the container of the service it shows. Outside any host, the `ViewLocator` uses the most recently created
  container; set its `Provider` to use another one.

## Pages

Show the current page with a `NavigationHost`:

```xml
<fluently:NavigationHost Navigation="{Binding Navigation}">
    <fluently:NavigationHost.PageTransition>
        <CrossFade Duration="0:0:0.15" />
    </fluently:NavigationHost.PageTransition>
</fluently:NavigationHost>
```

It follows its navigation service while it is shown, also when the service navigates on another thread.

## Dialogs

Wrap your content in a `DialogHost` to show the open dialogs of an `IDialogService` above it:

```xml
<fluently:DialogHost Dialogs="{Binding Dialogs}" DismissOnBackdropClick="True">
    <fluently:NavigationHost Navigation="{Binding Navigation}" />
</fluently:DialogHost>
```

- Every dialog gets a backdrop that covers everything below it and is centered above it. The backdrop is transparent,
  and still modal, until you set `Backdrop`, for example to a translucent black. The dialog's view draws its own
  surface, such as a `Border` with a background and padding. Style the backdrop with the class `dialog-backdrop` and
  the dialog's presenter, a `ContentPresenter`, with `dialog`.
- Avalonia clips a `UserControl` to its bounds, so a shadow on a dialog's surface is cut off when the surface fills
  the view. Turn the clipping off for views shown by the hosts:

  ```xml
  <Style Selector="ContentPresenter.dialog :is(UserControl), fluently|ToastHost :is(UserControl)">
      <Setter Property="ClipToBounds" Value="False" />
  </Style>
  ```
- Only the top dialog reacts to the keyboard, and Tab stays inside it. Focus moves into a dialog when it opens and back
  when it closes.
- Escape, and clicking the backdrop when `DismissOnBackdropClick` is set, call `IDialogService.RequestDismissAsync`, so
  a dialog with a guard can refuse or ask first.

## Toasts

Wrap your content in a `ToastHost`, outside the `DialogHost` so toasts show above dialogs:

```xml
<fluently:ToastHost Toasts="{Binding Toasts}">
    <fluently:DialogHost Dialogs="{Binding Dialogs}">
        <fluently:NavigationHost Navigation="{Binding Navigation}" />
    </fluently:DialogHost>
</fluently:ToastHost>
```

- The host shows the toasts at `IToastService.Position` and follows it, also while toasts are shown. The newest toast
  is always the one nearest the edge.
- Every toast is shown with its own view; Fluently has no look of its own for toasts. Write a view for the built-in
  `Toast`, `ViewFor<Toast>`, that shows its `Title`, `Message` and `Severity` and closes it with
  `IToastService.Dismiss`; any other `IToast` gets its view the same way.
- Toasts move like Windows notifications: they slide and fade in from their side, or from their edge when centered,
  and slide and fade back out a little faster. A leaving toast frees its room right away, so the others glide into
  place while it fades out, and a toast shown again while it leaves turns around from where it is.
- Toasts do not block the content: only the toasts themselves take clicks.

## Theme

`IThemeService` sets the application's `RequestedThemeVariant`, on the UI thread, and follows it when your code sets
it directly. Its `ActualTheme` follows the application's `ActualThemeVariant`; a custom theme variant counts as the
light or dark one it inherits from.

## Window and navigation pane

Derive your main window from `FluentWindow` and give it a `TitleBar` and a `NavigationPane`, like the apps built with
[WPF UI](https://github.com/lepoco/wpfui):

```xml
<fluently:FluentWindow x:Class="MyApp.MainWindow" Title="My App" Navigation="{Binding Navigation}">
    <Grid RowDefinitions="Auto,*">
        <fluently:TitleBar Icon="{StaticResource AppIcon}" />
        <fluently:NavigationPane Grid.Row="1" IsSearchVisible="True"
                                 IsBackButtonVisible="False">
            <fluently:NavigationPane.Items>
                <fluently:NavigationPaneItem Title="Home" Icon="{StaticResource HomeIcon}" Page="{x:Type vm:HomeViewModel}" />
                <fluently:NavigationPaneItem Title="Chats" Icon="{StaticResource ChatIcon}" Page="{x:Type vm:ChatsViewModel}" />
            </fluently:NavigationPane.Items>
            <fluently:NavigationPane.FooterItems>
                <fluently:NavigationPaneItem Title="Settings" Icon="{StaticResource SettingsIcon}" Page="{x:Type vm:SettingsViewModel}" />
            </fluently:NavigationPane.FooterItems>
            <fluently:NavigationHost Navigation="{Binding Navigation}" />
        </fluently:NavigationPane>
    </Grid>
</fluently:FluentWindow>
```

```csharp
public partial class MainWindow : FluentWindow
{
    public MainWindow() => InitializeComponent();
}
```

### FluentWindow

- Extends its content into the title bar (`ExtendsContentIntoTitleBar`, `TitleBarHeight`, 48 by default).
- Its `Navigation` is the window's main navigation service. The `TitleBar` and `NavigationPane` inside it use it
  unless they set their own `Navigation`, so you bind it once; set their own for nested navigation.
- The mouse's back and forward buttons go back and forward in `Navigation`, anywhere in the window, unless a dialog
  is open (`IsMouseNavigationEnabled`, on by default). This works with mice that send their side buttons as mouse
  buttons 4 and 5; vendor tools that turn them into shortcuts or gestures, such as Logi Options+, bypass it.
- Shows Mica on Windows 11 (`IsMicaEnabled`); while Mica is shown, its background is transparent, so give the areas
  above it translucent backgrounds. Other systems keep the window's background.

### TitleBar

- Shows a back button for its `Navigation` (the window's unless set), enabled while it can go back, an `Icon`, the
  window's title (or its own `Title`) and your `Content` on the right.
- Leaves room for the system's window buttons, moves the window when dragged, and maximizes or restores it when
  double-clicked.
- Hides itself while the window shows its own title bar.
- Restyle its parts with the classes `title-bar-back`, `title-bar-icon` and `title-bar-title`.

### NavigationPane

- Navigates to an item's `Page` when the item is selected, and selects the item of the current page: after going back,
  after navigating from code, and again when a page's guard refuses to be left. A page without an item keeps the
  previous item selected, for example a details page opened from a list. `SelectedItem` is the item of the current
  page.
- `Items` are shown at the top, `FooterItems` at the bottom, and `PaneContent` between them, for content of your own
  such as a list of recent chats. Its `Content` is shown next to the pane, usually a `NavigationHost`.
- A button collapses the pane to its icons (`IsPaneOpen`, `OpenPaneLength`, `CompactPaneLength`); it opens and
  collapses in 0.3 seconds with Fluent's decelerating curve.
- Thin lines separate the top, with the buttons and the search box, from the items, your `PaneContent` and the footer
  items.
- A back button for apps without a `TitleBar` (`IsBackButtonVisible`); turn it off when the title bar has one.
- A search box (`IsSearchVisible`, `SearchPlaceholder`) suggests items by title and navigates when you press Enter, to
  the highlighted suggestion or else the first match, or when you click a suggestion. Ctrl+L, or Cmd+L on macOS, puts
  the cursor into it from anywhere in the window (`SearchGesture`, turned off with `IsSearchShortcutEnabled`). While
  the pane is collapsed, a search button takes its place: it opens the pane and puts the cursor into the box.
- An item's `Icon` is a `Geometry`, shown as a `PathIcon`, or any control, such as an icon from an icon font.
- It is built from Avalonia's `SplitView`, `ListBox`, `AutoCompleteBox`, `Button` and `Separator`. Restyle the parts
  with the classes `navigation-back`, `navigation-toggle`, `navigation-search`, `navigation-search-button`,
  `navigation-items`, `navigation-footer-items` and `navigation-separator`.

## Fluently and FluentAvalonia

[FluentAvalonia](https://github.com/amwx/FluentAvalonia) brings the Fluent design and WinUI controls to Avalonia, such
as `NavigationView`, `Frame` and `ContentDialog`. Fluently is a navigation, dialog and toast library for view models.
They work well together: take FluentAvalonia's theme for the look and Fluently's `NavigationPane` and hosts for
navigation, as the [gallery](https://github.com/taner04/Fluently/tree/main/src/Fluently.Gallery) does. With
FluentAvalonia's `NavigationView` instead, put a `NavigationHost` in its content and select its item from
`INavigationService.Navigated`.

|                      | Fluently                                                                                                  | FluentAvalonia                                                                                           |
|----------------------|-----------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| Focus                | View-model-first navigation, dialogs and toasts                                                           | Fluent theme and WinUI controls                                                                          |
| Navigate to          | A view model type; the view is found by the view locator                                                  | A page type (`Frame.Navigate(Type, object)`), or an object through an `INavigationPageFactory` you write |
| Parameters           | Typed, checked at compile time (`INavigatedTo<TParameter>`)                                               | `object`, read from the navigation event arguments                                                       |
| Leaving a page       | `ICanNavigateFrom` on the view model, which can be async and show a dialog                                | The `Navigating` event, canceled by setting `Cancel`                                                     |
| Dependency injection | Every page and dialog resolves from its own service scope, shared with its view                           | Pages are created by the frame or by your factory                                                        |
| Dialogs              | Any view model with `IDialog<TResult>`, returning a typed `DialogResult<TResult>`; stackable, with guards | `ContentDialog`, returning `Primary`, `Secondary` or `None`                                              |
| UI                   | Hosts, a window, a title bar and a navigation pane, without a look of their own                           | A full set of styled controls                                                                            |
