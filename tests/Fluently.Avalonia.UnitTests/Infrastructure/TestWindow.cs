using Avalonia.Controls;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

internal static class TestWindow
{
    private static readonly AsyncLocal<List<Window>?> Opened = new();

    internal static Window Show(Control content, double width = 800, double height = 600)
    {
        var window = new Window { Content = content, Width = width, Height = height };
        Track(window);
        window.Show();
        return window;
    }

    internal static void Track(Window window) => Opened.Value?.Add(window);

    internal static List<Window> BeginTest() => Opened.Value = [];
}
