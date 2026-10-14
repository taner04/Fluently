namespace Fluently;

/// <summary>
/// A navigation parameter that knows its own type, so the page can be entered with
/// <see cref="INavigatedTo{TParameter}"/> without casts or reflection.
/// Records compare by value, so navigating to the current page with an equal parameter is detected.
/// </summary>
internal abstract record NavigationParameter
{
    public abstract Task EnterAsync(INavigableViewModel page, CancellationToken cancellationToken);
}
