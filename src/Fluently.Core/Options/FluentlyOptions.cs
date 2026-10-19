namespace Fluently;

/// <summary>
///     Configures Fluently in <c>services.AddFluently(options => …)</c>.
/// </summary>
public sealed class FluentlyOptions
{
    /// <summary>
    ///     How toasts behave.
    /// </summary>
    public ToastOptions Toasts { get; } = new();
}
