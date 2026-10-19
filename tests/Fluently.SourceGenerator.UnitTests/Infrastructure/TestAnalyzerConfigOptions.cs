using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fluently.SourceGenerator.UnitTests.Infrastructure;

internal sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
        values.TryGetValue(key, out value);
}
