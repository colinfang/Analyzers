namespace Analyzers.Tests;

[TestClass]
public class UseGetArrayDataReferenceAnalyzerTests
{
    private static Task VerifyAsync(string source)
        => AnalyzerTestHelper.VerifyAnalyzerAsync<UseGetArrayDataReferenceAnalyzer>(source);

    [TestMethod]
    public async Task GetReference_WithArrayAsSpan_ReportsDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(int[] values)
                {
                    ref int r = ref [|MemoryMarshal.GetReference(values.AsSpan())|];
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithMemoryExtensionsAsSpan_ReportsDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(byte[] values)
                {
                    ref byte r = ref [|MemoryMarshal.GetReference(MemoryExtensions.AsSpan(values))|];
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithImplicitArrayConversion_ReportsDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(double[] values)
                {
                    ref double r = ref [|MemoryMarshal.GetReference((ReadOnlySpan<double>)values)|];
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithArraySpanCast_ReportsDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(float[] values)
                {
                    ref float r = ref [|MemoryMarshal.GetReference((Span<float>)values)|];
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithArrayAsSpanZeroOffset_ReportsDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(int[] values)
                {
                    ref int r = ref [|MemoryMarshal.GetReference(values.AsSpan(0))|];
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithArrayAsSpanNonZeroOffset_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(int[] values)
                {
                    ref int r = ref MemoryMarshal.GetReference(values.AsSpan(1));
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithArrayAsSpanOffsetAndLength_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(int[] values)
                {
                    ref int r = ref MemoryMarshal.GetReference(values.AsSpan(0, 2));
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetReference_WithRegularSpanParameter_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(Span<int> span)
                {
                    ref int r = ref MemoryMarshal.GetReference(span);
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task GetArrayDataReference_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.InteropServices;

            public class Test
            {
                public void Method(int[] values)
                {
                    ref int r = ref MemoryMarshal.GetArrayDataReference(values);
                }
            }
            """;

        await VerifyAsync(code);
    }
}
