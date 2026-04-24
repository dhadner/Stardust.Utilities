```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.8246)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                 | Categories | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Allocated | Alloc Ratio |
|----------------------- |----------- |------------:|----------:|----------:|------:|--------:|---------:|----------:|------------:|
| Ours_Add               | Add        |    38.41 μs |  0.412 μs |  0.441 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Add         | Add        |    59.72 μs |  0.263 μs |  0.233 μs |  1.55 |    0.02 |        - |         - |          NA |
| MissingValues_Add      | Add        |    38.47 μs |  0.317 μs |  0.281 μs |  1.00 |    0.01 |        - |         - |          NA |
| BigInteger_Add         | Add        | 1,595.97 μs | 31.108 μs | 33.285 μs | 41.56 |    0.96 | 458.9844 | 2879640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Div               | Div        |   283.44 μs |  5.461 μs |  6.707 μs |  1.00 |    0.03 |        - |         - |          NA |
| Nethermind_Div         | Div        |   319.39 μs |  4.624 μs |  4.325 μs |  1.13 |    0.03 |        - |         - |          NA |
| MissingValues_Div      | Div        |   575.49 μs |  9.321 μs |  8.263 μs |  2.03 |    0.05 |        - |         - |          NA |
| BigInteger_Div         | Div        | 2,114.29 μs | 41.908 μs | 75.570 μs |  7.46 |    0.31 | 367.1875 | 2320000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Mod               | Mod        |   291.30 μs |  5.713 μs |  7.819 μs |  1.00 |    0.04 |        - |         - |          NA |
| Nethermind_Mod         | Mod        |   344.50 μs |  4.171 μs |  3.902 μs |  1.18 |    0.03 |        - |         - |          NA |
| MissingValues_Mod      | Mod        |   630.18 μs | 10.710 μs | 10.018 μs |  2.16 |    0.07 |        - |         - |          NA |
| BigInteger_Mod         | Mod        | 2,050.30 μs | 39.969 μs | 44.426 μs |  7.04 |    0.24 | 406.2500 | 2560000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Mul               | Mul        |    85.53 μs |  1.198 μs |  1.121 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Mul         | Mul        |   152.20 μs |  1.537 μs |  1.438 μs |  1.78 |    0.03 |        - |         - |          NA |
| MissingValues_Mul      | Mul        |   105.17 μs |  1.276 μs |  1.131 μs |  1.23 |    0.02 |        - |         - |          NA |
| BigInteger_Mul         | Mul        | 2,045.78 μs | 40.668 μs | 54.291 μs | 23.92 |    0.69 | 519.5313 | 3280000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Parse             | Parse      |   990.51 μs | 17.956 μs | 14.994 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Parse       | Parse      | 4,146.66 μs | 56.270 μs | 52.635 μs |  4.19 |    0.08 | 265.6250 | 1680000 B |          NA |
| MissingValues_Parse    | Parse      | 2,220.27 μs | 40.363 μs | 37.756 μs |  2.24 |    0.05 |        - |         - |          NA |
| BigInteger_Parse       | Parse      | 4,305.77 μs | 72.854 μs | 74.816 μs |  4.35 |    0.10 | 273.4375 | 1759640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Sub               | Sub        |    38.90 μs |  0.718 μs |  0.706 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Sub         | Sub        |    63.44 μs |  0.609 μs |  0.569 μs |  1.63 |    0.03 |        - |         - |          NA |
| MissingValues_Sub      | Sub        |    38.18 μs |  0.716 μs |  0.796 μs |  0.98 |    0.03 |        - |         - |          NA |
| BigInteger_Sub         | Sub        | 1,690.81 μs | 18.751 μs | 28.065 μs | 43.48 |    1.04 | 546.8750 | 3439640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_ToString          | ToString   | 1,244.24 μs |  9.772 μs |  9.141 μs |  1.00 |    0.01 | 292.9688 | 1840000 B |        1.00 |
| Nethermind_ToString    | ToString   | 2,102.07 μs | 41.954 μs | 84.749 μs |  1.69 |    0.07 | 380.8594 | 2400000 B |        1.30 |
| MissingValues_ToString | ToString   | 1,476.95 μs | 20.078 μs | 18.781 μs |  1.19 |    0.02 | 292.9688 | 1840000 B |        1.00 |
| BigInteger_ToString    | ToString   | 2,718.38 μs | 53.248 μs | 54.682 μs |  2.18 |    0.05 | 468.7500 | 2960000 B |        1.61 |
