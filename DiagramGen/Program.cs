// ─────────────────────────────────────────────────────────────────────────────
// DiagramGen -- Developer tool for regenerating RFC-style ASCII diagrams
//
// Generates BitFieldDiagram output for pre-defined numeric types (and any
// other [BitFields] / [BitFieldsView] structs) for embedding in documentation.
//
// Usage:
//   dotnet run --project DiagramGen
//   dotnet run --project DiagramGen -- --descriptions   (include field descriptions)
//
// The output is written to stdout. Pipe or copy into README.md / BITFIELDS.md.
// ─────────────────────────────────────────────────────────────────────────────

using Stardust.Utilities;

bool includeDescriptions = args.Contains("--descriptions", StringComparer.OrdinalIgnoreCase);

(string title, Type type, int bitsPerRow)[] types =
[
    ("IEEE754Half (16-bit)",       typeof(IEEE754Half),       16),
    ("IEEE754Single (32-bit)",     typeof(IEEE754Single),     32),
    ("IEEE754Double (64-bit)",     typeof(IEEE754Double),     64),
    ("DecimalBitFields (128-bit)", typeof(DecimalBitFields),  32),
];

foreach (var (title, type, bitsPerRow) in types)
{
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine(BitFieldDiagram.RenderToString(
        type, bitsPerRow: bitsPerRow, showByteOffset: false, includeDescriptions: includeDescriptions));
    Console.WriteLine();
}
