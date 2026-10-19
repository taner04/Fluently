namespace Fluently;

/// <summary>
///     Follows the service of a host while the host is in the visual tree: subscribes when it is attached, switches when
///     the host gets another service, and unsubscribes when it is detached.
/// </summary>
internal sealed class ServiceFollower<TService>(
    Action<TService> subscribe,
    Action<TService> unsubscribe,
    Action<TService?> followed)
    where TService : class
{
    private bool _isAttached;

    public TService? Current { get; private set; }

    public void Attach(TService? service)
    {
        _isAttached = true;
        Follow(service);
    }

    public void Detach()
    {
        Follow(null);
        _isAttached = false;
    }

    public void Change(TService? service)
    {
        if (_isAttached)
        {
            Follow(service);
        }
    }

    private void Follow(TService? service)
    {
        if (Current is not null)
        {
            unsubscribe(Current);
        }

        Current = service;
        if (service is not null)
        {
            subscribe(service);
        }

        followed(service);
    }
}
