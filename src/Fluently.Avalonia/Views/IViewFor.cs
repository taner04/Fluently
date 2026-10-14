namespace Fluently;

/// <summary>
/// Marks a view as the view of <typeparamref name="TViewModel"/>.
/// The view is created with its view model as a constructor argument.
/// </summary>
public interface IViewFor<out TViewModel>
{
    /// <summary>
    /// The view model this view shows.
    /// </summary>
    TViewModel ViewModel { get; }
}
