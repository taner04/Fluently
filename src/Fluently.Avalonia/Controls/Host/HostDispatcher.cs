using Avalonia.Threading;

namespace Fluently;

/// <summary>
///     Runs a host's update on the UI thread: right away when called there, otherwise posted to it, because services
///     raise their changes on whichever thread changed them.
/// </summary>
internal static class HostDispatcher
{
    public static void Run(Action update)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            update();
        }
        else
        {
            Dispatcher.UIThread.Post(update);
        }
    }
}
