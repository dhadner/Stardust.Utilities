```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.8246)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MEHJPP : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=5  WarmupCount=1  

```
| Method           | Mean      | Error     | StdDev    | Allocated |
|----------------- |----------:|----------:|----------:|----------:|
| UInt16Be_Add     | 16.546 ms | 0.4879 ms | 0.1267 ms |         - |
| UInt16Le_Add     |  7.848 ms | 0.4395 ms | 0.0680 ms |         - |
| UInt16Be_Sub     | 16.249 ms | 0.6262 ms | 0.0969 ms |         - |
| UInt16Le_Sub     |  7.802 ms | 0.2429 ms | 0.0376 ms |         - |
| UInt16Be_And     | 16.414 ms | 0.2987 ms | 0.0776 ms |         - |
| UInt16Le_And     |  7.821 ms | 0.2568 ms | 0.0397 ms |         - |
| UInt16Be_Compare | 14.579 ms | 0.5808 ms | 0.1508 ms |         - |
| UInt16Le_Compare |  8.190 ms | 0.1388 ms | 0.0360 ms |         - |
| UInt16Be_HiLo    | 20.022 ms | 0.2775 ms | 0.0721 ms |         - |
| UInt16Le_HiLo    |  6.117 ms | 0.1159 ms | 0.0179 ms |         - |
