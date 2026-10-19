namespace Fluently.Core.UnitTests.TestDoubles.Toasts;

internal sealed class ScopedToast(Greeting greeting) : IToast, IDisposable
{
    internal Greeting Greeting { get; } = greeting;

    internal bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
