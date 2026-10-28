using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Fluently;

internal sealed partial class ToastService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ToastOptions options,
    ViewModelScopes scopes,
    ILogger<ToastService> logger) : IToastService, IDisposable, IViewSource
{
    private static readonly PropertyChangedEventArgs ToastsChanged = new(nameof(Toasts));
    private static readonly PropertyChangedEventArgs PositionChanged = new(nameof(Position));

    private readonly List<ActiveToast> _active = [];
#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly ILogger _logger = logger;
    private bool _disposed;
    private ToastPosition _position = options.Position;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public IReadOnlyList<IToast> Toasts { get; private set; } = [];

    /// <inheritdoc />
    public ViewModelScopes Scopes { get; } = scopes;

    /// <inheritdoc />
    public ToastPosition Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            _position = value;
            PropertyChanged?.Invoke(this, PositionChanged);
        }
    }

    /// <inheritdoc />
    public void Show(IToast toast, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(toast);
        ShowCore(toast, Lifetime(duration), null);
    }

    /// <inheritdoc />
    public IToast Show(Type toastType, TimeSpan? duration = null)
    {
        toastType.ThrowIfNotAssignableTo<IToast>();
        var lifetime = Lifetime(duration);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var scope = scopeFactory.CreateAsyncScope();
        IToast toast;
        try
        {
            toast = (IToast)scope.ServiceProvider.GetRequiredService(toastType);
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        ShowCore(toast, lifetime, scope);
        return toast;
    }

    /// <inheritdoc />
    public bool Dismiss(IToast toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        return Remove(active => ReferenceEquals(active.Toast, toast));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<ActiveToast> entries;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            entries = [.. _active];
            _active.Clear();
            Toasts = [];
        }

        if (entries.Count > 0)
        {
            PropertyChanged?.Invoke(this, ToastsChanged);
        }

        foreach (var entry in entries)
        {
            StopTimer(entry);
            if (entry.Scope is { } scope)
            {
                Scopes.Release(entry.Toast, scope, exception => LogDisposeFailed(entry.Toast.GetType(), exception));
            }
        }
    }

    private TimeSpan Lifetime(TimeSpan? duration)
    {
        var lifetime = duration ?? options.Duration;
        if (!ToastOptions.IsValidDuration(lifetime))
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "A toast needs a positive duration of at most 49 days, or Timeout.InfiniteTimeSpan.");
        }

        return lifetime;
    }

    private void ShowCore(IToast toast, TimeSpan lifetime, AsyncServiceScope? scope)
    {
        if (toast is IHostedToast hosted)
        {
            hosted.Toasts = this;
        }

        var removed = new List<ActiveToast>();
        ActiveToast? restarted = null;
        ActiveToast entry;
        CancellationToken expiry;
        lock (_gate)
        {
            if (_disposed)
            {
                scope?.Dispose();
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            if (_active.Find(active => ReferenceEquals(active.Toast, toast)) is { } existing)
            {
                _active.Remove(existing);
                restarted = existing;
            }
            else if (_active.Count >= options.MaxCount)
            {
                removed.Add(_active[^1]);
                _active.RemoveAt(_active.Count - 1);
            }

            entry = new ActiveToast(toast, new CancellationTokenSource(), restarted?.Scope ?? scope);

            expiry = entry.Expiry.Token;
            _active.Insert(0, entry);
            Toasts = Snapshot();
        }

        if (restarted is not null)
        {
            StopTimer(restarted);
        }

        if (restarted?.Scope is not null && scope is { } redundant)
        {
            ReleaseScope(toast, redundant);
        }
        else if (scope is { } created)
        {
            Scopes.Track(toast, created.ServiceProvider);
        }

        Release(removed);
        PropertyChanged?.Invoke(this, ToastsChanged);

        if (lifetime != Timeout.InfiniteTimeSpan)
        {
            _ = DismissLaterAsync(entry, lifetime, expiry);
        }
    }

    private async Task DismissLaterAsync(ActiveToast entry, TimeSpan duration, CancellationToken expiry)
    {
        try
        {
            await Task.Delay(duration, timeProvider, expiry);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Remove(active => ReferenceEquals(active, entry));
    }

    private bool Remove(Predicate<ActiveToast> match)
    {
        ActiveToast? entry;
        lock (_gate)
        {
            entry = _active.Find(match);
            if (entry is null)
            {
                return false;
            }

            _active.Remove(entry);
            Toasts = Snapshot();
        }

        Release([entry]);
        PropertyChanged?.Invoke(this, ToastsChanged);
        return true;
    }

    private void Release(IEnumerable<ActiveToast> entries)
    {
        foreach (var entry in entries)
        {
            StopTimer(entry);
            if (entry.Scope is { } scope)
            {
                ReleaseScope(entry.Toast, scope);
            }
        }
    }

    private static void StopTimer(ActiveToast entry)
    {
        entry.Expiry.Cancel();
        entry.Expiry.Dispose();
    }

    private void ReleaseScope(IToast toast, AsyncServiceScope scope) =>
        _ = Scopes.ReleaseAsync(toast, scope, exception => LogDisposeFailed(toast.GetType(), exception));

    private IReadOnlyList<IToast> Snapshot() => [.. _active.Select(static active => active.Toast)];
}
