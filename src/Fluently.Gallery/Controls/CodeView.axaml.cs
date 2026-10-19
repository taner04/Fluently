using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Fluently.Gallery.Controls;

public partial class CodeView : UserControl
{
    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<CodeView, string?>(nameof(Code));

    public static readonly StyledProperty<CodeLanguage> LanguageProperty =
        AvaloniaProperty.Register<CodeView, CodeLanguage>(nameof(Language));

    private static readonly RegistryOptions DarkRegistry = new(ThemeName.DarkPlus);
    private static readonly RegistryOptions LightRegistry = new(ThemeName.LightPlus);
    private bool _isLight;

    private TextMate.Installation? _textMate;

    public CodeView()
    {
        InitializeComponent();
        ActualThemeVariantChanged += (_, _) => ApplyTheme();

        AddHandler(RequestBringIntoViewEvent, static (_, e) => e.Handled = true);
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

    private RegistryOptions Registry => _isLight ? LightRegistry : DarkRegistry;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CodeProperty)
        {
            Editor.Text = Code ?? "";
        }
        else if (change.Property == LanguageProperty)
        {
            ApplyLanguage();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyTheme();
    }

    private void ApplyLanguage() =>
        _textMate?.SetGrammar(Registry.GetScopeByLanguageId(Language == CodeLanguage.Xml ? "xml" : "csharp"));

    private void ApplyTheme()
    {
        if (TopLevel.GetTopLevel(this) is null)
        {
            return;
        }

        var isLight = ActualThemeVariant == ThemeVariant.Light;
        if (_textMate is not null && isLight == _isLight)
        {
            return;
        }

        _textMate?.Dispose();
        _isLight = isLight;
        _textMate = Editor.InstallTextMate(Registry);
        ApplyLanguage();
    }
}
