namespace Fluently.SourceGenerator.Generators.Views;

[Generator(LanguageNames.CSharp)]
public sealed class ViewRegistrationGenerator : IIncrementalGenerator
{
    private const string ViewForMetadataName = "Fluently.IViewFor`1";
    private const string HintName = "Fluently.ViewRegistrations.g.cs";

    // The base type every view must derive from, per UI framework. Only the frameworks the project
    // references are checked, so the generator itself stays independent of any UI framework.
    private static readonly string[] UiElementMetadataNames =
    [
        "Avalonia.Controls.Control",
        "System.Windows.FrameworkElement",
        "Microsoft.UI.Xaml.FrameworkElement",
        "Microsoft.Maui.Controls.VisualElement"
    ];

    private static readonly SymbolDisplayFormat TypeNameFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) => CreateCandidates(syntaxContext, cancellationToken))
            .SelectMany(static (candidates, _) => candidates);

        var assemblyName =
            context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Views");

        context.RegisterSourceOutput(candidates.Collect().Combine(assemblyName), static (output, input) =>
        {
            var (discovered, assembly) = input;
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
                        candidate.ViewModelTypeName,
                        candidate.ExpectedBaseTypes));
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

            if (registrations.Count > 0)
            {
                output.AddSource(HintName, SourceText.From(Render(assembly, registrations), Encoding.UTF8));
            }
        });
    }

    private static ImmutableArray<ViewCandidate> CreateCandidates(
        GeneratorSyntaxContext syntaxContext,
        CancellationToken cancellationToken)
    {
        var compilation = syntaxContext.SemanticModel.Compilation;
        if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node, cancellationToken)
                is not INamedTypeSymbol { IsAbstract: false, IsGenericType: false } view
            || compilation.GetTypeByMetadataName(ViewForMetadataName) is not { } viewFor)
        {
            return ImmutableArray<ViewCandidate>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ViewCandidate>();
        foreach (var contract in view.AllInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, viewFor)
                || IsRegisteredByConcreteBase(view, contract))
            {
                continue;
            }

            var uiElementTypes = UiElementMetadataNames
                .Select(compilation.GetTypeByMetadataName)
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            var viewModel = contract.TypeArguments[0];
            var constructorArguments = GetConstructorArguments(view, viewModel);

            builder.Add(new ViewCandidate(
                view.ToDisplayString(TypeNameFormat),
                viewModel.ToDisplayString(TypeNameFormat),
                SourceLocation.From(((ClassDeclarationSyntax)syntaxContext.Node).Identifier.GetLocation()),
                GetProblem(view, uiElementTypes) ??
                (constructorArguments is null ? ViewDiagnosticKind.NoViewModelConstructor : null),
                string.Join(", ", uiElementTypes.Select(static type => type.ToDisplayString())),
                constructorArguments ?? string.Empty));
        }

        return builder.ToImmutable();
    }

    private static bool IsRegisteredByConcreteBase(INamedTypeSymbol view, INamedTypeSymbol contract)
    {
        if (view.Interfaces.Contains(contract, SymbolEqualityComparer.Default))
        {
            return false;
        }

        for (var current = view.BaseType; current is not null; current = current.BaseType)
        {
            if (!current.IsAbstract && current.AllInterfaces.Contains(contract, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    private static ViewDiagnosticKind? GetProblem(INamedTypeSymbol view,
        ImmutableArray<INamedTypeSymbol> uiElementTypes)
    {
        if (!uiElementTypes.IsEmpty && !uiElementTypes.Any(uiElementType => DerivesFrom(view, uiElementType)))
        {
            return ViewDiagnosticKind.NotAUiElement;
        }

        return view.IsVisibleToGeneratedCode() ? null : ViewDiagnosticKind.NotAccessible;
    }

    // Uses the accessible constructor whose first parameter is the view model; when there are several,
    // the one with the most parameters. Every further parameter is resolved from the services.
    private static string? GetConstructorArguments(INamedTypeSymbol view, ITypeSymbol viewModel)
    {
        var constructor = view.InstanceConstructors
            .Where(candidate => candidate.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                                    or Accessibility.ProtectedOrInternal
                                && candidate.Parameters.Length > 0
                                && SymbolEqualityComparer.Default.Equals(candidate.Parameters[0].Type, viewModel))
            .OrderByDescending(static candidate => candidate.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null)
        {
            return null;
        }

        var arguments = new List<string> { "viewModel" };
        foreach (var parameter in constructor.Parameters.Skip(1))
        {
            var method = parameter.Type.IsReferenceType && parameter.NullableAnnotation == NullableAnnotation.Annotated
                ? "GetService"
                : "GetRequiredService";
            arguments.Add(
                $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.{method}<" +
                $"{parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeNameFormat)}>(services)");
        }

        return string.Join(", ", arguments);
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

    private static string Render(string assemblyName, IReadOnlyList<ViewCandidate> registrations)
    {
        var className = "ViewRegistrations_" + new string([
            .. assemblyName.Select(static character => char.IsLetterOrDigit(character) ? character : '_')
        ]);

        var lines = new StringBuilder();
        foreach (var registration in registrations)
        {
            lines
                .Append("        global::Fluently.ViewRegistry.Add<")
                .Append(registration.ViewModelTypeName)
                .Append(">(static (services, viewModel) => new ")
                .Append(registration.ViewTypeName)
                .Append('(')
                .Append(registration.ConstructorArguments)
                .Append("));\n");
        }

        return SourceTemplate.Render(
            "ViewRegistrations.cs.template",
            ("className", className),
            ("registrations", lines.ToString()));
    }
}
