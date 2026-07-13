# Personal C# Analyzers Collection

A collection of custom Roslyn analyzers focused on performance optimizations for modern .NET applications.

## Analyzers

### PERF0001: Expression misses ISpanFormattable implementation

Types should implement ISpanFormattable inside interpolated strings to prevent unnecessary runtime string allocations.

```csharp
// 🛑 Warns: MyCustomType does not implement ISpanFormattable
var obj = new MyCustomType();
var s = $"Value: {obj}";

// ✅ No Warning: int implements ISpanFormattable (formatted allocation-free)
int value = 42;
var s = $"Value: {value}";
```

### PERF0002: Add StructLayoutAttribute

Structs with multiple fields of different types should explicitly specify StructLayoutAttribute (e.g. LayoutKind.Auto) to optimize memory layout and eliminate padding.

```csharp
// 🛑 Warns: Mixed field alignment (int and double)
public struct MixedStruct
{
    public int X;
    public double Y;
}

// ✅ No Warning: All fields share the same alignment category
public struct RefStruct
{
    public string Name;
    public List<int> Items;
}
```

### PERF0003: Use MemoryMarshal.GetArrayDataReference instead of MemoryMarshal.GetReference

Calling MemoryMarshal.GetReference on an array span creates an unnecessary intermediate Span or ReadOnlySpan struct. Use MemoryMarshal.GetArrayDataReference directly on the array to eliminate overhead.

```csharp
// 🛑 Warns: Creates intermediate Span struct before getting data reference
ref byte r = ref MemoryMarshal.GetReference(array.AsSpan());

// ✅ No Warning: Directly gets array data reference without Span creation
ref byte r = ref MemoryMarshal.GetArrayDataReference(array);
```

### PERF0004: Redundant cast in Unsafe operation

The offset is already an unsigned 32-bit integer (uint) and does not emit sign-extension instructions. The cast is redundant.

```csharp
// 🛑 Warns (PERF0004): (nuint) cast on uint variable is redundant
uint uOffset = 42;
ref byte res = ref Unsafe.Add(ref ptr, (nuint)uOffset);

// ✅ Clean Code: pass uint directly
ref byte res = ref Unsafe.Add(ref ptr, uOffset);
```

### PERF0005: Cast signed offset to uint in Unsafe operations

Passing signed integers to Unsafe operations causes the JIT to emit sign-extension instructions (movsxd). Cast non-negative offsets to uint to zero-extend without overhead.

```csharp
// 🛑 Warns (PERF0005): int emits movsxd
ref byte res = ref Unsafe.Add(ref ptr, intOffset);

// 🛑 Warns (PERF0005): (nuint) cast on signed int still sign-extends and emits movsxd
ref byte res = ref Unsafe.Add(ref ptr, (nuint)intOffset);

// ✅ No Warning: cast to uint zero-extends for free without movsxd
ref byte res = ref Unsafe.Add(ref ptr, (uint)intOffset);
```

### PERF0006: Cast signed integer to uint before casting to native integer

Directly casting signed integers to nuint or nint emits sign-extension instructions (movsxd). Cast to uint first (e.g. (nuint)(uint)expr) to zero-extend without overhead.

```csharp
// 🛑 Warns (PERF0006): (nuint) cast on signed int emits movsxd
nuint value = (nuint)some_int;

// ✅ No Warning: cast to uint first zero-extends for free without movsxd
nuint value = (nuint)(uint)some_int;
```
