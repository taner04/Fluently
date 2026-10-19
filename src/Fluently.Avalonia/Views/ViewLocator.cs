using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Fluently;

/// <summary>
///     Shows view models as their views. Add it once to your application's data templates:
///     <code>
/// &lt;Application.DataTemplates&gt;
///     &lt;fluently:ViewLocator /&gt;
/// &lt;/Application.DataTemplates&gt;
/// </code>
///     Every <see cref="IViewFor{TViewModel}" /> view of your project is registered automatically. A view is created
///     once per view model instance and reused while the view model keeps its service scope, so a page keeps its view
///     when you navigate back to it.
///     The views come from the <see cref="IViewProvider" /> registered in your container, so you can change how views are
///     created by registering your own provider.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{

    /// <summary>
    ///     The provider that creates the views. Defaults to the <see cref="IViewProvider" /> of the container this locator
    ///     belongs to, or the most recently created container; set it to use a different one for this locator.
    /// </summary>
    public IViewProvider? Provider { get; set; }

    internal ViewModelScopes? Scopes { get; set; }

    private ViewModelScopes? Container => Scopes ?? ViewModelScopes.Latest;

    private IViewProvider CurrentProvider => Provider ?? ViewProviders.For(Container);

    /// <inheritdoc />
    public bool Match(object? data) => data is not null && CurrentProvider.CanProvide(data);

    /// <inheritdoc />
    public Control? Build(object? param) =>
        param is null ? null : ViewProviders.Create(Container, CurrentProvider, param);
}
