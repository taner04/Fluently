using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Fluently;

/// <summary>
///     One open dialog: a backdrop over everything below it, and the dialog centered above.
///     Tab cycles inside the layer, so the keyboard cannot reach the content below.
/// </summary>
internal sealed class DialogLayer : Panel
{
    public DialogLayer(IDialog dialog, object? content, IInputElement? previousFocus)
    {
        Dialog = dialog;
        PreviousFocus = previousFocus;
        Focusable = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

        Backdrop.Classes.Add("dialog-backdrop");
        Presenter = new ContentControl
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Presenter.Classes.Add("dialog");

        Children.Add(Backdrop);
        Children.Add(Presenter);
    }

    public IDialog Dialog { get; }

    public IInputElement? PreviousFocus { get; }

    public Border Backdrop { get; } = new();

    public ContentControl Presenter { get; }
}
