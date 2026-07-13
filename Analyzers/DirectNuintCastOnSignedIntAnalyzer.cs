using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectNuintCastOnSignedIntAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PERF0006";

    private const string Title = "Cast signed integer to uint before casting to native integer";
    private const string MessageFormat = "Cast signed integer '{0}' to uint first before casting to '{1}' to prevent sign-extension (movsxd)";
    private const string Description = "Directly casting signed integers to nuint or nint emits sign-extension instructions (movsxd). Cast to uint first (e.g. (nuint)(uint)expr) to zero-extend without overhead.";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsArray = [Rule];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsArray;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeCastExpression, SyntaxKind.CastExpression);
    }

    private static void AnalyzeCastExpression(SyntaxNodeAnalysisContext context)
    {
        var castExpr = (CastExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var typeInfo = semanticModel.GetTypeInfo(castExpr, context.CancellationToken);
        var targetTypeSymbol = typeInfo.Type;

        if (targetTypeSymbol is null || targetTypeSymbol.TypeKind == TypeKind.Error)
        {
            return;
        }

        // Target type must be nuint (UIntPtr) or nint (IntPtr)
        if (targetTypeSymbol.SpecialType is not (SpecialType.System_UIntPtr or SpecialType.System_IntPtr))
        {
            return;
        }

        var unwrappedInner = UnwrapParentheses(castExpr.Expression);

        var innerTypeInfo = semanticModel.GetTypeInfo(unwrappedInner, context.CancellationToken);
        var innerTypeSymbol = innerTypeInfo.Type;

        if (innerTypeSymbol is null || innerTypeSymbol.TypeKind == TypeKind.Error)
        {
            return;
        }

        // Check if the inner expression is a signed 32-bit integer (int), 16-bit integer (short), or 8-bit integer (sbyte)
        if (innerTypeSymbol.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int16 or SpecialType.System_SByte)
        {
            var targetTypeName = castExpr.Type.ToString();
            var diagnostic = Diagnostic.Create(
                Rule,
                castExpr.GetLocation(),
                unwrappedInner.ToString(),
                targetTypeName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expr)
    {
        while (expr is ParenthesizedExpressionSyntax parenthesized)
        {
            expr = parenthesized.Expression;
        }
        return expr;
    }
}
