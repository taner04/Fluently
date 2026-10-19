namespace Fluently;

/// <summary>
///     Stands in when no service provider is known for a view model, and explains the problem as soon as
///     a view actually asks for a service.
/// </summary>
internal sealed class MissingServiceProvider(object viewModel) : IServiceProvider
{
    public object GetService(Type serviceType) =>
        throw new InvalidOperationException(
            $"The view of {viewModel.GetType().Name} needs {serviceType.Name}, but no service provider is " +
            "available. Call AddFluently() and resolve INavigationService before views with services are shown.");
}
