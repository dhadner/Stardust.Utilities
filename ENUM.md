# EnhancedEnum Source Generator

Discriminated unions (sum types) for C#. Similar to Rust enums with associated data.

## Why Use This?

In C#, a regular `enum` can only hold a single integer value. But what if each enum case needs to carry *different* data? For example:

- `Step` — no data needed
- `SetBreakpoint` — needs an address and hit count
- `Evaluate` — needs an expression string

`EnhancedEnum` lets each variant carry its own strongly-typed payload, with exhaustive pattern matching enforced by the compiler.

## Quick Start

```csharp
using Stardust.Utilities;

[EnhancedEnum]
public partial struct DebugCommand
{
    [EnumKind]
    public enum Kind
    {
        [EnumValue(typeof((uint address, int hitCount)))]
        SetBreakpoint,
        
        [EnumValue(typeof(string))]
        Evaluate,
        
        [EnumValue]  // No payload
        Step,
        
        [EnumValue]
        Continue,
    }
}
```

## Pattern Matching with Switch

### Using the Match Method (Recommended)

The `Match` method provides exhaustive, compiler-enforced pattern matching:

```csharp
string result = command.Match(
    SetBreakpoint: bp => $"Break at 0x{bp.address:X8} (hits: {bp.hitCount})",
    Evaluate: expr => $"Eval: {expr}",
    Step: () => "Single step",
    @Continue: () => "Continue execution"
);
```

If you add a new variant to the enum, the compiler will error until you handle it in all `Match` calls.

### Using C# Switch Expressions with Is Properties

```csharp
string result = command.Tag switch
{
    DebugCommand.Kind.SetBreakpoint when command.TryGetSetBreakpoint(out var bp) 
        => $"Break at 0x{bp.address:X8}",
    DebugCommand.Kind.Evaluate when command.TryGetEvaluate(out var expr) 
        => $"Eval: {expr}",
    DebugCommand.Kind.Step => "Step",
    DebugCommand.Kind.Continue => "Continue",
    _ => throw new InvalidOperationException()
};
```

### Using Traditional Switch Statements

```csharp
switch (command.Tag)
{
    case DebugCommand.Kind.SetBreakpoint:
        if (command.TryGetSetBreakpoint(out var bp))
            Console.WriteLine($"Breakpoint at 0x{bp.address:X8}, hits={bp.hitCount}");
        break;
        
    case DebugCommand.Kind.Evaluate:
        if (command.TryGetEvaluate(out var expr))
            Console.WriteLine($"Evaluating: {expr}");
        break;
        
    case DebugCommand.Kind.Step:
        Console.WriteLine("Stepping");
        break;
        
    case DebugCommand.Kind.Continue:
        Console.WriteLine("Continuing");
        break;
}
```

### Quick Checks with Is Properties

```csharp
if (command.IsStep)
{
    // Handle step - no payload to extract
}

if (command.IsSetBreakpoint)
{
    // We know it's a breakpoint, extract the data
    command.TryGetSetBreakpoint(out var bp);
    ProcessBreakpoint(bp.address, bp.hitCount);
}
```

## Payload Types

### Value Type Payloads (Tuples)

```csharp
[EnumValue(typeof((uint address, int hitCount)))]
SetBreakpoint,

// Usage
var cmd = DebugCommand.SetBreakpoint((0x00401000u, 3));
if (cmd.TryGetSetBreakpoint(out var bp))
{
    Console.WriteLine($"Address: 0x{bp.address:X8}, Hits: {bp.hitCount}");
}
```

### Reference Type Payloads

```csharp
[EnumValue(typeof(string))]
Evaluate,

[EnumValue(typeof(Breakpoint))]  // Custom class
SetBreakpointEx,

// Usage
var cmd = DebugCommand.Evaluate("PC + 4");
if (cmd.TryGetEvaluate(out string expr))
{
    Console.WriteLine($"Expression: {expr}");
}
```

### No Payload (Unit Variants)

```csharp
[EnumValue]  // No typeof() means no payload
Step,

// Usage
var cmd = DebugCommand.Step();
if (cmd.IsStep)
{
    SingleStep();
}
```

