using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeAddMissingNuintCastCodeFixProvider)), Shared]
public sealed class UnsafeAddMissingNuintCastCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        UnsafeAddMissingNuintCastAnalyzer.MissingCastDiagnosticId,
        UnsafeAddMissingNuintCastAnalyzer.RedundantCastDiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;
        var expression = root.FindNode(span, getInnermostNodeForTie: true) as ExpressionSyntax;
        if (expression is null) return;

        if (diagnostic.Id == UnsafeAddMissingNuintCastAnalyzer.MissingCastDiagnosticId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Cast offset to uint",
                    createChangedDocument: c => CastToUintAsync(context.Document, expression, c),
                    equivalenceKey: "CastToUint"),
                diagnostic);
        }
        else if (diagnostic.Id == UnsafeAddMissingNuintCastAnalyzer.RedundantCastDiagnosticId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove redundant cast",
                    createChangedDocument: c => RemoveRedundantCastAsync(context.Document, expression, c),
                    equivalenceKey: "RemoveRedundantCast"),
                diagnostic);
        }
    }

    private static async Task<Document> CastToUintAsync(Document document, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var castExpression = SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName("uint"),
            expression.WithoutTrivia())
            .WithTriviaFrom(expression)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(expression, castExpression);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> RemoveRedundantCastAsync(Document document, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var unwrapped = UnwrapParentheses(expression);
        if (unwrapped is CastExpressionSyntax castExpr)
        {
            var replacementNode = castExpr.Expression
                .WithTriviaFrom(expression)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(expression, replacementNode);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
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
