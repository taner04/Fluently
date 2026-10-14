using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fluently;

internal sealed class ViewRegistry
{
    private readonly Dictionary<Type, Type> _viewTypes = [];

    public ViewRegistry(IEnumerable<Assembly> assemblies)
    {
        var views = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(viewType => viewType.GetInterfaces()
                .Where(contract => contract.IsGenericType &&
                                   contract.GetGenericTypeDefinition() == typeof(IViewFor<>))
                .Select(contract => (ViewModelType: contract.GenericTypeArguments[0], ViewType: viewType)));

        foreach (var (viewModelType, viewType) in views)
        {
            if (!_viewTypes.TryAdd(viewModelType, viewType))
            {
                throw new InvalidOperationException(
                    $"{viewModelType.Name} has more than one view: {_viewTypes[viewModelType].Name} and {viewType.Name}.");
            }
        }
    }

    public bool TryGetViewType(Type viewModelType, [NotNullWhen(true)] out Type? viewType) =>
        _viewTypes.TryGetValue(viewModelType, out viewType);
}
