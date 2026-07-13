namespace Analyzers.Tests;

[TestClass]
public class DirectNuintCastOnSignedIntCodeFixTests
{
    private static Task VerifyCodeFixAsync(string source, string fixedSource)
        => AnalyzerTestHelper.VerifyCodeFixAsync<DirectNuintCastOnSignedIntAnalyzer, DirectNuintCastOnSignedIntCodeFixProvider>(source, fixedSource);

    [TestMethod]
    public async Task PERF0006_FixesNuintCastOnIntVariable()
    {
        const string source = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nuint res = {|PERF0006:(nuint)val|};
                }
            }
            """;

        const string fixedSource = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nuint res = (nuint)(uint)val;
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource);
    }

    [TestMethod]
    public async Task PERF0006_FixesNintCastOnIntVariable()
    {
        const string source = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nint res = {|PERF0006:(nint)val|};
                }
            }
            """;

        const string fixedSource = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nint res = (nint)(uint)val;
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource);
    }
}
