using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.VSDiagnostics;

namespace Stardust.Utilities.Benchmarks;
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[CPUUsageDiagnoser]
public class Int256BatchedOpsBenchmarks
{
    private const int BATCH_SIZE = 1024;
    private UInt256[] _oursA = null!;
    private UInt256[] _oursB = null!;
    private UInt256[] _oursR = null!;
    private Nethermind.Int256.UInt256[] _nethA = null!;
    private Nethermind.Int256.UInt256[] _nethB = null!;
    private Nethermind.Int256.UInt256[] _nethR = null!;
    private MissingValues.UInt256[] _mvA = null!;
    private MissingValues.UInt256[] _mvB = null!;
    private MissingValues.UInt256[] _mvR = null!;
    [GlobalSetup]
    public void Setup()
    {
        _oursA = new UInt256[BATCH_SIZE];
        _oursB = new UInt256[BATCH_SIZE];
        _oursR = new UInt256[BATCH_SIZE];
        _nethA = new Nethermind.Int256.UInt256[BATCH_SIZE];
        _nethB = new Nethermind.Int256.UInt256[BATCH_SIZE];
        _nethR = new Nethermind.Int256.UInt256[BATCH_SIZE];
        _mvA = new MissingValues.UInt256[BATCH_SIZE];
        _mvB = new MissingValues.UInt256[BATCH_SIZE];
        _mvR = new MissingValues.UInt256[BATCH_SIZE];
        ulong s0 = 0x0123456789ABCDEFUL, s1 = 0xFEDCBA9876543210UL, s2 = 0xAABBCCDDEEFF0011UL, s3 = 0x1122334455667788UL;
        for (int i = 0; i < BATCH_SIZE; i++)
        {
            s0 = unchecked(s0 * 6364136223846793005UL + 1442695040888963407UL);
            s1 = unchecked(s1 * 6364136223846793005UL + 1442695040888963407UL);
            s2 = unchecked(s2 * 6364136223846793005UL + 1442695040888963407UL);
            s3 = unchecked(s3 * 6364136223846793005UL + 1442695040888963407UL);
            ulong t0 = s0, t1 = s1, t2 = s2, t3 = s3;
            s0 = unchecked(s0 * 6364136223846793005UL + 1442695040888963407UL);
            s1 = unchecked(s1 * 6364136223846793005UL + 1442695040888963407UL);
            s2 = unchecked(s2 * 6364136223846793005UL + 1442695040888963407UL);
            s3 = unchecked(s3 * 6364136223846793005UL + 1442695040888963407UL);
            ulong u0 = s0, u1 = s1, u2 = s2, u3 = s3;
            _oursA[i] = new UInt256(t3, t2, t1, t0);
            _oursB[i] = new UInt256(u3, u2, u1, u0);
            _nethA[i] = new Nethermind.Int256.UInt256(t0, t1, t2, t3);
            _nethB[i] = new Nethermind.Int256.UInt256(u0, u1, u2, u3);
            _mvA[i] = new MissingValues.UInt256(new UInt128(t3, t2), new UInt128(t1, t0));
            _mvB[i] = new MissingValues.UInt256(new UInt128(u3, u2), new UInt128(u1, u0));
        }
    }

    [BenchmarkCategory("BatchAdd"), Benchmark(Baseline = true)]
    public UInt256 Ours_Batched_Add()
    {
        var a = _oursA;
        var b = _oursB;
        var r = _oursR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] + b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchAdd"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Batched_Add()
    {
        var a = _nethA;
        var b = _nethB;
        var r = _nethR;
        for (int i = 0; i < BATCH_SIZE; i++)
        {
            Nethermind.Int256.UInt256.Add(a[i], b[i], out r[i]);
        }

        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchAdd"), Benchmark]
    public MissingValues.UInt256 MissingValues_Batched_Add()
    {
        var a = _mvA;
        var b = _mvB;
        var r = _mvR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] + b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchSub"), Benchmark(Baseline = true)]
    public UInt256 Ours_Batched_Sub()
    {
        var a = _oursA;
        var b = _oursB;
        var r = _oursR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] - b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchSub"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Batched_Sub()
    {
        var a = _nethA;
        var b = _nethB;
        var r = _nethR;
        for (int i = 0; i < BATCH_SIZE; i++)
        {
            Nethermind.Int256.UInt256.Subtract(a[i], b[i], out r[i]);
        }

        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchSub"), Benchmark]
    public MissingValues.UInt256 MissingValues_Batched_Sub()
    {
        var a = _mvA;
        var b = _mvB;
        var r = _mvR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] - b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchMul"), Benchmark(Baseline = true)]
    public UInt256 Ours_Batched_Mul()
    {
        var a = _oursA;
        var b = _oursB;
        var r = _oursR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] * b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchMul"), Benchmark]
    public Nethermind.Int256.UInt256 Nethermind_Batched_Mul()
    {
        var a = _nethA;
        var b = _nethB;
        var r = _nethR;
        for (int i = 0; i < BATCH_SIZE; i++)
        {
            Nethermind.Int256.UInt256.Multiply(a[i], b[i], out r[i]);
        }

        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchMul"), Benchmark]
    public MissingValues.UInt256 MissingValues_Batched_Mul()
    {
        var a = _mvA;
        var b = _mvB;
        var r = _mvR;
        for (int i = 0; i < BATCH_SIZE; i++)
            r[i] = a[i] * b[i];
        return r[BATCH_SIZE - 1];
    }

    [BenchmarkCategory("BatchMul"), Benchmark]
    public UInt256 Ours_Batched_Mul_Vectorized()
    {
        UInt256Vector.Multiply(_oursA, _oursB, _oursR);
        return _oursR[BATCH_SIZE - 1];
    }
}