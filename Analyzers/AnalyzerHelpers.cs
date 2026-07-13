using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

[assembly: InternalsVisibleTo("Analyzers.Tests")]

namespace Analyzers;

internal static class AnalyzerHelpers
{
    public static bool ImplementsInterface(
        [NotNullWhen(true)] this ITypeSymbol? typeSymbol,
        [NotNullWhen(true)] INamedTypeSymbol? interfaceSymbol)
    {
        return ImplementsInterfaceCore(typeSymbol, interfaceSymbol, visited: null);
    }

    private static bool ImplementsInterfaceCore(
        ITypeSymbol? typeSymbol,
        INamedTypeSymbol? interfaceSymbol,
        HashSet<ITypeSymbol>? visited)
    {
        if (typeSymbol is null || interfaceSymbol is null)
        {
            return false;
        }

        typeSymbol = typeSymbol.UnwrapNullable();

        if (SymbolEqualityComparer.Default.Equals(typeSymbol, interfaceSymbol))
        {
            return true;
        }

        if (typeSymbol is ITypeParameterSymbol typeParam)
        {
            visited ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            if (!visited.Add(typeParam))
            {
                return false;
            }

            foreach (var constraint in typeParam.ConstraintTypes)
            {
                if (ImplementsInterfaceCore(constraint, interfaceSymbol, visited))
                {
                    return true;
                }
            }
            return false;
        }

        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAttribute(
        [NotNullWhen(true)] this ISymbol? symbol,
        [NotNullWhen(true)] INamedTypeSymbol? attributeType)
    {
        if (symbol is null || attributeType is null)
        {
            return false;
        }

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    [return: NotNullIfNotNull(nameof(typeSymbol))]
    public static ITypeSymbol? UnwrapNullable(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var typeArgument] })
        {
            return typeArgument;
        }

        return typeSymbol;
    }
}

