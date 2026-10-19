using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Fluently;

/// <summary>
///     Knows which view belongs to which view model. The source generator in Fluently.Core registers every
///     <see cref="IViewFor{TViewModel}" /> view of your project here when the assembly loads, so you normally
///     never call it yourself; a UI package such as Fluently.Avalonia shows the views.
/// </summary>
public static class ViewRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, IViewFor<object>>> Factories =
        new();

    /// <summary>
    ///     Registers how the view of <typeparamref name="TViewModel" /> is created, for example for views the
    ///     generator cannot see. <paramref name="createView" /> gets the service provider of the scope the view model
    ///     was created from, its page, dialog or toast scope, or the container's root provider.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model the view shows.</typeparam>
    /// <param name="createView">Creates the view from the service provider and the view model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="createView" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><typeparamref name="TViewModel" /> already has a view.</exception>
    public static void Add<TViewModel>(Func<IServiceProvider, TViewModel, IViewFor<TViewModel>> createView)
        where TViewModel : class
    {
        ArgumentNullException.ThrowIfNull(createView);

        if (!Factories.TryAdd(typeof(TViewModel), (services, viewModel) => createView(services, (TViewModel)viewModel)))
        {
            throw new InvalidOperationException($"{typeof(TViewModel).Name} already has a view.");
        }
    }

    internal static bool Contains(Type viewModelType) => Factories.ContainsKey(viewModelType);

    internal static bool TryCreateView(
        object viewModel,
        IServiceProvider services,
        [NotNullWhen(true)] out IViewFor<object>? view)
    {
        if (!Factories.TryGetValue(viewModel.GetType(), out var factory))
        {
            view = null;
            return false;
        }

        view = factory(services, viewModel);
        return true;
    }
}
