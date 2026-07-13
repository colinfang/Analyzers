using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

// Modified from https://github.com/meziantou/Meziantou.Analyzer/blob/main/src/Meziantou.Analyzer/Rules/UseStructLayoutAttributeAnalyzer.cs

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStructLayoutAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PERF0002";
    private const string Title = "Add StructLayoutAttribute";
    private const string MessageFormat = "Add StructLayoutAttribute to struct '{0}'";
    private const string Description = "Structs with multiple fields of different types should explicitly specify StructLayoutAttribute (e.g. LayoutKind.Auto) to optimize memory layout and eliminate padding.";
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsArray = [Rule];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsArray;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
            if (attributeType is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(symbolContext => Analyze(symbolContext, attributeType), SymbolKind.NamedType);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol attributeType)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        if (symbol.TypeKind != TypeKind.Struct || symbol.IsRefLikeType || symbol.HasAttribute(attributeType))
        {
            return;
        }

        ITypeSymbol? firstFieldType = null;
        int? firstFieldCategory = null;
        var hasDifferentFieldCategories = false;
        var instanceFieldCount = 0;

        foreach (var member in symbol.GetMembers())
        {
            if (member is not IFieldSymbol field)
            {
                continue;
            }

            if (field.IsConst || field.IsStatic)
            {
                continue;
            }

            var category = GetAlignmentCategory(field.Type);

            if (firstFieldType is null)
            {
                firstFieldType = field.Type;
                firstFieldCategory = category;
            }
            else
            {
                var isSameType = SymbolEqualityComparer.Default.Equals(firstFieldType, field.Type);
                var isSameCategory = firstFieldCategory != -1 && firstFieldCategory == category;

                if (!isSameType && !isSameCategory)
                {
                    hasDifferentFieldCategories = true;
                }
            }

            instanceFieldCount++;
        }

        if (instanceFieldCount > 1 && hasDifferentFieldCategories)
        {
            var location = symbol.Locations.Length > 0 ? symbol.Locations[0] : Location.None;
            var diagnostic = Diagnostic.Create(Rule, location, symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static int GetAlignmentCategory(ITypeSymbol type)
    {
        type = type.UnwrapNullable();

        if (type.IsReferenceType || type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)
        {
            return 8;
        }

        if (type is ITypeParameterSymbol typeParam)
        {
            return typeParam.HasReferenceTypeConstraint ? 8 : -1;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol)
        {
            type = enumSymbol.EnumUnderlyingType ?? type;
        }

        var category = type.SpecialType switch
        {
            SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_IntPtr or SpecialType.System_UIntPtr => 8,
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single or SpecialType.System_Char => 4,
            SpecialType.System_Int16 or SpecialType.System_UInt16 => 2,
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Boolean => 1,
            _ => -1
        };

        if (category != -1)
        {
            return category;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Struct } structSymbol && !structSymbol.IsRefLikeType)
        {
            var maxCategory = 1;
            var hasFields = false;

            foreach (var member in structSymbol.GetMembers())
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false } field)
                {
                    hasFields = true;
                    var fieldCat = GetAlignmentCategory(field.Type);
                    if (fieldCat == -1)
                    {
                        return -1;
                    }
                    if (fieldCat > maxCategory)
                    {
                        maxCategory = fieldCat;
                    }
                }
            }

            return hasFields ? maxCategory : 1;
        }

        return -1;
    }
}
