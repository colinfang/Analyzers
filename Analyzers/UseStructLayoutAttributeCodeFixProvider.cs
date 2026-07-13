using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseStructLayoutAttributeCodeFixProvider)), Shared]
public sealed class UseStructLayoutAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseStructLayoutAttributeAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var span = diagnostic.Location.SourceSpan;
        var structDeclaration = root.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<StructDeclarationSyntax>().FirstOrDefault();
        if (structDeclaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [StructLayout(LayoutKind.Auto)]",
                createChangedDocument: c => AddStructLayoutAttributeAsync(context.Document, structDeclaration, c),
                equivalenceKey: nameof(UseStructLayoutAttributeCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> AddStructLayoutAttributeAsync(Document document, StructDeclarationSyntax structDeclaration, CancellationToken cancellationToken)
    {
        var root = (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

        var attributeArgument = SyntaxFactory.AttributeArgument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("LayoutKind"),
                SyntaxFactory.IdentifierName("Auto")));

        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("StructLayout"),
            SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(attributeArgument)));

        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newStructDeclaration = structDeclaration.AddAttributeLists(attributeList);

        var newRoot = root.ReplaceNode(structDeclaration, newStructDeclaration);

        bool hasUsing = newRoot.Usings.Any(u => u.Name?.ToString() == "System.Runtime.InteropServices");
        if (!hasUsing)
        {
            var interopUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.InteropServices"))
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithAdditionalAnnotations(Formatter.Annotation);

            newRoot = newRoot.AddUsings(interopUsing);
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        var text = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var normalizedText = text.ToString().Replace("\r\n", "\n");
        return newDocument.WithText(Microsoft.CodeAnalysis.Text.SourceText.From(normalizedText));
    }
}
