using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Fluently;

/// <summary>
///     Shows a <see cref="Geometry" /> given as an icon as a <see cref="PathIcon" />; any other icon is shown as it is.
/// </summary>
internal static class IconTemplate
{
    public static IDataTemplate Create(double size) =>
        new FuncDataTemplate<Geometry>((geometry, _) => new PathIcon { Data = geometry, Width = size, Height = size });
}
