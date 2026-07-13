namespace Analyzers.Tests;

[TestClass]
public class UnsafeAddMissingNuintCastAnalyzerTests
{
    private static Task VerifyAsync(string source)
        => AnalyzerTestHelper.VerifyAnalyzerAsync<UnsafeAddMissingNuintCastAnalyzer>(source);

    [TestMethod]
    public async Task UnsafeAdd_WithIntVariable_ReportsPERF0005()
    {
        const string code = """
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

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithNuintCastOfInt_ReportsPERF0005()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, {|PERF0005:(nuint)offset|});
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithNintCastOfInt_ReportsPERF0005()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, {|PERF0005:(nint)offset|});
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithNuintCastOfUint_ReportsPERF0004()
    {
        const string code = """
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

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithUintCastOfInt_NoDiagnostic()
    {
        const string code = """
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

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithUintVariable_NoDiagnostic()
    {
        const string code = """
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

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithNuintVariable_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, nuint offset)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, offset);
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithUintLiteral_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr)
                {
                    ref byte res = ref Unsafe.Add(ref ptr, 1u);
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAdd_WithIntLiteral_ReportsPERF0005()
    {
        const string code = """
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

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAddByteOffset_WithIntVariable_ReportsPERF0005()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.AddByteOffset(ref ptr, {|PERF0005:offset|});
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeAddByteOffset_WithUintCast_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.AddByteOffset(ref ptr, (uint)offset);
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeSubtract_WithIntVariable_ReportsPERF0005()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Subtract(ref ptr, {|PERF0005:offset|});
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeSubtract_WithUintCast_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.Subtract(ref ptr, (uint)offset);
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeSubtractByteOffset_WithIntVariable_ReportsPERF0005()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.SubtractByteOffset(ref ptr, {|PERF0005:offset|});
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task UnsafeSubtractByteOffset_WithUintCast_NoDiagnostic()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;

            public class Test
            {
                public void Method(ref byte ptr, int offset)
                {
                    ref byte res = ref Unsafe.SubtractByteOffset(ref ptr, (uint)offset);
                }
            }
            """;

        await VerifyAsync(code);
    }
}
