using Microsoft.CodeAnalysis.Diagnostics;

namespace Fluently.SourceGenerator.UnitTests.Infrastructure;

internal sealed class TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalValues)
    : AnalyzerConfigOptionsProvider
{
    private static readonly AnalyzerConfigOptions Empty =
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(globalValues);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;
}