## Generated API

For each `[EnhancedEnum]` struct, the generator creates:

```csharp
public readonly partial struct DebugCommand : IEquatable<DebugCommand>
{
    // Tag property (which variant)
    public Kind Tag { get; }
    
    // Factory methods
    public static DebugCommand SetBreakpoint((uint, int) value);
    public static DebugCommand Evaluate(string value);
    public static DebugCommand Step();
    public static DebugCommand Continue();
    
    // Is properties
    public bool IsSetBreakpoint { get; }
    public bool IsEvaluate { get; }
    public bool IsStep { get; }
    public bool IsContinue { get; }
    
    // TryGet methods
    public bool TryGetSetBreakpoint(out (uint, int) value);
    public bool TryGetEvaluate(out string value);
    
    // Exhaustive Match (returns TResult)
    public TResult Match<TResult>(
        Func<(uint, int), TResult> SetBreakpoint,
        Func<string, TResult> Evaluate,
        Func<TResult> Step,
        Func<TResult> @Continue);
    
    // Exhaustive Match (void, for side effects)
    public void Match(
        Action<(uint, int)> SetBreakpoint,
        Action<string> Evaluate,
        Action Step,
        Action @Continue);
    
    // Equality
    public bool Equals(DebugCommand other);
    public static bool operator ==(DebugCommand left, DebugCommand right);
    public static bool operator !=(DebugCommand left, DebugCommand right);
}
```

## Real-World Example: Debugger Commands

```csharp
[EnhancedEnum]
public partial struct DebuggerCommand
{
    [EnumKind]
    public enum Kind
    {
        // Memory operations
        [EnumValue(typeof((uint address, uint length)))]
        ReadMemory,
        
        [EnumValue(typeof((uint address, byte[] data)))]
        WriteMemory,
        
        // Breakpoint operations
        [EnumValue(typeof(uint))]  // Just the address
        SetBreakpoint,
        
        [EnumValue(typeof(uint))]
        ClearBreakpoint,
        
        // Execution control
        [EnumValue]
        Step,
        
        [EnumValue]
        StepOver,
        
        [EnumValue]
        Continue,
        
        [EnumValue]
        Pause,
        
        // Register operations
        [EnumValue(typeof(string))]  // Register name
        ReadRegister,
        
        [EnumValue(typeof((string name, uint value)))]
        WriteRegister,
    }
}

// Command processor using Match
public void ProcessCommand(DebuggerCommand cmd)
{
    cmd.Match(
        ReadMemory: args => {
            var data = Machine.Memory.Read(args.address, args.length);
            SendResponse(data);
        },
        WriteMemory: args => {
            Machine.Memory.Write(args.address, args.data);
            SendAck();
        },
        SetBreakpoint: addr => {
            Breakpoints.Add(addr);
            SendAck();
        },
        ClearBreakpoint: addr => {
            Breakpoints.Remove(addr);
            SendAck();
        },
        Step: () => Machine.Step(),
        StepOver: () => Machine.StepOver(),
        Continue: () => Machine.Run(),
        Pause: () => Machine.Pause(),
        ReadRegister: name => {
            var value = Machine.CPU.GetRegister(name);
            SendResponse(value);
        },
        WriteRegister: args => {
            Machine.CPU.SetRegister(args.name, args.value);
            SendAck();
        }
    );
}
```

## Performance

The source generator creates a **zero-allocation struct** with:
- Inline payload storage (no boxing for value types)
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on all methods
- Compile-time constants for tag values

Reference type payloads are stored as references (not boxed).

## Comparison with Alternatives

| Feature | EnhancedEnum (Generator) | C# Records | Rust Enums |
|---------|-------------------------|------------|------------|
| Exhaustive matching | ? Compiler-enforced | ? Manual | ? Compiler-enforced |
| Payload per variant | ? Different types | ? Via inheritance | ? Different types |
| Zero allocation | ? Struct-based | ? Heap allocated | ? Stack/inline |
| Pattern matching | ? Match method | ? switch expressions | ? match |
