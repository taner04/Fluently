namespace Fluently.Avalonia.SourceGenerator.Generators.Views;

[Generator(LanguageNames.CSharp)]
public sealed class ViewRegistrationGenerator : IIncrementalGenerator
{
    private const string ViewForMetadataName = "Fluently.IViewFor`1";
    private const string ControlMetadataName = "Avalonia.Controls.Control";
    private const string HintName = "Fluently.GeneratedViews.g.cs";

    private static readonly SymbolDisplayFormat TypeNameFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) => CreateCandidates(syntaxContext, cancellationToken))
            .SelectMany(static (candidates, _) => candidates);

        var hasViewFor = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName(ViewForMetadataName) is not null);

        context.RegisterSourceOutput(candidates.Collect().Combine(hasViewFor), static (output, input) =>
        {
            var (discovered, isFluentlyReferenced) = input;
            if (!isFluentlyReferenced)
            {
                return;
            }

            var registrations = new List<ViewCandidate>();
            var viewsByViewModel = new Dictionary<string, string>(StringComparer.Ordinal);

            // A partial view declared in several files is found once per declaration; keep the first.
            foreach (var candidate in discovered
                         .GroupBy(static candidate => (candidate.ViewTypeName, candidate.ViewModelTypeName))
                         .Select(static group => group
                             .OrderBy(static candidate => candidate.Location.Path, StringComparer.Ordinal)
                             .ThenBy(static candidate => candidate.Location.SpanStart)
                             .First())
                         .OrderBy(static candidate => candidate.ViewModelTypeName, StringComparer.Ordinal)
                         .ThenBy(static candidate => candidate.ViewTypeName, StringComparer.Ordinal))
            {
                output.CancellationToken.ThrowIfCancellationRequested();

                if (candidate.Problem is { } problem)
                {
                    output.ReportDiagnostic(ViewDiagnostics.Create(
                        problem,
                        candidate.Location,
                        candidate.ViewTypeName,
                        candidate.ViewModelTypeName));
                    continue;
                }

                if (viewsByViewModel.TryGetValue(candidate.ViewModelTypeName, out var existingView))
                {
                    output.ReportDiagnostic(ViewDiagnostics.Create(
                        ViewDiagnosticKind.DuplicateView,
                        candidate.Location,
                        candidate.ViewTypeName,
                        candidate.ViewModelTypeName,
                        existingView));
                    continue;
                }

                viewsByViewModel.Add(candidate.ViewModelTypeName, candidate.ViewTypeName);
                registrations.Add(candidate);
            }

            output.AddSource(HintName, SourceText.From(Render(registrations), Encoding.UTF8));
        });
    }

    private static ImmutableArray<ViewCandidate> CreateCandidates(
        GeneratorSyntaxContext syntaxContext,
        CancellationToken cancellationToken)
    {
        if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, cancellationToken)
                is not INamedTypeSymbol { IsAbstract: false, IsGenericType: false } view
            || syntaxContext.SemanticModel.Compilation.GetTypeByMetadataName(ViewForMetadataName) is not { } viewFor)
        {
            return ImmutableArray<ViewCandidate>.Empty;
        }

        var control = syntaxContext.SemanticModel.Compilation.GetTypeByMetadataName(ControlMetadataName);
        var location = SourceLocation.From(
            ((ClassDeclarationSyntax)syntaxContext.Node).Identifier.GetLocation());
        var problem = GetProblem(view, control);
        var builder = ImmutableArray.CreateBuilder<ViewCandidate>();

        foreach (var contract in view.Interfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, viewFor))
            {
                builder.Add(new ViewCandidate(
                    view.ToDisplayString(TypeNameFormat),
                    contract.TypeArguments[0].ToDisplayString(TypeNameFormat),
                    location,
                    problem));
            }
        }

        return builder.ToImmutable();
    }

    private static ViewDiagnosticKind? GetProblem(INamedTypeSymbol view, INamedTypeSymbol? control)
    {
        if (control is null || !DerivesFrom(view, control))
        {
            return ViewDiagnosticKind.NotAControl;
        }

        for (ISymbol? symbol = view; symbol is INamedTypeSymbol type; symbol = type.ContainingType)
        {
            if (type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return ViewDiagnosticKind.NotAccessible;
            }
        }

        return null;
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static string Render(IReadOnlyList<ViewCandidate> registrations)
    {
        var source = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("#nullable enable")
            .AppendLine()
            .AppendLine("namespace Fluently;")
            .AppendLine()
            .AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"Fluently.Avalonia.SourceGenerator\", \"1.0.0.0\")]")
            .AppendLine("internal static class FluentlyViewsServiceCollectionExtensions")
            .AppendLine("{")
            .AppendLine("    /// <summary>")
            .AppendLine("    /// Registers the view locator with every view of this assembly that implements")
            .AppendLine("    /// <see cref=\"global::Fluently.IViewFor{TViewModel}\"/>.")
            .AppendLine("    /// Call <c>UseFluently</c> on the application afterwards to activate the view locator.")
            .AppendLine("    /// </summary>")
            .AppendLine("    internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddFluentlyViews(")
            .AppendLine("        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>")
            .AppendLine("        global::Fluently.AvaloniaServiceCollectionExtensions.AddFluentlyAvalonia(services, static views =>")
            .AppendLine("        {");

        foreach (var registration in registrations)
        {
            source
                .Append("            views.Add<")
                .Append(registration.ViewTypeName)
                .Append(", ")
                .Append(registration.ViewModelTypeName)
                .AppendLine(">();");
        }

        return source
            .AppendLine("        });")
            .AppendLine("}")
            .ToString();
    }
}
