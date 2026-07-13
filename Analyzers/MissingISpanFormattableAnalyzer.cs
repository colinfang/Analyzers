using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingISpanFormattableAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PERF0001";
    private const string Title = "Expression misses ISpanFormattable implementation";
    private const string MessageFormat = "The expression '{0}' inside the interpolated string hole does not implement ISpanFormattable, causing string allocation";
    private const string Description = "Types should implement ISpanFormattable inside interpolated strings to prevent unnecessary runtime string allocations.";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsArray = [Rule];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsArray;

    public override void Initialize(AnalysisContext context)
    {
        // Configure Roslyn to skip generated code and enable concurrent execution
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            var spanFormattableType = compilation.GetTypeByMetadataName("System.ISpanFormattable");
            if (spanFormattableType is null)
            {
                return;
            }

            var formattableType = compilation.GetTypeByMetadataName("System.IFormattable");
            var spanType = compilation.GetTypeByMetadataName("System.Span`1");
            var readOnlySpanType = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInterpolatedString(syntaxContext, spanFormattableType, formattableType, spanType, readOnlySpanType),
                SyntaxKind.InterpolatedStringExpression);
        });
    }

    private static void AnalyzeInterpolatedString(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol spanFormattableType,
        INamedTypeSymbol? formattableType,
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType)
    {
        var interpolatedString = (InterpolatedStringExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Skip if the interpolated string is target-typed to FormattableString or IFormattable
        var stringTypeInfo = semanticModel.GetTypeInfo(interpolatedString, context.CancellationToken);
        if (stringTypeInfo.ConvertedType?.ImplementsInterface(formattableType) == true)
        {
            return;
        }

        foreach (var contentItem in interpolatedString.Contents)
        {
            if (contentItem is InterpolationSyntax interpolation)
            {
                ExpressionSyntax expression = interpolation.Expression;
                var typeInfo = semanticModel.GetTypeInfo(expression, context.CancellationToken);
                var typeSymbol = typeInfo.Type;

                if (typeSymbol is null || typeSymbol.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                typeSymbol = typeSymbol.UnwrapNullable();

                // Ignore types that have specialized efficient handlers or cached strings in interpolated string handlers
                if (typeSymbol.SpecialType is SpecialType.System_String or SpecialType.System_Char or SpecialType.System_Boolean ||
                    typeSymbol.TypeKind == TypeKind.Enum ||
                    IsSpanOrReadOnlySpanOfChar(typeSymbol, spanType, readOnlySpanType))
                {
                    continue;
                }

                if (!typeSymbol.ImplementsInterface(spanFormattableType))
                {
                    var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), expression.ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsSpanOrReadOnlySpanOfChar(ITypeSymbol typeSymbol, INamedTypeSymbol? spanType, INamedTypeSymbol? readOnlySpanType)
    {
        if (typeSymbol is INamedTypeSymbol { TypeArguments: [var typeArg] } namedType &&
            typeArg.SpecialType == SpecialType.System_Char)
        {
            var originalDef = namedType.OriginalDefinition;
            return SymbolEqualityComparer.Default.Equals(originalDef, spanType) ||
                   SymbolEqualityComparer.Default.Equals(originalDef, readOnlySpanType);
        }

        return false;
    }
}
