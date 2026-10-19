namespace Fluently;

/// <summary>
///     Creates independent navigation services, for example for nested navigation inside a page.
/// </summary>
public interface INavigationServiceFactory
{
    /// <summary>
    ///     Creates a navigation service with its own history.
    ///     The caller owns it and disposes it when the navigation is no longer needed.
    /// </summary>
    /// <returns>A new navigation service without pages.</returns>
    INavigationService Create();
}
