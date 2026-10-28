namespace Fluently.Gallery.Snippets;

public static class CodeSnippets
{
    public const string GettingStarted =
        """
        // App.axaml.cs
        var services = new ServiceCollection()
            .AddLogging()
            .AddFluently(options =>
            {
                options.Toasts.Position = ToastPosition.BottomRight;
                options.Toasts.Duration = TimeSpan.FromSeconds(6);
            })
            .AddViewModels()
            .BuildServiceProvider();

        var navigation = services.GetRequiredService<INavigationService>();
        await navigation.NavigateToAsync<HomeViewModel>();
        """;

    public const string GettingStartedXaml =
        """
        <!-- App.axaml -->
        <Application.DataTemplates>
            <fluently:ViewLocator />
        </Application.DataTemplates>

        <!-- MainWindow.axaml -->
        <fluently:NavigationHost Navigation="{Binding Navigation}" />
        """;

    public const string Registration =
        """
        [SingletonPage]
        public sealed partial class HomeViewModel(INavigationService navigation) : INavigableViewModel;

        [TransientPage]
        public sealed partial class DetailsViewModel(ItemCatalog catalog) : ObservableObject, INavigatedTo<int>;

        [Dialog]
        [ObservableObject]
        public sealed partial class ConfirmDialogViewModel : DialogViewModel<bool>;
        """;

    public const string ViewFor =
        """
        internal partial class HomeView : ViewFor<HomeViewModel>
        {
            public HomeView(HomeViewModel viewModel) : base(viewModel) => InitializeComponent();
        }
        """;

    public const string TypedNavigation =
        """
        // ItemsViewModel.cs
        await navigation.NavigateToAsync<DetailsViewModel, int>(item.Id);

        // DetailsViewModel.cs
        public sealed partial class DetailsViewModel(ItemCatalog catalog) : ObservableObject, INavigatedTo<int>
        {
            [ObservableProperty] public partial SampleItem? Item { get; private set; }

            public Task OnNavigatedToAsync(int parameter, CancellationToken cancellationToken)
            {
                Item = catalog.Find(parameter);
                return Task.CompletedTask;
            }
        }

        services.AddTransient<ItemsViewModel>().AddTransient<DetailsViewModel>();
        """;

    public const string DeleteWithDialog =
        """
        // ItemsViewModel.cs
        [RelayCommand]
        private async Task DeleteAsync(SampleItem item)
        {
            var request = new ConfirmRequest("Delete item?", $"\"{item.Name}\" will be removed from the list.", "Delete");
            var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request);
            if (result.Value)
            {
                var index = catalog.Items.IndexOf(item);
                catalog.Items.Remove(item);
                toasts.Show(new UndoToastViewModel(catalog, item, index));
            }
        }
        """;

    public const string Guard =
        """
        public sealed partial class EditorViewModel(IDialogService dialogs) : ObservableObject, ICanNavigateFrom
        {
            public async Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
            {
                if (!HasChanges)
                {
                    return true;
                }

                var request = new ConfirmRequest("Discard changes?", "Your edits have not been saved.", "Discard");
                var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(
                    request, cancellationToken);
                return result.Value;
            }
        }
        """;

    public const string ConfirmDialog =
        """
        // ConfirmDialogViewModel.cs
        [ObservableObject]
        public sealed partial class ConfirmDialogViewModel : DialogViewModel<bool>, INavigatedTo<ConfirmRequest>
        {
            [ObservableProperty] public partial ConfirmRequest? Request { get; private set; }

            public Task OnNavigatedToAsync(ConfirmRequest parameter, CancellationToken cancellationToken)
            {
                Request = parameter;
                return Task.CompletedTask;
            }

            [RelayCommand]
            private Task ConfirmAsync() => CloseAsync(true);

            [RelayCommand]
            private Task CancelAsync() => CloseAsync(false);
        }

        // DialogsViewModel.cs
        var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(request);
        LastResult = result.IsCompleted ? $"Confirm returned {result.Value}." : "Confirm was dismissed.";
        """;

    public const string InputDialog =
        """
        // InputDialogViewModel.cs
        [ObservableObject]
        public sealed partial class InputDialogViewModel(IDialogService dialogs)
            : DialogViewModel<string>, INavigatedTo<string>, ICanNavigateFrom
        {
            public async Task<bool> CanNavigateFromAsync(CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(Text))
                {
                    return true;
                }

                var request = new ConfirmRequest("Discard input?", "What you typed will be lost.", "Discard");
                var result = await dialogs.ShowAsync<ConfirmDialogViewModel, bool, ConfirmRequest>(
                    request, cancellationToken);
                return result.Value;
            }

            [RelayCommand(CanExecute = nameof(CanAccept))]
            private Task AcceptAsync() => CloseAsync(Text.Trim());

            [RelayCommand]
            private Task CancelAsync() => DismissAsync();
        }
        """;

    public const string Toasts =
        """
        toasts.ShowInformation("Sync started", "Your files are being synchronized.");
        toasts.ShowSuccess("Saved", "Your changes were saved.");
        toasts.ShowWarning("Low disk space", "Less than 1 GB is left on this drive.");
        toasts.ShowError("Upload failed", "The server could not be reached.");

        toasts.Show("Saved", "Your changes were saved.", ToastSeverity.Success);

        toasts.ShowWarning("Offline", "Reconnecting…", Timeout.InfiniteTimeSpan);

        toasts.Position = ToastPosition.BottomRight;

        var toast = toasts.Show("Uploading", "report.pdf");
        toasts.Dismiss(toast);
        """;

    public const string UndoToast =
        """
        // UndoToastViewModel.cs
        public sealed partial class UndoToastViewModel(ItemCatalog catalog, SampleItem item, int index)
            : ToastViewModel
        {
            public string Message { get; } = $"\"{item.Name}\" was deleted.";

            [RelayCommand]
            private void Undo()
            {
                if (!catalog.Items.Contains(item))
                {
                    catalog.Items.Insert(Math.Min(index, catalog.Items.Count), item);
                }

                Dismiss();
            }
        }

        // ItemsViewModel.cs
        toasts.Show(new UndoToastViewModel(catalog, item, index));
        """;

    public const string ToastHost =
        """
        <!-- MainWindow.axaml -->
        <fluently:ToastHost Toasts="{Binding Toasts}">
            <fluently:DialogHost Dialogs="{Binding Dialogs}">
                <fluently:NavigationHost Navigation="{Binding Navigation}" />
            </fluently:DialogHost>
        </fluently:ToastHost>

        <!-- ToastView.axaml: the look of a Toast, with a code-behind deriving from ViewFor<Toast> -->
        <UserControl xmlns:fluently="using:Fluently"
                     x:Class="MyApp.Views.ToastView"
                     x:DataType="fluently:Toast">
            <Border Classes="toast">
                <StackPanel Spacing="6">
                    <TextBlock Text="{Binding Title}" FontWeight="SemiBold" />
                    <TextBlock Text="{Binding Message}" TextWrapping="Wrap" />
                </StackPanel>
            </Border>
        </UserControl>
        """;

    public const string DialogHost =
        """
        <!-- MainWindow.axaml -->
        <fluently:DialogHost Dialogs="{Binding Dialogs}" DismissOnBackdropClick="True"
                             Backdrop="{DynamicResource SmokeFillColorDefaultBrush}">
            <fluently:NavigationHost Navigation="{Binding Navigation}" />
        </fluently:DialogHost>
        """;
}
