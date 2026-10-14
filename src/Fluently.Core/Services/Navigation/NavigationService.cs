using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed partial class NavigationService : INavigationService
{
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private NavigationEntry? _current;
    private bool _disposed;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _logger = logger;

        // The container creates this service from its root provider, which is where views of view models
        // that Fluently did not create get their services from.
        ViewModelServices.SetRoot(serviceProvider);
    }

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public INavigableViewModel? CurrentPage => _current?.Page;

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public Task<bool> NavigateToAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TPage>(
        CancellationToken cancellationToken = default)
        where TPage : INavigableViewModel =>
        NavigateToAsync(typeof(TPage), cancellationToken);

    public Task<bool> NavigateToAsync<TPage, TParameter>(
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TPage : INavigatedTo<TParameter>
        where TParameter : notnull
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return NavigateCoreAsync(typeof(TPage), new NavigationParameter<TParameter>(parameter), cancellationToken);
    }

    public Task<bool> NavigateToAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type viewModelType,
        CancellationToken cancellationToken = default)
    {
        viewModelType.ThrowIfNotAssignableTo<INavigableViewModel>();

        if (RequiresParameter(viewModelType))
        {
            LogParameterRequired(viewModelType);
            return Task.FromResult(false);
        }

        return NavigateCoreAsync(viewModelType, null, cancellationToken);
    }

    public Task<bool> GoBackAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_backStack, _forwardStack, cancellationToken);

    public Task<bool> GoForwardAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_forwardStack, _backStack, cancellationToken);

    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        HistoryState before;
        await EnterTransitionAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            before = CaptureState();
            await ReleaseAllAsync(_backStack);
            await ReleaseAllAsync(_forwardStack);
        }
        finally
        {
            _transitionLock.Release();
        }

        RaisePropertyChanges(before);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var entry in TakeAllEntries())
        {
            await ReleaseAsync(entry);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var entry in TakeAllEntries())
        {
            Release(entry);
        }
    }

    private static bool RequiresParameter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type viewModelType) =>
        !typeof(INavigatedTo).IsAssignableFrom(viewModelType)
        && viewModelType.GetInterfaces().Any(static contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(INavigatedTo<>));
}
