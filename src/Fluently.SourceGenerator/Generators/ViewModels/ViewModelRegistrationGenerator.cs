using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fluently.SourceGenerator.Generators.ViewModels;

[Generator(LanguageNames.CSharp)]
public sealed class ViewModelRegistrationGenerator : IIncrementalGenerator
{
    private const string HintName = "Fluently.ViewModelRegistrations.g.cs";
    private const string AttributeMetadataName = "Fluently.SingletonPageAttribute";

    private static readonly (string MetadataName, ViewModelRegistrationKind Kind)[] Attributes =
    [
        ("Fluently.SingletonPageAttribute", ViewModelRegistrationKind.SingletonPage),
        ("Fluently.TransientPageAttribute", ViewModelRegistrationKind.TransientPage),
        ("Fluently.ScopedPageAttribute", ViewModelRegistrationKind.ScopedPage),
        ("Fluently.DialogAttribute", ViewModelRegistrationKind.Dialog),
        ("Fluently.ToastAttribute", ViewModelRegistrationKind.Toast)
    ];

    private static readonly SymbolDisplayFormat TypeNameFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = Attributes
            .Select(attribute => context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    attribute.MetadataName,
                    static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    (attributeContext, _) => CreateCandidate(attributeContext, attribute.Kind))
                .Collect())
            .Aggregate(static (all, next) => all
                .Combine(next)
                .Select(static (pair, _) => pair.Left.AddRange(pair.Right)));

        var referencesFluently = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.GetTypeByMetadataName(AttributeMetadataName) is not null);

        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Combine(context.CompilationProvider)
            .Select(static (input, _) => RootNamespace(input.Left, input.Right));

        context.RegisterSourceOutput(candidates.Combine(referencesFluently).Combine(rootNamespace),
            static (output, input) =>
            {
                var ((discovered, isReferenced), @namespace) = input;
                if (!isReferenced)
                {
                    return;
                }

                var registrations = new List<ViewModelCandidate>();
                foreach (var group in discovered
                             .GroupBy(static candidate => candidate.TypeName)
                             .OrderBy(static group => group.Key, StringComparer.Ordinal))
                {
                    output.CancellationToken.ThrowIfCancellationRequested();

                    var first = group
                        .OrderBy(static candidate => candidate.Location.Path, StringComparer.Ordinal)
                        .ThenBy(static candidate => candidate.Location.SpanStart)
                        .First();

                    if (group.Select(static candidate => candidate.Kind).Distinct().Count() > 1)
                    {
                        output.ReportDiagnostic(ViewModelDiagnostics.Create(ViewModelDiagnosticKind.SeveralAttributes,
                            first));
                        continue;
                    }

                    if (first.Problem is { } problem)
                    {
                        output.ReportDiagnostic(ViewModelDiagnostics.Create(problem, first));
                        continue;
                    }

                    registrations.Add(first);
                }

                output.AddSource(HintName, SourceText.From(Render(@namespace, registrations), Encoding.UTF8));
            });
    }

    private static string RootNamespace(AnalyzerConfigOptionsProvider options, Compilation compilation)
    {
        var name = options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var configured)
                   && !string.IsNullOrWhiteSpace(configured)
            ? configured
            : compilation.AssemblyName ?? "Fluently";

        var segments = name.Split('.')
            .Select(static segment => new string([
                .. segment.Select(static character =>
                    char.IsLetterOrDigit(character) || character == '_' ? character : '_')
            ]))
            .Where(static segment => segment.Length > 0)
            .Select(static segment => char.IsDigit(segment[0]) ? "_" + segment : segment)
            .Select(static segment => SyntaxFacts.GetKeywordKind(segment) != SyntaxKind.None ? "@" + segment : segment);

        return string.Join(".", segments) is { Length: > 0 } valid ? valid : "Fluently";
    }

    private static ViewModelCandidate CreateCandidate(
        GeneratorAttributeSyntaxContext attributeContext,
        ViewModelRegistrationKind kind)
    {
        var type = (INamedTypeSymbol)attributeContext.TargetSymbol;
        var location = SourceLocation.From(
            attributeContext.Attributes.FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax().GetLocation()
            ?? type.Locations.FirstOrDefault()
            ?? Location.None);

        var problem = !IsCreatable(type)
            ? ViewModelDiagnosticKind.NotCreatable
            : !Implements(type, kind, attributeContext.SemanticModel.Compilation)
                ? ViewModelDiagnosticKind.AttributeDoesNotMatch
                : (ViewModelDiagnosticKind?)null;

        return new ViewModelCandidate(type.ToDisplayString(TypeNameFormat), kind, location, problem);
    }

    private static bool IsCreatable(INamedTypeSymbol type) =>
        !type.IsAbstract && !type.IsStatic && !type.IsGenericType && type.IsVisibleToGeneratedCode();

    private static bool Implements(INamedTypeSymbol type, ViewModelRegistrationKind kind, Compilation compilation)
    {
        var contractName = kind switch
        {
            ViewModelRegistrationKind.Dialog => "Fluently.IDialog`1",
            ViewModelRegistrationKind.Toast => "Fluently.IToast",
            _ => "Fluently.INavigableViewModel"
        };

        return compilation.GetTypeByMetadataName(contractName) is { } contract
               && type.AllInterfaces.Any(candidate =>
                   SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, contract));
    }

    private static string Render(string @namespace, IReadOnlyList<ViewModelCandidate> registrations)
    {
        const string extensions =
            "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions";
        var lines = new StringBuilder();
        foreach (var registration in registrations)
        {
            var method = registration.Kind switch
            {
                ViewModelRegistrationKind.SingletonPage => "TryAddSingleton",
                ViewModelRegistrationKind.ScopedPage => "TryAddScoped",
                _ => "TryAddTransient"
            };

            lines
                .Append("            ").Append(extensions).Append('.').Append(method)
                .Append('<').Append(registration.TypeName).Append(">(services);\n");
        }

        return SourceTemplate.Render(
            "ViewModelRegistrations.cs.template",
            ("namespace", @namespace),
            ("registrations", lines.ToString()));
    }
}
