namespace Fluently.SourceGenerator.Generators.Common;

internal static class SymbolExtensions
{
    internal static bool IsVisibleToGeneratedCode(this INamedTypeSymbol type)
    {
        for (ISymbol? symbol = type; symbol is INamedTypeSymbol current; symbol = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }
        }

        return true;
    }
}
