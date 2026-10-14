using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Fluently;

/// <summary>
/// Shows view models as their views. Add it once to your application's data templates:
/// <code>
/// &lt;Application.DataTemplates&gt;
///     &lt;fluently:ViewLocator /&gt;
/// &lt;/Application.DataTemplates&gt;
/// </code>
/// Every <see cref="IViewFor{TViewModel}"/> view of your project is registered automatically. A view is created
/// once per view model instance and reused, so a page keeps its view when you navigate back to it.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _views = new();

    /// <inheritdoc />
    public bool Match(object? data) => data is not null && ViewRegistry.Contains(data.GetType());

    /// <inheritdoc />
    public Control? Build(object? param) =>
        param is null ? null : _views.GetValue(param, CreateView);

    private static Control CreateView(object viewModel)
    {
        if (!ViewRegistry.TryCreateView(viewModel, out var view))
        {
            throw new InvalidOperationException($"No view is registered for {viewModel.GetType().Name}.");
        }

        return view as Control
               ?? throw new InvalidOperationException(
                   $"The view {view.GetType().Name} of {viewModel.GetType().Name} is not an Avalonia control.");
    }
}
