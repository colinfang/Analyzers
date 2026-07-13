using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzers.Tests;

[TestClass]
public class AnalyzerHelpersTests
{
    private readonly CSharpCompilation _compilation = CreateCompilation();

    [TestMethod]
    public void AnalyzerHelpers_NullArguments_ReturnsFalseOrNull()
    {
        ITypeSymbol? nullType = null;
        ISymbol? nullSymbol = null;
        var intSymbol = GetSymbol("System.Int32");
        var interfaceSymbol = GetSymbol("IMyInterface");

        Assert.IsFalse(nullType.ImplementsInterface(null));
        Assert.IsFalse(nullType.ImplementsInterface(interfaceSymbol));
        Assert.IsFalse(intSymbol.ImplementsInterface(null));

        Assert.IsFalse(nullSymbol.HasAttribute(null));
        Assert.IsFalse(nullSymbol.HasAttribute(interfaceSymbol));
        Assert.IsFalse(intSymbol.HasAttribute(null));

        Assert.IsNull(nullType.UnwrapNullable());
    }

    [TestMethod]
    public void UnwrapNullable_NonNullableType_ReturnsSameType()
    {
        var intSymbol = GetSymbol("System.Int32");
        var unwrapped = intSymbol.UnwrapNullable();

        Assert.IsNotNull(unwrapped);
        Assert.AreSame(intSymbol, unwrapped);
    }

    [TestMethod]
    public void UnwrapNullable_NullableType_ReturnsUnderlyingType()
    {
        var nullableInt = _compilation.GetTypeByMetadataName("System.Nullable`1")?
            .Construct(_compilation.GetTypeByMetadataName("System.Int32")!);

        Assert.IsNotNull(nullableInt);
        var unwrapped = nullableInt.UnwrapNullable();

        Assert.IsNotNull(unwrapped);
        Assert.AreEqual("Int32", unwrapped.Name);
    }

    [TestMethod]
    public void ImplementsInterface_DirectImplementation_ReturnsTrue()
    {
        var myClass = GetSymbol("MyClass");
        var myInterface = GetSymbol("IMyInterface");

        Assert.IsTrue(myClass.ImplementsInterface(myInterface));
    }

    [TestMethod]
    public void ImplementsInterface_TypeIsInterface_ReturnsTrue()
    {
        var myInterface = GetSymbol("IMyInterface");

        Assert.IsTrue(myInterface.ImplementsInterface(myInterface));
    }

    [TestMethod]
    public void ImplementsInterface_NotImplemented_ReturnsFalse()
    {
        var intSymbol = GetSymbol("System.Int32");
        var myInterface = GetSymbol("IMyInterface");

        Assert.IsFalse(intSymbol.ImplementsInterface(myInterface));
    }

    [TestMethod]
    public void HasAttribute_SymbolWithAttribute_ReturnsTrue()
    {
        var attributedClass = GetSymbol("AttributedClass");
        var attributeSymbol = GetSymbol("MyAttribute");

        Assert.IsTrue(attributedClass.HasAttribute(attributeSymbol));
    }

    [TestMethod]
    public void HasAttribute_SymbolWithoutAttribute_ReturnsFalse()
    {
        var plainClass = GetSymbol("MyClass");
        var attributeSymbol = GetSymbol("MyAttribute");

        Assert.IsFalse(plainClass.HasAttribute(attributeSymbol));
    }

    [TestMethod]
    public void ImplementsInterface_TypeParameterWithConstraint_ReturnsTrue()
    {
        var genericClass = GetSymbol("GenericClass`1");
        var typeParam = genericClass.TypeParameters[0];
        var myInterface = GetSymbol("IMyInterface");

        Assert.IsTrue(typeParam.ImplementsInterface(myInterface));
    }

    [TestMethod]
    public void ImplementsInterface_TypeParameterTransitiveConstraint_ReturnsTrue()
    {
        var chainedGeneric = GetSymbol("ChainedGeneric`2");
        var typeParam1 = chainedGeneric.TypeParameters[0];
        var myInterface = GetSymbol("IMyInterface");

        Assert.IsTrue(typeParam1.ImplementsInterface(myInterface));
    }

    private static CSharpCompilation CreateCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using System;

            public class MyAttribute : Attribute { }

            public interface IMyInterface { }

            [My]
            public class AttributedClass { }

            public class MyClass : IMyInterface { }

            public class GenericClass<T> where T : IMyInterface { }

            public class ChainedGeneric<T1, T2> where T1 : T2 where T2 : IMyInterface { }
            """);

        var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        return CSharpCompilation.Create("TestAssembly", [syntaxTree], [mscorlib]);
    }

    private INamedTypeSymbol GetSymbol(string metadataName)
    {
        var symbol = _compilation.GetTypeByMetadataName(metadataName);
        Assert.IsNotNull(symbol, $"Symbol '{metadataName}' should exist in test compilation.");
        return symbol;
    }
}
