using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    public static async Task VerifyAnalyzerAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        ArgumentNullException.ThrowIfNull(source);

        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        await test.RunAsync();
    }

    public static async Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(string source, string fixedSource)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(fixedSource);

        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        await test.RunAsync();
    }
}
