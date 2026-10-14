namespace Fluently;

/// <summary>
/// A navigation parameter of type <typeparamref name="TParameter"/> for pages that implement
/// <see cref="INavigatedTo{TParameter}"/>.
/// </summary>
internal sealed record NavigationParameter<TParameter>(TParameter Value) : NavigationParameter
    where TParameter : notnull
{
    public override Task EnterAsync(INavigableViewModel page, CancellationToken cancellationToken) =>
        ((INavigatedTo<TParameter>)page).OnNavigatedToAsync(Value, cancellationToken);
}
