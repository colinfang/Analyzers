using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DirectNuintCastOnSignedIntCodeFixProvider)), Shared]
public sealed class DirectNuintCastOnSignedIntCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DirectNuintCastOnSignedIntAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;
        var node = root.FindNode(span, getInnermostNodeForTie: true);
        if (node is not CastExpressionSyntax castExpression) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Cast to uint first before native integer cast",
                createChangedDocument: c => CastToUintFirstAsync(context.Document, castExpression, c),
                equivalenceKey: "CastToUintFirst"),
            diagnostic);
    }

    private static async Task<Document> CastToUintFirstAsync(Document document, CastExpressionSyntax castExpression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var innerExpression = ParenthesizeIfNeeded(castExpression.Expression);

        var uintCast = SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName("uint"),
            innerExpression);

        var outerCast = SyntaxFactory.CastExpression(
            castExpression.Type,
            uintCast)
            .WithTriviaFrom(castExpression)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(castExpression, outerCast);
        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax ParenthesizeIfNeeded(ExpressionSyntax expression)
    {
        if (expression is ParenthesizedExpressionSyntax ||
            expression is IdentifierNameSyntax ||
            expression is MemberAccessExpressionSyntax ||
            expression is InvocationExpressionSyntax ||
            expression is ElementAccessExpressionSyntax ||
            expression is LiteralExpressionSyntax)
        {
            return expression;
        }

        return SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia());
    }
}
