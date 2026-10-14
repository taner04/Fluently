using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Fluently;

internal sealed class ViewLocator(IServiceProvider serviceProvider, ViewRegistry viewRegistry) : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _views = new();

    public bool Match(object? data) => data is not null && viewRegistry.Contains(data.GetType());

    public Control? Build(object? param) =>
        param is null ? null : _views.GetValue(param, CreateView);

    private Control CreateView(object viewModel) =>
        viewRegistry.TryCreate(serviceProvider, viewModel, out var view)
            ? view
            : throw new InvalidOperationException($"No view is registered for {viewModel.GetType().Name}.");
}
