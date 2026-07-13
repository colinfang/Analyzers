namespace Analyzers.Tests;

[TestClass]
public class UseStructLayoutAttributeAnalyzerTests
{
    private static Task VerifyAsync(string source)
        => AnalyzerTestHelper.VerifyAnalyzerAsync<UseStructLayoutAttributeAnalyzer>(source);

    [TestMethod]
    public async Task Struct_WithMultipleFieldsOfDifferentTypes_MissingStructLayout_ReportsDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct [|MyStruct|]
            {
                public int X;
                public double Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithMultipleFieldsOfDifferentTypes_ReferenceTypeAndValueType_ReportsDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct [|MyStruct|]
            {
                public string Name;
                public int Age;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithMultipleFieldsOfSameType_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct MyStruct
            {
                public int X;
                public int Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithStructLayoutAttribute_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Sequential)]
            public struct MyStruct
            {
                public int X;
                public double Y;
            }

            [StructLayout(LayoutKind.Auto)]
            public struct AutoStruct
            {
                public int X;
                public double Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithExplicitLayoutAttribute_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            [StructLayout(LayoutKind.Explicit)]
            public struct MyStruct
            {
                [FieldOffset(0)]
                public int X;
                [FieldOffset(4)]
                public double Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithSingleField_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct MyStruct
            {
                public int X;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithStaticAndConstFields_IgnoredCorrectly()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct MyStruct
            {
                public const int MaxCount = 100;
                public static double Scale = 1.0;
                public int X;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithEmptyNestedStructsOfSameType_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct EmptyStruct { }

            public struct MyStruct
            {
                public EmptyStruct E1;
                public EmptyStruct E2;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithEmptyNestedStructAndDifferentTypeField_MissingStructLayout_ReportsDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct EmptyStruct { }

            public struct [|MyStruct|]
            {
                public EmptyStruct E;
                public int X;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithPointerFieldsOfSameType_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public unsafe struct MyStruct
            {
                public int* P1;
                public int* P2;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task ClassOrEnum_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public class MyClass
            {
                public int X;
                public double Y;
            }

            public enum MyEnum
            {
                A,
                B
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithAutoPropertiesOfDifferentTypes_ReportsDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct [|MyStruct|]
            {
                public int X { get; set; }
                public double Y { get; set; }
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithPrimaryConstructorDifferentTypes_ReportsDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct [|MyStruct|](int x, double y)
            {
                public int X => x;
                public double Y => y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithPrimaryConstructorSameTypes_NoDiagnostic()
    {
        const string code = """
            using System.Runtime.InteropServices;

            public struct MyStruct(int x, int y)
            {
                public int X => x;
                public int Y => y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithMultipleReferenceTypes_NoDiagnostic()
    {
        const string code = """
            using System.Collections.Generic;

            public class MyGenericClass<T> { }

            public struct MyStruct
            {
                public string Name;
                public List<int> Items;
                public MyGenericClass<string> GenericObj;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithGenericClassFieldsDifferentTypeArguments_NoDiagnostic()
    {
        const string code = """
            public class MyGenericClass<T> { }

            public struct MyStruct<T1, T2>
            {
                public MyGenericClass<T1> Field1;
                public MyGenericClass<T2> Field2;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task RefStruct_NoDiagnostic()
    {
        const string code = """
            public ref struct MyRefStruct
            {
                public int X;
                public double Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithNullableSameCategory_NoDiagnostic()
    {
        const string code = """
            public struct MyStruct
            {
                public int X;
                public int? Y;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithDecimalAndDouble_NoDiagnostic()
    {
        const string code = """
            public struct MyStruct
            {
                public decimal Value;
                public double Scale;
            }
            """;

        await VerifyAsync(code);
    }

    [TestMethod]
    public async Task Struct_WithNestedStructSameCategory_NoDiagnostic()
    {
        const string code = """
            public struct Point2D
            {
                public int X;
                public int Y;
            }

            public struct Point3D
            {
                public Point2D Location;
                public int Z;
            }
            """;

        await VerifyAsync(code);
    }
}


