using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

/// <summary>
/// The views the view locator can create. The generated <c>AddFluentlyViews()</c> fills it with every
/// <see cref="IViewFor{TViewModel}"/> view of your assembly; use <c>AddFluentlyAvalonia</c> to fill it by hand.
/// </summary>
public sealed class ViewRegistry
{
    private readonly Dictionary<Type, Func<IServiceProvider, object, Control>> _factories = [];

    internal ViewRegistry()
    {
    }

    /// <summary>
    /// Registers <typeparamref name="TView"/> as the view of <typeparamref name="TViewModel"/>.
    /// The view is created through the service provider with the view model as a constructor argument.
    /// </summary>
    /// <exception cref="InvalidOperationException"><typeparamref name="TViewModel"/> already has a view.</exception>
    public ViewRegistry Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView, TViewModel>()
        where TView : Control, IViewFor<TViewModel>
        where TViewModel : class
    {
        if (!_factories.TryAdd(
                typeof(TViewModel),
                static (serviceProvider, viewModel) => ActivatorUtilities.CreateInstance<TView>(serviceProvider, viewModel)))
        {
            throw new InvalidOperationException($"{typeof(TViewModel).Name} already has a view.");
        }

        return this;
    }

    internal bool Contains(Type viewModelType) => _factories.ContainsKey(viewModelType);

    internal bool TryCreate(
        IServiceProvider serviceProvider,
        object viewModel,
        [NotNullWhen(true)] out Control? view)
    {
        if (_factories.TryGetValue(viewModel.GetType(), out var factory))
        {
            view = factory(serviceProvider, viewModel);
            return true;
        }

        view = null;
        return false;
    }
}
