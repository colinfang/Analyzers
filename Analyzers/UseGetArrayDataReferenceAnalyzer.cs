using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseGetArrayDataReferenceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PERF0003";
    private const string Title = "Use MemoryMarshal.GetArrayDataReference instead of MemoryMarshal.GetReference";
    private const string MessageFormat = "Use MemoryMarshal.GetArrayDataReference({0}) instead of MemoryMarshal.GetReference for arrays";
    private const string Description = "Calling MemoryMarshal.GetReference on an array span creates an unnecessary intermediate Span or ReadOnlySpan struct. Use MemoryMarshal.GetArrayDataReference directly on the array to eliminate overhead.";
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

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var memoryMarshalType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.MemoryMarshal");
            if (memoryMarshalType is null)
            {
                return;
            }

            if (memoryMarshalType.GetMembers("GetArrayDataReference").Length == 0)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(syntaxContext, memoryMarshalType),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol memoryMarshalType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.Name != "GetReference")
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, memoryMarshalType))
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        var argument = invocation.ArgumentList.Arguments[0];
        var argExpr = argument.Expression;

        var (isArrayTarget, arrayExprText) = TryExtractArrayTarget(argExpr, semanticModel, context.CancellationToken);
        if (isArrayTarget && arrayExprText is not null)
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), arrayExprText);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static (bool IsArray, string? ArrayText) TryExtractArrayTarget(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            if (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }
            else if (expression is CastExpressionSyntax castExpr)
            {
                expression = castExpr.Expression;
            }
            else
            {
                break;
            }
        }

        // 1. Check if argument expression is an invocation of AsSpan (instance or static MemoryExtensions style)
        if (expression is InvocationExpressionSyntax invocation)
        {
            var methodSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol is { Name: "AsSpan" } && methodSymbol.ContainingType?.Name == "MemoryExtensions")
            {
                ExpressionSyntax? arrayCandidate = null;
                int extraArgStartIndex = 0;

                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var exprSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol;
                    if (exprSymbol is INamedTypeSymbol)
                    {
                        // Static call style: MemoryExtensions.AsSpan(array, ...)
                        if (invocation.ArgumentList.Arguments.Count > 0)
                        {
                            arrayCandidate = invocation.ArgumentList.Arguments[0].Expression;
                            extraArgStartIndex = 1;
                        }
                    }
                    else
                    {
                        // Extension call style: array.AsSpan(...)
                        arrayCandidate = memberAccess.Expression;
                        extraArgStartIndex = 0;
                    }
                }
                else if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    // Static call without member access (e.g. static import): AsSpan(array, ...)
                    arrayCandidate = invocation.ArgumentList.Arguments[0].Expression;
                    extraArgStartIndex = 1;
                }

                if (arrayCandidate != null)
                {
                    int extraArgCount = invocation.ArgumentList.Arguments.Count - extraArgStartIndex;
                    if (extraArgCount > 0)
                    {
                        if (extraArgCount == 1)
                        {
                            var constantOpt = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[extraArgStartIndex].Expression, cancellationToken);
                            if (!constantOpt.HasValue || constantOpt.Value is not int val || val != 0)
                            {
                                return (false, null);
                            }
                        }
                        else
                        {
                            return (false, null);
                        }
                    }

                    var arrayType = semanticModel.GetTypeInfo(arrayCandidate, cancellationToken).Type;
                    if (arrayType is IArrayTypeSymbol { Rank: 1 })
                    {
                        return (true, arrayCandidate.ToString());
                    }
                }
            }
        }

        // 2. Direct array argument passed with implicit conversion to Span<T> / ReadOnlySpan<T>
        var argType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        if (argType is IArrayTypeSymbol { Rank: 1 })
        {
            return (true, expression.ToString());
        }

        return (false, null);
    }
}
