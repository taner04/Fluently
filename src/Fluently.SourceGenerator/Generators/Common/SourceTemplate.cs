namespace Fluently.SourceGenerator.Generators.Common;

/// <summary>
/// Loads the source templates embedded in this assembly and fills their <c>{{ name }}</c> placeholders. A placeholder
/// on a line of its own is replaced together with that line, so an empty value leaves no blank line behind.
/// </summary>
internal static class SourceTemplate
{
    private const string ResourcePrefix = "Fluently.SourceGenerator.Templates.";

    internal static string Render(string templateName, params (string Name, string Value)[] values)
    {
        var source = Load(templateName);
        foreach (var (name, value) in values)
        {
            var placeholder = "{{ " + name + " }}";
            source = source
                .Replace("\n" + placeholder + "\n", "\n" + value)
                .Replace(placeholder, value);
        }

        return source;
    }

    private static string Load(string templateName)
    {
        var resourceName = ResourcePrefix + templateName;
        using var stream = typeof(SourceTemplate).Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"The template '{resourceName}' is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }
}
