namespace Analyzers.Tests;

[TestClass]
public class DirectNuintCastOnSignedIntAnalyzerTests
{
    private static Task VerifyAsync(string source)
        => AnalyzerTestHelper.VerifyAnalyzerAsync<DirectNuintCastOnSignedIntAnalyzer>(source);

    [TestMethod]
    public async Task NuintCast_OnIntVariable_ReportsPERF0006()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nuint res = {|PERF0006:(nuint)val|};
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task NintCast_OnIntVariable_ReportsPERF0006()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nint res = {|PERF0006:(nint)val|};
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task NuintCast_OnShortVariable_ReportsPERF0006()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(short val)
                {
                    nuint res = {|PERF0006:(nuint)val|};
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task NuintCast_OnUintCast_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    nuint res = (nuint)(uint)val;
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task NuintCast_OnUintVariable_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(uint val)
                {
                    nuint res = (nuint)val;
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task LongCast_OnIntVariable_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int val)
                {
                    long res = (long)val;
                }
            }
            """;

        await VerifyAsync(code);
    }
}
