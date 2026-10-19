using Avalonia;
using Avalonia.Controls;

namespace Fluently.Gallery.Controls;

public partial class ExampleCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ExampleCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<ExampleCard, string?>(nameof(Description));

    public static readonly StyledProperty<object?> ExampleProperty =
        AvaloniaProperty.Register<ExampleCard, object?>(nameof(Example));

    public static readonly StyledProperty<string?> CSharpProperty =
        AvaloniaProperty.Register<ExampleCard, string?>(nameof(CSharp));

    public static readonly StyledProperty<string?> XamlProperty =
        AvaloniaProperty.Register<ExampleCard, string?>(nameof(Xaml));

    public static readonly StyledProperty<bool> CodeInlineProperty =
        AvaloniaProperty.Register<ExampleCard, bool>(nameof(CodeInline));

    public ExampleCard()
    {
        InitializeComponent();
        UpdateParts();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Example
    {
        get => GetValue(ExampleProperty);
        set => SetValue(ExampleProperty, value);
    }

    public string? CSharp
    {
        get => GetValue(CSharpProperty);
        set => SetValue(CSharpProperty, value);
    }

    public string? Xaml
    {
        get => GetValue(XamlProperty);
        set => SetValue(XamlProperty, value);
    }

    public bool CodeInline
    {
        get => GetValue(CodeInlineProperty);
        set => SetValue(CodeInlineProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ExampleProperty || change.Property == CSharpProperty ||
            change.Property == XamlProperty || change.Property == CodeInlineProperty)
        {
            UpdateParts();
        }
    }

    private void UpdateParts()
    {
        if (ExampleArea is null || CodeArea is null || CodePanel is null)
        {
            return;
        }

        var hasExample = Example is not null;
        var hasCode = CSharp is not null || Xaml is not null;

        ExampleArea.IsVisible = hasExample;
        ExampleArea.CornerRadius = hasCode ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(8);
        ExampleArea.BorderThickness = new Thickness(1);

        CodeArea.IsVisible = hasCode;
        CodeArea.CornerRadius = hasExample ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(8);
        CodeArea.BorderThickness = hasExample ? new Thickness(1, 0, 1, 1) : new Thickness(1);

        CodeExpander.Content = null;
        InlineCode.Child = null;
        if (CodeInline)
        {
            InlineCode.Child = CodePanel;
        }
        else
        {
            CodeExpander.Content = CodePanel;
        }

        CodeExpander.IsVisible = !CodeInline;
        InlineCode.IsVisible = CodeInline;
    }
}
