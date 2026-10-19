using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

internal sealed partial class NavigationService : INavigationService, IViewSource
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
        Scopes = serviceProvider.GetService<ViewModelScopes>() ?? new ViewModelScopes(serviceProvider);
    }

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public INavigableViewModel? CurrentPage => _current?.Page;

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public Task<bool> NavigateToAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type viewModelType,
        CancellationToken cancellationToken = default)
    {
        viewModelType.ThrowIfNotAssignableTo<INavigableViewModel>();

        if (NavigationParameter.IsRequiredBy(viewModelType))
        {
            LogParameterRequired(viewModelType);
            return Task.FromResult(false);
        }

        return NavigateCoreAsync(viewModelType, null, cancellationToken);
    }

    public Task<bool> NavigateToAsync<TParameter>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type viewModelType,
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TParameter : notnull
    {
        viewModelType.ThrowIfNotAssignableTo<INavigableViewModel>();
        ArgumentNullException.ThrowIfNull(parameter);

        if (!typeof(INavigatedTo<TParameter>).IsAssignableFrom(viewModelType))
        {
            LogParameterNotAccepted(viewModelType, typeof(TParameter));
            return Task.FromResult(false);
        }

        return NavigateCoreAsync(viewModelType, new NavigationParameter<TParameter>(parameter), cancellationToken);
    }

    public Task<bool> GoBackAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_backStack, _forwardStack, cancellationToken);

    public Task<bool> GoForwardAsync(CancellationToken cancellationToken = default) =>
        NavigateHistoryAsync(_forwardStack, _backStack, cancellationToken);

    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        HistoryState before;
        await _transitionLock.EnterAsync(cancellationToken);
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

    public ViewModelScopes Scopes { get; }
}
