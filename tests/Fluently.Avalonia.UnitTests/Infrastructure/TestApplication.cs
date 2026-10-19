using Avalonia;
using Avalonia.Themes.Fluent;

namespace Fluently.Avalonia.UnitTests.Infrastructure;

public sealed class TestApplication : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}
