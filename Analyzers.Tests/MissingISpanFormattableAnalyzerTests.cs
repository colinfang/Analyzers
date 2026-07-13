namespace Analyzers.Tests;

[TestClass]
public class MissingISpanFormattableAnalyzerTests
{
    private static Task VerifyAsync(string source)
        => AnalyzerTestHelper.VerifyAnalyzerAsync<MissingISpanFormattableAnalyzer>(source);

    [TestMethod]
    public async Task Interpolation_WithNonSpanFormattableType_ReportsDiagnostic()
    {
        const string code = """
            using System;

            public class MyCustomType { }

            public class Test
            {
                public void Method()
                {
                    var obj = new MyCustomType();
                    var s = $"Value: {[|obj|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithInt_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method()
                {
                    int value = 42;
                    var s = $"Value: {value}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithBool_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(bool flag, bool? nullableFlag)
                {
                    var s1 = $"Flag: {flag}";
                    var s2 = $"NullableFlag: {nullableFlag}";
                }
            }
            """;

        await VerifyAsync(code);
    }


    [TestMethod]
    public async Task Interpolation_WithStringAndChar_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method()
                {
                    string name = "Gemini";
                    char prefix = 'A';
                    var s = $"{prefix}: {name}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithCustomSpanFormattableType_NoDiagnostic()
    {
        const string code = """
            using System;

            public class MySpanFormattableType : ISpanFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => "";
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
                {
                    charsWritten = 0;
                    return true;
                }
            }

            public class Test
            {
                public void Method()
                {
                    var obj = new MySpanFormattableType();
                    var s = $"Value: {obj}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithNullableInt_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int? value)
                {
                    var s = $"Value: {value}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithNullableNonSpanFormattable_ReportsDiagnostic()
    {
        const string code = """
            using System;

            public struct MyCustomStruct { }

            public class Test
            {
                public void Method(MyCustomStruct? value)
                {
                    var s = $"Value: {[|value|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithIFormattableOnly_ReportsDiagnostic()
    {
        const string code = """
            using System;

            public class LegacyFormattable : IFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => "";
            }

            public class Test
            {
                public void Method(LegacyFormattable value)
                {
                    var s = $"Value: {[|value|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithArray_ReportsDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method(int[] arr)
                {
                    var s = $"Value: {[|arr|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithMultipleHoles_ReportsMultipleDiagnostics()
    {
        const string code = """
            using System;

            public class MyTypeA { }
            public class MyTypeB { }

            public class Test
            {
                public void Method(MyTypeA a, MyTypeB b)
                {
                    var s = $"A: {[|a|]}, B: {[|b|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithEnumType_NoDiagnostic()
    {
        const string code = """
            using System;

            public enum LogLevel { Debug, Info, Error }

            public class Test
            {
                public void Method(LogLevel level)
                {
                    var s = $"Status: {level}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithReadOnlySpanOfChar_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method()
                {
                    ReadOnlySpan<char> span = "Hello".AsSpan();
                    var s = $"Value: {span}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithSpanOfChar_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method()
                {
                    Span<char> span = new char[] { 'H', 'i' };
                    var s = $"Value: {span}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithFormattableStringTargetType_NoDiagnostic()
    {
        const string code = """
            using System;

            public class MyCustomType { }

            public class Test
            {
                public void Method()
                {
                    var obj = new MyCustomType();
                    FormattableString s = $"Value: {obj}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithUnconstrainedGeneric_ReportsDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method<T>(T value)
                {
                    var s = $"Value: {[|value|]}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithSpanFormattableConstrainedGeneric_NoDiagnostic()
    {
        const string code = """
            using System;

            public class Test
            {
                public void Method<T>(T value) where T : ISpanFormattable
                {
                    var s = $"Value: {value}";
                }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Interpolation_WithIFormattableTargetType_NoDiagnostic()
    {
        const string code = """
            using System;

            public class MyCustomType { }

            public class Test
            {
                public void Method()
                {
                    var obj = new MyCustomType();
                    IFormattable s = $"Value: {obj}";
                }
            }
            """;

        await VerifyAsync(code);
    }
}
