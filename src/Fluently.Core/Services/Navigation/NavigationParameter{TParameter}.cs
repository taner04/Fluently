namespace Fluently;

/// <summary>
///     A navigation parameter of type <typeparamref name="TParameter" /> for pages and dialogs that implement
///     <see cref="INavigatedTo{TParameter}" />.
/// </summary>
internal sealed record NavigationParameter<TParameter>(TParameter Value) : NavigationParameter
    where TParameter : notnull
{
    public override Task EnterAsync(object viewModel, CancellationToken cancellationToken) =>
        ((INavigatedTo<TParameter>)viewModel).OnNavigatedToAsync(Value, cancellationToken);
}
