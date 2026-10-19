using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Fluently.Gallery.Controls;

public partial class CodeBlock : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<CodeBlock, string?>(nameof(Label));

    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<CodeBlock, string?>(nameof(Code));

    public static readonly StyledProperty<CodeLanguage> LanguageProperty =
        AvaloniaProperty.Register<CodeBlock, CodeLanguage>(nameof(Language));

    private static readonly TimeSpan CopiedDuration = TimeSpan.FromSeconds(1.5);

    public CodeBlock()
    {
        InitializeComponent();
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public CodeLanguage Language
    {
        get => GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (Code is null || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        await clipboard.SetTextAsync(Code);
        ShowCopied(true);
        await Task.Delay(CopiedDuration);
        ShowCopied(false);
    }

    private void ShowCopied(bool copied)
    {
        CopyIcon.Data = (Geometry)this.FindResource(copied ? "CheckIcon" : "CopyIcon")!;
        CopyText.Text = copied ? "Copied" : "Copy";
    }
}
