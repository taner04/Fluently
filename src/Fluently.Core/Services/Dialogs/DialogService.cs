using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

internal sealed partial class DialogService(
    IServiceScopeFactory scopeFactory,
    ViewModelScopes scopes,
    ILogger<DialogService> logger)
    : IDialogService, IDisposable, IAsyncDisposable, IViewSource
{
    private static readonly PropertyChangedEventArgs DialogsChanged = new(nameof(Dialogs));
    private static readonly PropertyChangedEventArgs CurrentDialogChanged = new(nameof(CurrentDialog));

    private readonly List<DialogEntry> _entries = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger _logger = logger;
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        foreach (var entry in CloseAllEntries())
        {
            await ReleaseAsync(entry);
            entry.Dismiss();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<IDialog> Dialogs { get; private set; } = [];

    public IDialog? CurrentDialog => Dialogs.Count > 0 ? Dialogs[^1] : null;

    public Task<DialogResult<TResult>> ShowAsync<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type dialogType,
        CancellationToken cancellationToken = default)
    {
        dialogType.ThrowIfNotAssignableTo<IDialog<TResult>>();

        if (NavigationParameter.IsRequiredBy(dialogType))
        {
            LogParameterRequired(dialogType);
            return Task.FromResult(DialogResult<TResult>.Dismissed);
        }

        return ShowCoreAsync<TResult>(dialogType, null, cancellationToken);
    }

    public Task<DialogResult<TResult>> ShowAsync<TResult, TParameter>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type dialogType,
        TParameter parameter,
        CancellationToken cancellationToken = default)
        where TParameter : notnull
    {
        dialogType.ThrowIfNotAssignableTo<IDialog<TResult>>();
        ArgumentNullException.ThrowIfNull(parameter);

        if (!typeof(INavigatedTo<TParameter>).IsAssignableFrom(dialogType))
        {
            LogParameterNotAccepted(dialogType, typeof(TParameter));
            return Task.FromResult(DialogResult<TResult>.Dismissed);
        }

        return ShowCoreAsync<TResult>(dialogType, new NavigationParameter<TParameter>(parameter), cancellationToken);
    }

    public async Task<bool> CloseAsync<TResult>(IDialog<TResult> dialog, TResult result)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (await RemoveAsync(open => ReferenceEquals(open.Dialog, dialog), typeof(TResult))
            is not DialogEntry<TResult> entry)
        {
            return false;
        }

        entry.Complete(result);
        return true;
    }

    public async Task<bool> DismissAsync(IDialog dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (await RemoveAsync(open => ReferenceEquals(open.Dialog, dialog), null) is not { } entry)
        {
            return false;
        }

        entry.Dismiss();
        return true;
    }

    public async Task<bool> RequestDismissAsync(IDialog dialog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        DialogEntry? entry;
        await _lock.EnterAsync(cancellationToken);
        try
        {
            entry = _entries.Find(open => open.IsOpen && ReferenceEquals(open.Dialog, dialog));
            if (entry is null || entry.IsDismissing)
            {
                return false;
            }

            entry.IsDismissing = true;
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            if (!await CanDismissAsync(entry, cancellationToken))
            {
                return false;
            }

            if (await RemoveAsync(open => ReferenceEquals(open, entry), null) is null)
            {
                return false;
            }

            entry.Dismiss();
            return true;
        }
        finally
        {
            entry.IsDismissing = false;
        }
    }

    public void Dispose()
    {
        foreach (var entry in CloseAllEntries())
        {
            Scopes.Release(entry.Dialog, entry.Scope, exception => LogDisposeFailed(entry.Dialog.GetType(), exception));
            entry.Dismiss();
        }
    }

    public ViewModelScopes Scopes { get; } = scopes;

    private async Task<DialogResult<TResult>> ShowCoreAsync<TResult>(
        Type viewModelType,
        NavigationParameter? parameter,
        CancellationToken cancellationToken)
    {
        if (await OpenAsync<TResult>(viewModelType, parameter, cancellationToken) is not { } entry)
        {
            return DialogResult<TResult>.Dismissed;
        }

        try
        {
            return await entry.Result.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (await RemoveAsync(open => ReferenceEquals(open, entry), null) is null)
            {
                return await entry.Result;
            }

            entry.Dismiss();
            throw;
        }
    }

    private async Task<DialogEntry<TResult>?> OpenAsync<TResult>(
        Type viewModelType,
        NavigationParameter? parameter,
        CancellationToken cancellationToken)
    {
        DialogEntry<TResult> entry;
        await _lock.EnterAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (CreateEntry<TResult>(viewModelType, parameter) is not { } created)
            {
                return null;
            }

            entry = created;
            _entries.Add(entry);
        }
        finally
        {
            _lock.Release();
        }

        if (!await EnterAsync(entry, cancellationToken))
        {
            return null;
        }

        IDialog? before;
        bool isShown;
        await _lock.WaitAsync();
        try
        {
            before = CurrentDialog;

            isShown = !_disposed && _entries.Contains(entry);
            if (isShown)
            {
                entry.IsOpen = true;
                Dialogs = Snapshot();
            }
            else
            {
                _entries.Remove(entry);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (!isShown)
        {
            await LeaveAsync(entry);
            await ReleaseAsync(entry);
            entry.Dismiss();
            return entry;
        }

        RaisePropertyChanges(before);
        return entry;
    }

    private DialogEntry<TResult>? CreateEntry<TResult>(Type viewModelType, NavigationParameter? parameter)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var dialog = (IDialog)scope.ServiceProvider.GetRequiredService(viewModelType);
            if (_entries.Any(open => ReferenceEquals(open.Dialog, dialog)))
            {
                LogAlreadyOpen(viewModelType);
                scope.Dispose();
                return null;
            }

            if (dialog is IHostedDialog hosted)
            {
                hosted.Dialogs = this;
            }

            Scopes.Track(dialog, scope.ServiceProvider);
            return new DialogEntry<TResult>(dialog, parameter, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private async Task<bool> EnterAsync(DialogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await entry.EnterAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AbandonAsync(entry);
            throw;
        }
        catch (Exception exception)
        {
            LogOpenFailed(entry.Dialog.GetType(), exception);
            await AbandonAsync(entry);
            return false;
        }
    }

    private async Task AbandonAsync(DialogEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            _entries.Remove(entry);
        }
        finally
        {
            _lock.Release();
        }

        await LeaveAsync(entry);
        await ReleaseAsync(entry);
        entry.Dismiss();
    }

    private async Task<DialogEntry?> RemoveAsync(Predicate<DialogEntry> match, Type? resultType)
    {
        DialogEntry? entry;
        IDialog? before;
        await _lock.WaitAsync();
        try
        {
            before = CurrentDialog;
            entry = _entries.Find(match);
            if (entry is null)
            {
                return null;
            }

            if (resultType is not null && entry.ResultType != resultType)
            {
                throw new InvalidOperationException(
                    $"{entry.Dialog.GetType().Name} was shown for a result of type {entry.ResultType.Name}, " +
                    $"not {resultType.Name}.");
            }

            _entries.Remove(entry);
            if (entry.IsOpen)
            {
                Dialogs = Snapshot();
            }
        }
        finally
        {
            _lock.Release();
        }

        if (entry.IsOpen)
        {
            RaisePropertyChanges(before);
            await LeaveAsync(entry);
            await ReleaseAsync(entry);
        }

        return entry;
    }

    private Task<bool> CanDismissAsync(DialogEntry entry, CancellationToken cancellationToken) =>
        Lifecycle.CanLeaveAsync(
            entry.Dialog,
            exception => LogDismissGuardFailed(entry.Dialog.GetType(), exception),
            cancellationToken);

    private async Task LeaveAsync(DialogEntry entry)
    {
        try
        {
            await entry.LeaveAsync();
        }
        catch (Exception exception)
        {
            LogCloseFailed(entry.Dialog.GetType(), exception);
        }
    }

    private Task ReleaseAsync(DialogEntry entry) =>
        Scopes.ReleaseAsync(entry.Dialog, entry.Scope,
            exception => LogDisposeFailed(entry.Dialog.GetType(), exception));

    private List<DialogEntry> CloseAllEntries()
    {
        var before = CurrentDialog;
        var entries = TakeAllEntries();
        if (before is not null)
        {
            RaisePropertyChanges(before);
        }

        return entries;
    }

    private List<DialogEntry> TakeAllEntries()
    {
        _lock.Wait();
        try
        {
            if (_disposed)
            {
                return [];
            }

            _disposed = true;
            var entries = _entries.Where(static entry => entry.IsOpen).ToList();
            _entries.Clear();
            Dialogs = [];
            return entries;
        }
        finally
        {
            _lock.Release();
        }
    }

    private IReadOnlyList<IDialog> Snapshot() =>
        [.. _entries.Where(static entry => entry.IsOpen).Select(static entry => entry.Dialog)];

    private void RaisePropertyChanges(IDialog? before)
    {
        PropertyChanged?.Invoke(this, DialogsChanged);

        if (!ReferenceEquals(before, CurrentDialog))
        {
            PropertyChanged?.Invoke(this, CurrentDialogChanged);
        }
    }
}
