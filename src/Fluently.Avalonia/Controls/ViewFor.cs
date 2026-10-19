using Avalonia.Controls;

namespace Fluently;

/// <summary>
///     A <see cref="UserControl" /> that is the view of <typeparamref name="TViewModel" />.
///     It keeps the view model in <see cref="ViewModel" /> and uses it as its <c>DataContext</c>.
///     Derive from it instead of implementing <see cref="IViewFor{TViewModel}" /> yourself:
///     <code>
/// internal partial class ChatView : ViewFor&lt;ChatViewModel&gt;
/// {
///     public ChatView(ChatViewModel viewModel) : base(viewModel) => InitializeComponent();
/// }
/// </code>
/// </summary>
/// <typeparam name="TViewModel">The type of the view model this view shows.</typeparam>
public abstract class ViewFor<TViewModel> : UserControl, IViewFor<TViewModel>
{
    /// <summary>
    ///     Creates the view of <paramref name="viewModel" />.
    /// </summary>
    /// <param name="viewModel">The view model this view shows.</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel" /> is null.</exception>
    protected ViewFor(TViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        ViewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>
    ///     The view model this view shows.
    /// </summary>
    public TViewModel ViewModel { get; }
}
