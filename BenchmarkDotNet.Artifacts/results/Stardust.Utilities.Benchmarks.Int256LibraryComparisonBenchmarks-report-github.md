```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.8246)
11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.6 (10.0.626.17701), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                 | Categories | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Allocated | Alloc Ratio |
|----------------------- |----------- |------------:|----------:|----------:|------:|--------:|---------:|----------:|------------:|
| Ours_Add               | Add        |    40.96 μs |  0.814 μs |  1.681 μs |  1.00 |    0.06 |        - |         - |          NA |
| Nethermind_Add         | Add        |    60.83 μs |  1.164 μs |  1.089 μs |  1.49 |    0.06 |        - |         - |          NA |
| MissingValues_Add      | Add        |    41.04 μs |  0.741 μs |  1.791 μs |  1.00 |    0.06 |        - |         - |          NA |
| BigInteger_Add         | Add        | 1,572.85 μs | 31.306 μs | 70.663 μs | 38.47 |    2.28 | 458.9844 | 2879640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Div               | Div        |   281.34 μs |  3.727 μs |  3.303 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Div         | Div        |   319.65 μs |  5.060 μs |  4.486 μs |  1.14 |    0.02 |        - |         - |          NA |
| MissingValues_Div      | Div        |   583.96 μs |  8.998 μs |  7.976 μs |  2.08 |    0.04 |        - |         - |          NA |
| BigInteger_Div         | Div        | 2,069.04 μs | 41.316 μs | 38.647 μs |  7.36 |    0.16 | 367.1875 | 2320000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Mod               | Mod        |   286.26 μs |  3.301 μs |  2.756 μs |  1.00 |    0.01 |        - |         - |          NA |
| Nethermind_Mod         | Mod        |   334.84 μs |  3.414 μs |  3.193 μs |  1.17 |    0.02 |        - |         - |          NA |
| MissingValues_Mod      | Mod        |   618.63 μs | 10.328 μs |  8.063 μs |  2.16 |    0.03 |        - |         - |          NA |
| BigInteger_Mod         | Mod        | 2,066.71 μs | 40.953 μs | 53.250 μs |  7.22 |    0.19 | 406.2500 | 2560000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Mul               | Mul        |    85.60 μs |  1.329 μs |  1.243 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Mul         | Mul        |   147.97 μs |  1.622 μs |  1.354 μs |  1.73 |    0.03 |        - |         - |          NA |
| MissingValues_Mul      | Mul        |    86.49 μs |  1.533 μs |  1.434 μs |  1.01 |    0.02 |        - |         - |          NA |
| BigInteger_Mul         | Mul        | 2,162.51 μs | 42.123 μs | 85.090 μs | 25.27 |    1.05 | 519.5313 | 3280000 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Parse             | Parse      |   976.52 μs | 15.069 μs | 13.358 μs |  1.00 |    0.02 |        - |         - |          NA |
| Nethermind_Parse       | Parse      | 4,214.37 μs | 83.176 μs | 73.734 μs |  4.32 |    0.09 | 250.0000 | 1680000 B |          NA |
| MissingValues_Parse    | Parse      | 2,216.06 μs | 42.835 μs | 62.787 μs |  2.27 |    0.07 |        - |         - |          NA |
| BigInteger_Parse       | Parse      | 4,495.57 μs | 79.568 μs | 70.535 μs |  4.60 |    0.09 | 273.4375 | 1759640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_Sub               | Sub        |    37.04 μs |  0.415 μs |  0.324 μs |  1.00 |    0.01 |        - |         - |          NA |
| Nethermind_Sub         | Sub        |    63.05 μs |  0.532 μs |  0.498 μs |  1.70 |    0.02 |        - |         - |          NA |
| MissingValues_Sub      | Sub        |    36.97 μs |  0.156 μs |  0.130 μs |  1.00 |    0.01 |        - |         - |          NA |
| BigInteger_Sub         | Sub        | 1,716.13 μs | 34.174 μs | 58.030 μs | 46.33 |    1.59 | 546.8750 | 3439640 B |          NA |
|                        |            |             |           |           |       |         |          |           |             |
| Ours_ToString          | ToString   | 1,311.38 μs | 25.917 μs | 44.705 μs |  1.00 |    0.05 | 292.9688 | 1840000 B |        1.00 |
| Nethermind_ToString    | ToString   | 2,064.37 μs | 41.257 μs | 93.964 μs |  1.58 |    0.09 | 378.9063 | 2400000 B |        1.30 |
| MissingValues_ToString | ToString   | 1,420.77 μs | 25.415 μs | 21.223 μs |  1.08 |    0.04 | 292.9688 | 1840000 B |        1.00 |
| BigInteger_ToString    | ToString   | 2,588.16 μs | 50.609 μs | 65.806 μs |  1.98 |    0.08 | 468.7500 | 2960000 B |        1.61 |
