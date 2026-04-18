using BenchmarkDotNet.Attributes;

namespace Stardust.Utilities.Benchmarks;

/// <summary>
/// Measures the overhead of big-endian and little-endian wrapper types versus
/// native C# primitives for common operations: addition, subtraction, bitwise
/// AND/OR/XOR, comparison, and Hi/Lo decomposition.
///
/// Each benchmark performs 10M iterations internally to produce measurable,
/// stable timings. The accumulator is returned to prevent dead-code elimination.
/// </summary>
[MemoryDiagnoser(false)]
public class EndianOverheadBenchmarks
{
    private const int ITERATIONS = 10_000_000;

    // ════════════════════════════════════════════════════════════
    //  16-bit
    // ════════════════════════════════════════════════════════════

    // ── Addition ────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public int Native_UShort_Add()
    {
        int acc = 0;
        ushort a = 0x1234; ushort b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((ushort)(a + b));
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_Add()
    {
        int acc = 0;
        UInt16Be a = 0x1234; UInt16Be b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a + b);
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_Add()
    {
        int acc = 0;
        UInt16Le a = 0x1234; UInt16Le b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a + b);
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ── Subtraction ─────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_Sub()
    {
        int acc = 0;
        ushort a = 0xFFFF; ushort b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((ushort)(a - b));
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_Sub()
    {
        int acc = 0;
        UInt16Be a = 0xFFFF; UInt16Be b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a - b);
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_Sub()
    {
        int acc = 0;
        UInt16Le a = 0xFFFF; UInt16Le b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a - b);
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ── Bitwise AND ─────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_And()
    {
        int acc = 0;
        ushort a = 0xABCD; ushort b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a & b);
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_And()
    {
        int acc = 0;
        UInt16Be a = 0xABCD; UInt16Be b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a & b);
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_And()
    {
        int acc = 0;
        UInt16Le a = 0xABCD; UInt16Le b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a & b);
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ── Comparison ──────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_Compare()
    {
        int acc = 0;
        ushort a = 0x1234; ushort b = 0x5678;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a < b ? 1 : 0;
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_Compare()
    {
        int acc = 0;
        UInt16Be a = 0x1234; UInt16Be b = 0x5678;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a < b ? 1 : 0;
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_Compare()
    {
        int acc = 0;
        UInt16Le a = 0x1234; UInt16Le b = 0x5678;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a < b ? 1 : 0;
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ── Hi/Lo ───────────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_HiLo()
    {
        int acc = 0;
        ushort a = 0xABCD;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.Hi() + a.Lo();
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_HiLo()
    {
        int acc = 0;
        UInt16Be a = 0xABCD;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.Hi() + a.Lo();
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_HiLo()
    {
        int acc = 0;
        UInt16Le a = 0xABCD;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a.Hi() + a.Lo();
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  32-bit
    // ════════════════════════════════════════════════════════════

    [Benchmark]
    public long Native_UInt_Add()
    {
        long acc = 0;
        uint a = 0x12345678; uint b = 0x00110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked(a + b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Be_Add()
    {
        long acc = 0;
        UInt32Be a = 0x12345678; UInt32Be b = 0x00110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a + b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Le_Add()
    {
        long acc = 0;
        UInt32Le a = 0x12345678; UInt32Le b = 0x00110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a + b);
            a = new UInt32Le((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt_And()
    {
        long acc = 0;
        uint a = 0xABCDEF01; uint b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a & b;
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Be_And()
    {
        long acc = 0;
        UInt32Be a = 0xABCDEF01; UInt32Be b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a & b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Le_And()
    {
        long acc = 0;
        UInt32Le a = 0xABCDEF01; UInt32Le b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a & b);
            a = new UInt32Le((uint)a + 1);
        }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  64-bit
    // ════════════════════════════════════════════════════════════

    [Benchmark]
    public long Native_ULong_Add()
    {
        long acc = 0;
        ulong a = 0x123456789ABCDEF0; ulong b = 0x0011001100110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += unchecked((long)(a + b));
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Be_Add()
    {
        long acc = 0;
        UInt64Be a = 0x123456789ABCDEF0; UInt64Be b = 0x0011001100110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a + b);
            a = (UInt64Be)((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Le_Add()
    {
        long acc = 0;
        UInt64Le a = 0x123456789ABCDEF0; UInt64Le b = 0x0011001100110011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a + b);
            a = new UInt64Le((ulong)a + 1);
        }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  128-bit
    // ════════════════════════════════════════════════════════════

    [Benchmark]
    public long Native_UInt128_Add()
    {
        long acc = 0;
        UInt128 a = (UInt128)0x123456789ABCDEF0; UInt128 b = 0x0011;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a + b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Be_Add()
    {
        long acc = 0;
        UInt128Be a = new((UInt128)0x123456789ABCDEF0); UInt128Be b = new((UInt128)0x0011);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a + b);
            a = new UInt128Be((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Le_Add()
    {
        long acc = 0;
        UInt128Le a = new((UInt128)0x123456789ABCDEF0); UInt128Le b = new((UInt128)0x0011);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a + b);
            a = new UInt128Le((UInt128)a + 1);
        }
        return acc;
    }
}
