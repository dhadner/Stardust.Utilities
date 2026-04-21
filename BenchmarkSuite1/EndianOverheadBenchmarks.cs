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

    // ── Bitwise OR ──────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_Or()
    {
        int acc = 0;
        ushort a = 0xABCD; ushort b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a | b);
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_Or()
    {
        int acc = 0;
        UInt16Be a = 0xABCD; UInt16Be b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a | b);
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_Or()
    {
        int acc = 0;
        UInt16Le a = 0xABCD; UInt16Le b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a | b);
            a = new UInt16Le((ushort)((ushort)a + 1));
        }
        return acc;
    }

    // ── Bitwise XOR ─────────────────────────────────────────────

    [Benchmark]
    public int Native_UShort_Xor()
    {
        int acc = 0;
        ushort a = 0xABCD; ushort b = 0x0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a ^ b);
            a = unchecked((ushort)(a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Be_Xor()
    {
        int acc = 0;
        UInt16Be a = 0xABCD; UInt16Be b = new UInt16Be(0x0F0F);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a ^ b);
            a = unchecked((UInt16Be)(ushort)((ushort)a + 1));
        }
        return acc;
    }

    [Benchmark]
    public int UInt16Le_Xor()
    {
        int acc = 0;
        UInt16Le a = 0xABCD; UInt16Le b = new UInt16Le(0x0F0F);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (ushort)(a ^ b);
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

    [Benchmark]
    public long Native_UInt_Or()
    {
        long acc = 0;
        uint a = 0xABCDEF01; uint b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a | b;
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Be_Or()
    {
        long acc = 0;
        UInt32Be a = 0xABCDEF01; UInt32Be b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a | b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Le_Or()
    {
        long acc = 0;
        UInt32Le a = 0xABCDEF01; UInt32Le b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a | b);
            a = new UInt32Le((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt_Xor()
    {
        long acc = 0;
        uint a = 0xABCDEF01; uint b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += a ^ b;
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Be_Xor()
    {
        long acc = 0;
        UInt32Be a = 0xABCDEF01; UInt32Be b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a ^ b);
            a = (UInt32Be)((uint)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt32Le_Xor()
    {
        long acc = 0;
        UInt32Le a = 0xABCDEF01; UInt32Le b = 0x0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (uint)(a ^ b);
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

    [Benchmark]
    public long Native_ULong_And()
    {
        long acc = 0;
        ulong a = 0xABCDEF0123456789; ulong b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a & b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Be_And()
    {
        long acc = 0;
        UInt64Be a = 0xABCDEF0123456789; UInt64Be b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a & b);
            a = (UInt64Be)((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Le_And()
    {
        long acc = 0;
        UInt64Le a = 0xABCDEF0123456789; UInt64Le b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a & b);
            a = new UInt64Le((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_ULong_Or()
    {
        long acc = 0;
        ulong a = 0xABCDEF0123456789; ulong b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a | b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Be_Or()
    {
        long acc = 0;
        UInt64Be a = 0xABCDEF0123456789; UInt64Be b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a | b);
            a = (UInt64Be)((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Le_Or()
    {
        long acc = 0;
        UInt64Le a = 0xABCDEF0123456789; UInt64Le b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a | b);
            a = new UInt64Le((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_ULong_Xor()
    {
        long acc = 0;
        ulong a = 0xABCDEF0123456789; ulong b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a ^ b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Be_Xor()
    {
        long acc = 0;
        UInt64Be a = 0xABCDEF0123456789; UInt64Be b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a ^ b);
            a = (UInt64Be)((ulong)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt64Le_Xor()
    {
        long acc = 0;
        UInt64Le a = 0xABCDEF0123456789; UInt64Le b = 0x0F0F0F0F0F0F0F0F;
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a ^ b);
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

    [Benchmark]
    public long Native_UInt128_And()
    {
        long acc = 0;
        UInt128 a = new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210);
        UInt128 b = new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a & b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Be_And()
    {
        long acc = 0;
        UInt128Be a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Be b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a & b);
            a = new UInt128Be((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Le_And()
    {
        long acc = 0;
        UInt128Le a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Le b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a & b);
            a = new UInt128Le((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt128_Or()
    {
        long acc = 0;
        UInt128 a = new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210);
        UInt128 b = new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a | b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Be_Or()
    {
        long acc = 0;
        UInt128Be a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Be b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a | b);
            a = new UInt128Be((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Le_Or()
    {
        long acc = 0;
        UInt128Le a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Le b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a | b);
            a = new UInt128Le((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt128_Xor()
    {
        long acc = 0;
        UInt128 a = new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210);
        UInt128 b = new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(a ^ b);
            a = unchecked(a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Be_Xor()
    {
        long acc = 0;
        UInt128Be a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Be b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a ^ b);
            a = new UInt128Be((UInt128)a + 1);
        }
        return acc;
    }

    [Benchmark]
    public long UInt128Le_Xor()
    {
        long acc = 0;
        UInt128Le a = new(new UInt128(0xABCDEF0123456789, 0xFEDCBA9876543210));
        UInt128Le b = new(new UInt128(0x0F0F0F0F0F0F0F0F, 0x0F0F0F0F0F0F0F0F));
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(UInt128)(a ^ b);
            a = new UInt128Le((UInt128)a + 1);
        }
        return acc;
    }

    // ════════════════════════════════════════════════════════════
    //  256-bit
    // ════════════════════════════════════════════════════════════

    [Benchmark]
    public long Native_UInt256_Add()
    {
        long acc = 0;
        UInt256 a = new(0UL, 0UL, 0x123456789ABCDEF0UL, 0x0011001100110011UL);
        UInt256 b = new(0UL, 0UL, 0UL, 0x0011UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a + b);
            a = unchecked(a + UInt256.One);
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Be_Add()
    {
        long acc = 0;
        UInt256Be a = new UInt256(0UL, 0UL, 0x123456789ABCDEF0UL, 0x0011001100110011UL);
        UInt256Be b = new UInt256(0UL, 0UL, 0UL, 0x0011UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a + b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Le_Add()
    {
        long acc = 0;
        UInt256Le a = new UInt256(0UL, 0UL, 0x123456789ABCDEF0UL, 0x0011001100110011UL);
        UInt256Le b = new UInt256(0UL, 0UL, 0UL, 0x0011UL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a + b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt256_And()
    {
        long acc = 0;
        UInt256 a = new(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256 b = new(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a & b);
            a = unchecked(a + UInt256.One);
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Be_And()
    {
        long acc = 0;
        UInt256Be a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Be b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a & b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Le_And()
    {
        long acc = 0;
        UInt256Le a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Le b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a & b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt256_Or()
    {
        long acc = 0;
        UInt256 a = new(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256 b = new(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a | b);
            a = unchecked(a + UInt256.One);
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Be_Or()
    {
        long acc = 0;
        UInt256Be a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Be b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a | b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Le_Or()
    {
        long acc = 0;
        UInt256Le a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Le b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a | b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long Native_UInt256_Xor()
    {
        long acc = 0;
        UInt256 a = new(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256 b = new(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(a ^ b);
            a = unchecked(a + UInt256.One);
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Be_Xor()
    {
        long acc = 0;
        UInt256Be a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Be b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a ^ b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    [Benchmark]
    public long UInt256Le_Xor()
    {
        long acc = 0;
        UInt256Le a = new UInt256(0xABCDEF0123456789UL, 0xFEDCBA9876543210UL, 0xABCDEF0123456789UL, 0xFEDCBA9876543210UL);
        UInt256Le b = new UInt256(0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL, 0x0F0F0F0F0F0F0F0FUL);
        for (int i = 0; i < ITERATIONS; i++)
        {
            acc += (long)(ulong)(UInt256)(a ^ b);
            a = (UInt256)a + UInt256.One;
        }
        return acc;
    }

    /// <summary>
    /// Measures the round-trip cost of reading 32 bytes as big-endian, converting to
    /// host-native UInt256, and writing back — the typical I/O boundary pattern.
    /// </summary>
    [Benchmark]
    public long UInt256Be_RoundTrip()
    {
        long acc = 0;
        UInt256 native = new(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL, 0x1112131415161718UL, 0x191A1B1C1D1E1F20UL);
        Span<byte> buf = stackalloc byte[32];
        for (int i = 0; i < ITERATIONS / 10; i++)
        {
            UInt256Be be = native;           // UInt256 → UInt256Be (4 BSWAPs)
            be.WriteTo(buf);                 // 32-byte span copy
            UInt256Be back = new UInt256Be(buf); // 32-byte span copy
            UInt256 result = back;           // UInt256Be → UInt256 (4 BSWAPs)
            acc += (long)(ulong)result;
            native = unchecked(native + UInt256.One);
        }
        return acc;
    }

    /// <summary>
    /// Measures the round-trip cost of reading 32 bytes as little-endian, converting to
    /// host-native UInt256, and writing back.  On LE hardware the conversions are
    /// near-zero cost; the dominant cost is the two 32-byte memory copies.
    /// </summary>
    [Benchmark]
    public long UInt256Le_RoundTrip()
    {
        long acc = 0;
        UInt256 native = new(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL, 0x1112131415161718UL, 0x191A1B1C1D1E1F20UL);
        Span<byte> buf = stackalloc byte[32];
        for (int i = 0; i < ITERATIONS / 10; i++)
        {
            UInt256Le le = native;           // UInt256 → UInt256Le (4 plain writes, LE-native)
            le.WriteTo(buf);                 // 32-byte span copy
            UInt256Le back = new UInt256Le(buf); // 32-byte span copy
            UInt256 result = back;           // UInt256Le → UInt256 (4 plain reads, LE-native)
            acc += (long)(ulong)result;
            native = unchecked(native + UInt256.One);
        }
        return acc;
    }
}
