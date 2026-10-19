namespace Fluently.SourceGenerator.Generators.Common;

internal readonly record struct SourceLocation(
    bool IsInSource,
    string Path,
    int SpanStart,
    int SpanLength,
    int StartLine,
    int StartCharacter,
    int EndLine,
    int EndCharacter)
{
    internal static SourceLocation From(Location location)
    {
        if (!location.IsInSource)
        {
            return default;
        }

        var lineSpan = location.GetLineSpan();
        return new SourceLocation(
            true,
            lineSpan.Path ?? string.Empty,
            location.SourceSpan.Start,
            location.SourceSpan.Length,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character);
    }

    internal Location ToLocation()
    {
        if (!IsInSource)
        {
            return Location.None;
        }

        return Location.Create(
            Path,
            new TextSpan(SpanStart, SpanLength),
            new LinePositionSpan(
                new LinePosition(StartLine, StartCharacter),
                new LinePosition(EndLine, EndCharacter)));
    }
}
