using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Fluently;

internal sealed class ViewLocator(IServiceProvider serviceProvider, ViewRegistry viewRegistry) : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _views = new();

    public bool Match(object? data) => data is not null && viewRegistry.TryGetViewType(data.GetType(), out _);

    public Control? Build(object? param) =>
        param is null ? null : _views.GetValue(param, CreateView);

    private Control CreateView(object viewModel)
    {
        if (!viewRegistry.TryGetViewType(viewModel.GetType(), out var viewType))
        {
            throw new InvalidOperationException($"No view implements IViewFor<{viewModel.GetType().Name}>.");
        }

        return (Control)ActivatorUtilities.CreateInstance(serviceProvider, viewType, viewModel);
    }
}
