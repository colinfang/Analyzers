using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnsafeAddMissingNuintCastAnalyzer : DiagnosticAnalyzer
{
    public const string MissingCastDiagnosticId = "PERF0005";
    public const string RedundantCastDiagnosticId = "PERF0004";

    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor MissingCastRule = new(
        MissingCastDiagnosticId,
        "Cast signed offset to uint in Unsafe operations",
        "Cast signed offset '{0}' to uint to prevent emitting movsxd instruction in Unsafe.{1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Passing signed integers to Unsafe operations causes the JIT to emit sign-extension instructions (movsxd). Cast non-negative offsets to uint to zero-extend without overhead.");

    private static readonly DiagnosticDescriptor RedundantCastRule = new(
        RedundantCastDiagnosticId,
        "Redundant cast in Unsafe operation",
        "Cast '{0}' is redundant in Unsafe.{1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The offset is already an unsigned 32-bit integer (uint) and does not emit sign-extension instructions. The cast is redundant.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MissingCastRule, RedundantCastRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var unsafeType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.Unsafe");
            if (unsafeType is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(syntaxContext, unsafeType),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol unsafeType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, unsafeType))
        {
            return;
        }

        if (methodSymbol.Name is not ("Add" or "AddByteOffset" or "Subtract" or "SubtractByteOffset"))
        {
            return;
        }

        if (methodSymbol.Parameters.Length != 2)
        {
            return;
        }

        var offsetArgument = GetOffsetArgument(invocation, methodSymbol);
        if (offsetArgument is null)
        {
            return;
        }

        AnalyzeOffsetExpression(context, offsetArgument.Expression, methodSymbol.Name);
    }

    private static void AnalyzeOffsetExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, string methodName)
    {
        var semanticModel = context.SemanticModel;
        var typeInfo = semanticModel.GetTypeInfo(expression, context.CancellationToken);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol is null || typeSymbol.TypeKind == TypeKind.Error)
        {
            return;
        }

        var unwrappedExpr = UnwrapParentheses(expression);

        if (unwrappedExpr is CastExpressionSyntax castExpr)
        {
            var innerType = semanticModel.GetTypeInfo(castExpr.Expression, context.CancellationToken).Type;
            if (innerType is not null && innerType.TypeKind != TypeKind.Error)
            {
                // If inner type is uint, casting to (nuint) or (uint) is redundant
                if (innerType.SpecialType == SpecialType.System_UInt32)
                {
                    var diagnostic = Diagnostic.Create(RedundantCastRule, expression.GetLocation(), expression.ToString(), methodName);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                // If inner type is signed (e.g. int) and cast is to (nuint) or (nint), it's ineffective and still sign-extends
                if (typeSymbol.SpecialType is SpecialType.System_UIntPtr or SpecialType.System_IntPtr)
                {
                    var diagnostic = Diagnostic.Create(MissingCastRule, expression.GetLocation(), expression.ToString(), methodName);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }

        // Native uint variables, 1u literals, or valid (uint)int_var casts
        if (typeSymbol.SpecialType == SpecialType.System_UInt32)
        {
            return;
        }

        // Native nuint variables (not explicit casts) are valid
        if (typeSymbol.SpecialType == SpecialType.System_UIntPtr && unwrappedExpr is not CastExpressionSyntax)
        {
            return;
        }

        // Otherwise (int, nint, etc.), report missing cast warning (PERF0005)
        var missingCastDiagnostic = Diagnostic.Create(MissingCastRule, expression.GetLocation(), expression.ToString(), methodName);
        context.ReportDiagnostic(missingCastDiagnostic);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expr)
    {
        while (expr is ParenthesizedExpressionSyntax parenthesized)
        {
            expr = parenthesized.Expression;
        }
        return expr;
    }

    private static ArgumentSyntax? GetOffsetArgument(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
        {
            return null;
        }

        var offsetParamName = methodSymbol.Parameters[1].Name;

        foreach (var argument in arguments)
        {
            if (argument.NameColon is { } nameColon)
            {
                if (nameColon.Name.Identifier.Text == offsetParamName)
                {
                    return argument;
                }
            }
        }

        // Positional argument
        if (arguments.Count == 2)
        {
            return arguments[1];
        }

        return null;
    }
}
