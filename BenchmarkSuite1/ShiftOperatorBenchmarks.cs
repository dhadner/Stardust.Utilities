using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Measures the cost of iterative shift operators (with MustBe normalization per bit)
/// versus normal single-operation shift operators on bit field structs.
/// Iterative shifts apply an OR mask after each 1-bit shift to enforce undefined-bits-must-be-ones.
/// Each benchmark performs 50M iterations internally to produce measurable, stable timings.
/// </summary>
[CPUUsageDiagnoser]
[MemoryDiagnoser(false)]
public class ShiftOperatorBenchmarks
{
    private const byte NORMALIZATION_OR_MASK = 0x89;
    private const int ITERATIONS = 50_000_000;

    private sbyte _value;

    [GlobalSetup]
    public void Setup()
    {
        _value = unchecked((sbyte)0xA5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static sbyte NormalizeValue(sbyte v) => (sbyte)((byte)v | NORMALIZATION_OR_MASK);

    // --- Normal (single-operation) shifts: no MustBe constraints ---

    [Benchmark(Baseline = true)]
    [Arguments(1)]
    [Arguments(4)]
    [Arguments(7)]
    public int NormalShiftLeft(int count)
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((sbyte)(v << count));
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(4)]
    [Arguments(7)]
    public int NormalShiftRight(int count)
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((sbyte)(v >> count));
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    // --- Iterative shifts: normalize MustBe constraints after each bit position ---

    [Benchmark]
    [Arguments(1)]
    [Arguments(4)]
    [Arguments(7)]
    public int IterativeShiftLeft(int count)
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sbyte current = NormalizeValue(v);
            for (int j = 0; j < count; j++)
                current = (sbyte)((byte)unchecked((sbyte)(current << 1)) | NORMALIZATION_OR_MASK);
            acc += current;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(4)]
    [Arguments(7)]
    public int IterativeShiftRight(int count)
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sbyte current = NormalizeValue(v);
            for (int j = 0; j < count; j++)
                current = (sbyte)((byte)unchecked((sbyte)(current >> 1)) | NORMALIZATION_OR_MASK);
            acc += current;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    // --- Construction: unconstrained vs constrained (AND+OR normalization) ---
    // Simulates the real generated constructor. Constrained adds (value & AND) | OR.

    [Benchmark]
    public int UnconstrainedConstruction()
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            byte raw = unchecked((byte)v);
            sbyte constructed = unchecked((sbyte)raw);
            acc += constructed;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    [Benchmark]
    public int ConstrainedConstruction()
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            byte raw = unchecked((byte)v);
            sbyte constructed = unchecked((sbyte)((raw & 0xFF) | NORMALIZATION_OR_MASK));
            acc += constructed;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    // --- Bitwise OR: unconstrained vs constrained (normalize after OR) ---

    [Benchmark]
    public int UnconstrainedBitwiseOr()
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            byte raw = unchecked((byte)((byte)v | 0x06));
            sbyte result = unchecked((sbyte)raw);
            acc += result;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }

    [Benchmark]
    public int ConstrainedBitwiseOr()
    {
        int acc = 0;
        sbyte v = _value;
        for (int i = 0; i < ITERATIONS; i++)
        {
            byte raw = unchecked((byte)((byte)v | 0x06));
            sbyte result = unchecked((sbyte)((raw & 0xFF) | NORMALIZATION_OR_MASK));
            acc += result;
            v = unchecked((sbyte)(v + 1));
        }
        return acc;
    }
}
