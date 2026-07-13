namespace Analyzers.Tests;

[TestClass]
public class UnsafeAddMissingNuintCastCodeFixTests
{
    private static Task VerifyCodeFixAsync(string source, string fixedSource)
        => AnalyzerTestHelper.VerifyCodeFixAsync<UnsafeAddMissingNuintCastAnalyzer, UnsafeAddMissingNuintCastCodeFixProvider>(source, fixedSource);

    [TestMethod]
    public async Task PERF0005_CastsOffsetToUint()
    {
        const string source = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, {|PERF0005:offset|});
                }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, (uint)offset);
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource);
    }

    [TestMethod]
    public async Task PERF0005_CastsLiteralOffsetToUint()
    {
        const string source = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, {|PERF0005:1|});
                }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, (uint)1);
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource);
    }

    [TestMethod]
    public async Task PERF0004_RemovesRedundantNuintCastOnUintVariable()
    {
        const string source = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, uint offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, {|PERF0004:(nuint)offset|});
                }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, uint offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, offset);
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource);
    }
}
