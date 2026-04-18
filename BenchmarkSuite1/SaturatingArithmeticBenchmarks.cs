using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Measures the overhead of saturating arithmetic (<see cref="Extensions.SaturatingAdd{T}"/>
/// and <see cref="Extensions.SaturatingSub{T}"/>) versus normal (wrapping) addition and
/// subtraction across all supported integer widths: 8, 16, 32, 64, and 128 bits, plus
/// big-endian and little-endian wrapper types.
///
/// Each benchmark performs 10M iterations internally to produce measurable, stable timings.
/// The accumulator is returned to prevent dead-code elimination.
/// </summary>
[MemoryDiagnoser(false)]
[DisassemblyDiagnoser(maxDepth: 2)]
public class SaturatingArithmeticBenchmarks
{
    private const int ITERATIONS = 10_000_000;

    // ── 8-bit ───────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public int Byte_Add()
    {
        int acc = 0;
        byte a = 200; byte b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((byte)(a + b));
            a = unchecked((byte)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Byte_SaturatingAdd()
    {
        int acc = 0;
        byte a = 200; byte b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingAdd(b);
            a = unchecked((byte)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Byte_Sub()
    {
        int acc = 0;
        byte a = 50; byte b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((byte)(a - b));
            a = unchecked((byte)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Byte_SaturatingSub()
    {
        int acc = 0;
        byte a = 50; byte b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingSub(b);
            a = unchecked((byte)(a + 1));
        }
        return acc;
    }

    // ── 16-bit ──────────────────────────────────────────────────

    [Benchmark]
    public int Short_Add()
    {
        int acc = 0;
        short a = 30000; short b = 10000;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((short)(a + b));
            a = unchecked((short)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Short_SaturatingAdd()
    {
        int acc = 0;
        short a = 30000; short b = 10000;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingAdd(b);
            a = unchecked((short)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Ushort_Add()
    {
        int acc = 0;
        ushort a = 60000; ushort b = 10000;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((ushort)(a + b));
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int Ushort_SaturatingAdd()
    {
        int acc = 0;
        ushort a = 60000; ushort b = 10000;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingAdd(b);
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    // ── 32-bit ──────────────────────────────────────────────────

    [Benchmark]
    public long Int_Add()
    {
        long acc = 0;
        int a = int.MaxValue - 100; int b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked(a + b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Int_SaturatingAdd()
    {
        long acc = 0;
        int a = int.MaxValue - 100; int b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingAdd(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Uint_Sub()
    {
        long acc = 0;
        uint a = 50; uint b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked(a - b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Uint_SaturatingSub()
    {
        long acc = 0;
        uint a = 50; uint b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingSub(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    // ── 64-bit ──────────────────────────────────────────────────

    [Benchmark]
    public long Long_Add()
    {
        long acc = 0;
        long a = long.MaxValue - 100; long b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked(a + b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Long_SaturatingAdd()
    {
        long acc = 0;
        long a = long.MaxValue - 100; long b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.SaturatingAdd(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Ulong_Sub()
    {
        long acc = 0;
        ulong a = 50; ulong b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((long)(a - b));
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Ulong_SaturatingSub()
    {
        long acc = 0;
        ulong a = 50; ulong b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)a.SaturatingSub(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    // ── 128-bit ─────────────────────────────────────────────────

    [Benchmark]
    public long Int128_Add()
    {
        long acc = 0;
        Int128 a = Int128.MaxValue - 100; Int128 b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a + b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Int128_SaturatingAdd()
    {
        long acc = 0;
        Int128 a = Int128.MaxValue - 100; Int128 b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)a.SaturatingAdd(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128_Sub()
    {
        long acc = 0;
        UInt128 a = 50; UInt128 b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a - b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128_SaturatingSub()
    {
        long acc = 0;
        UInt128 a = 50; UInt128 b = 100;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)a.SaturatingSub(b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    // ── Big-Endian (UInt32Be as representative) ─────────────────

    [Benchmark]
    public long UInt32Be_Add()
    {
        long acc = 0;
        UInt32Be a = uint.MaxValue - 100; UInt32Be b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a + b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Be_SaturatingAdd()
    {
        long acc = 0;
        UInt32Be a = uint.MaxValue - 100; UInt32Be b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)a.SaturatingAdd(b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    // ── Little-Endian (UInt32Le as representative) ──────────────

    [Benchmark]
    public long UInt32Le_Add()
    {
        long acc = 0;
        UInt32Le a = uint.MaxValue - 100; UInt32Le b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a + b);
            a = new UInt32Le((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Le_SaturatingAdd()
    {
        long acc = 0;
        UInt32Le a = uint.MaxValue - 100; UInt32Le b = 50;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)a.SaturatingAdd(b);
            a = new UInt32Le((uint)a + 1);
        }
        return acc;
    }
}
