using System.Text;

namespace Stardust.Utilities;

/// <summary>
/// Generates RFC 2360-style ASCII diagrams from <see cref="BitFieldInfo"/> metadata.
/// Works with any <c>[BitFields]</c> or <c>[BitFieldsView]</c> struct that exposes a
/// static <c>Fields</c> property.
/// </summary>
/// <example>
/// <code>
/// // Generate diagram from any BitFields/BitFieldsView struct:
/// List&lt;string&gt; lines = BitFieldDiagram.Render(IPv4HeaderView.Fields);
///
/// // With descriptions legend:
/// List&lt;string&gt; lines = BitFieldDiagram.Render(CpuStatusRegister.Fields, includeDescriptions: true);
///
/// // Custom width (8 or 16 bits per row):
/// List&lt;string&gt; lines = BitFieldDiagram.Render(CpuStatusRegister.Fields, bitsPerRow: 8);
///
/// // Render multiple structs as a unified list with consistent scale:
/// var sections = new DiagramSection[]
/// {
///     new("Data Registers", M68020DataRegisters.Fields.ToArray()),
///     new("SR", M68020SR.Fields.ToArray()),
/// };
/// string diagram = BitFieldDiagram.RenderListToString(sections);
/// </code>
/// </example>

/// <summary>
/// Describes a labeled section for multi-struct diagram rendering via
/// <see cref="BitFieldDiagram.RenderList"/> and <see cref="BitFieldDiagram.RenderListToString"/>.
/// </summary>
/// <param name="Label">Section heading displayed above the diagram (empty string for no heading).</param>
/// <param name="Fields">The field metadata array (from the generated <c>Fields</c> property).</param>
public readonly record struct DiagramSection(string Label, BitFieldInfo[] Fields);

public static class BitFieldDiagram
{
    /// <summary>
    /// Renders an RFC-style ASCII bit field diagram from the given field metadata.
    /// Bit order is detected automatically from field metadata: MSB-first fields (RFC convention)
    /// show bit 0 on the left; LSB-first fields (hardware convention) show bit 0 on the right.
    /// When adjacent fields have different bit orders, a new section with fresh headers is emitted.
    /// </summary>
    /// <param name="fields">The field metadata list (from the generated <c>Fields</c> property).</param>
    /// <param name="bitsPerRow">Number of bits per diagram row. Defaults to 32. Common values: 8, 16, 32.</param>
    /// <param name="includeDescriptions">When true, appends a legend with field descriptions below the diagram.</param>
    /// <param name="showByteOffset">When true, shows hex byte offset (e.g., 0x00) at the left of each content row.</param>
    /// <param name="minCellWidth">Minimum cell width (characters per bit column). When 0, computed automatically.
    /// Used internally by <see cref="RenderList"/> to enforce consistent scale across sections.</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> Render(ReadOnlySpan<BitFieldInfo> fields, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, int minCellWidth = 0)
    {
        if (fields.Length == 0)
            return ["(no fields)"];

        if (bitsPerRow < 1) bitsPerRow = 32;

        // Determine total bit span
        int maxBit = 0;
        foreach (var f in fields)
        {
            if (f.EndBit > maxBit) maxBit = f.EndBit;
        }
        // Use the struct's declared total bits when available (so trailing undefined bits are shown)
        if (fields.Length > 0 && fields[0].StructTotalBits > 0)
            maxBit = Math.Max(maxBit, fields[0].StructTotalBits - 1);
        int totalRows = (maxBit / bitsPerRow) + 1;

        // Build a cell map: for each bit position, store the field (or null for gaps)
        var cellMap = new BitFieldInfo?[maxBit + 1];
        foreach (var f in fields)
        {
            for (int b = f.StartBit; b <= f.EndBit; b++)
            {
                if (b < cellMap.Length) cellMap[b] = f;
            }
        }

        // Pre-scan all spans to compute minimum cell width that fits every named field label.
        // Undefined gaps use "U" if they don't fit, so they don't drive the width.
        int cellWidth = Math.Max(2, minCellWidth);
        bool hasUndefined = false;
        for (int row = 0; row < totalRows; row++)
        {
            int rowStart = row * bitsPerRow;
            int col = 0;
            while (col < bitsPerRow)
            {
                int bp = rowStart + col;
                if (bp > maxBit) break;
                var field = cellMap[bp];
                int spanStart = col;
                while (col < bitsPerRow)
                {
                    int bp2 = rowStart + col;
                    if (bp2 > maxBit) break;
                    if (cellMap[bp2] != field) break;
                    if (field != null && bp2 > field.EndBit) break;
                    col++;
                }
                int spanBits = col - spanStart;
                if (field != null)
                {
                    // Need: spanBits * cellWidth - 1 >= name.Length
                    int needed = (field.Name.Length + spanBits) / spanBits;
                    cellWidth = Math.Max(cellWidth, needed);
                }
                else
                {
                    hasUndefined = true;
                }
            }
        }

        var lines = new List<string>();
        int gutterWidth = showByteOffset ? 7 : 0;
        string gutterBlank = new string(' ', gutterWidth);
        int lineWidth = 1 + bitsPerRow * cellWidth;

        BitOrder? prevBitOrder = null;
        for (int row = 0; row < totalRows; row++)
        {
            int rowStartBit = row * bitsPerRow;
            int rowEndBit = Math.Min(rowStartBit + bitsPerRow - 1, maxBit);
            int rowCols = rowEndBit - rowStartBit + 1;
            int byteOffset = rowStartBit / 8;
            string offsetLabel = showByteOffset
                ? $"0x{byteOffset:X2}".PadRight(gutterWidth)
                : "";

            // Detect bit order for this row from the first defined field
            BitOrder rowOrder = prevBitOrder ?? BitOrder.BitZeroIsLsb;
            for (int b = rowStartBit; b <= rowEndBit; b++)
            {
                var bitFieldInfo = cellMap[b];
                if (bitFieldInfo != null)
                {
                    rowOrder = bitFieldInfo.BitOrder;
                    break;
                }
            }
            bool reversed = (rowOrder == BitOrder.BitZeroIsLsb);
            bool sectionChange = (prevBitOrder == null || rowOrder != prevBitOrder);

            // Emit bit-position header rows when bit order changes (or first row)
            if (sectionChange)
            {
                if (prevBitOrder != null)
                    lines.Add("");

                int headerWidth = gutterWidth + 1 + rowCols * cellWidth;
                var tensChars = new char[headerWidth];
                var onesChars = new char[headerWidth];
                Array.Fill(tensChars, ' ');
                Array.Fill(onesChars, ' ');
                for (int col = 0; col < rowCols; col++)
                {
                    int bitNum = reversed ? (rowCols - 1 - col) : col;
                    int center = gutterWidth + col * cellWidth + cellWidth / 2;
                    if (center < onesChars.Length)
                    {
                        onesChars[center] = (char)('0' + (bitNum % 10));
                        if (bitNum % 10 == 0)
                            tensChars[center] = (char)('0' + (bitNum / 10));
                    }
                }
                lines.Add(new string(tensChars).TrimEnd());
                lines.Add(new string(onesChars).TrimEnd());

                // Top separator for new section
                lines.Add(gutterBlank + BuildSeparator(rowCols, cellWidth));
            }

            // Field content row with byte offset label
            var contentLine = new StringBuilder(gutterWidth + lineWidth);
            contentLine.Append(offsetLabel);
            contentLine.Append('|');
            int col2 = 0;
            while (col2 < rowCols)
            {
                int bitPos = reversed
                    ? rowStartBit + rowCols - 1 - col2
                    : rowStartBit + col2;
                var field = cellMap[bitPos];
                int spanStart = col2;
                while (col2 < rowCols)
                {
                    int bp = reversed
                        ? rowStartBit + rowCols - 1 - col2
                        : rowStartBit + col2;
                    if (cellMap[bp] != field) break;
                    if (field != null && (reversed ? bp < field.StartBit : bp > field.EndBit)) break;
                    col2++;
                }
                int spanLen = col2 - spanStart;
                int spanCharWidth = spanLen * cellWidth - 1;

                string label;
                if (field != null)
                {
                    label = field.Name;
                }
                else
                {
                    label = spanCharWidth >= 9 ? "Undefined" : "U";
                }
                if (label.Length > spanCharWidth)
                    label = label[..spanCharWidth];

                int totalPad = spanCharWidth - label.Length;
                int leftPad = totalPad / 2;
                int rightPad = totalPad - leftPad;
                contentLine.Append(' ', leftPad);
                contentLine.Append(label);
                contentLine.Append(' ', rightPad);
                contentLine.Append('|');
            }
            lines.Add(contentLine.ToString());

            // Bottom separator
            lines.Add(gutterBlank + BuildSeparator(rowCols, cellWidth));

            prevBitOrder = rowOrder;
        }

        // Descriptions legend
        if (includeDescriptions || hasUndefined)
        {
            bool hasAny = false;
            if (includeDescriptions)
            {
                foreach (var f in fields)
                {
                    var desc = f.GetDescription();
                    string? mustBe = f.FieldMustBe switch
                    {
                        1 => "must be 0",
                        2 => "must be 1",
                        _ => null
                    };
                    if (desc != null || mustBe != null)
                    {
                        if (!hasAny)
                        {
                            lines.Add("");
                            hasAny = true;
                        }
                        string text = (desc, mustBe) switch
                        {
                            (not null, not null) => $"{desc} ({mustBe})",
                            (not null, null) => desc,
                            (null, not null) => mustBe,
                            _ => ""
                        };
                        lines.Add($"  {f.Name}: {text}");
                    }
                }
            }
            if (hasUndefined)
            {
                lines.Add("");
                int undefinedMode = fields.Length > 0 ? fields[0].StructUndefinedMustBe : 0;
                string undefinedLegend = undefinedMode switch
                {
                    1 => "  U/Undefined = bits not defined in the struct (must be 0)",
                    2 => "  U/Undefined = bits not defined in the struct (must be 1)",
                    _ => "  U/Undefined = bits not defined in the struct"
                };
                lines.Add(undefinedLegend);
            }

        }

        return lines;
    }

    /// <summary>
    /// Renders the diagram as a single string with newlines.
    /// </summary>
    public static string RenderToString(ReadOnlySpan<BitFieldInfo> fields, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, int minCellWidth = 0)
    {
        var lines = Render(fields, bitsPerRow, includeDescriptions, showByteOffset, minCellWidth);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Computes the minimum cell width (characters per bit column) needed to fit all field names
    /// for the given fields at the specified bits-per-row. Useful for pre-computing a shared width
    /// across multiple structs, though <see cref="RenderList"/> handles this automatically.
    /// </summary>
    public static int ComputeMinCellWidth(ReadOnlySpan<BitFieldInfo> fields, int bitsPerRow = 32)
    {
        if (fields.Length == 0 || bitsPerRow < 1) return 2;

        int maxBit = 0;
        foreach (var f in fields)
        {
            if (f.EndBit > maxBit) maxBit = f.EndBit;
        }
        if (fields[0].StructTotalBits > 0)
            maxBit = Math.Max(maxBit, fields[0].StructTotalBits - 1);
        int totalRows = (maxBit / bitsPerRow) + 1;

        var cellMap = new BitFieldInfo?[maxBit + 1];
        foreach (var f in fields)
        {
            for (int b = f.StartBit; b <= f.EndBit; b++)
            {
                if (b < cellMap.Length) cellMap[b] = f;
            }
        }

        int cellWidth = 2;
        for (int row = 0; row < totalRows; row++)
        {
            int rowStart = row * bitsPerRow;
            int col = 0;
            while (col < bitsPerRow)
            {
                int bp = rowStart + col;
                if (bp > maxBit) break;
                var field = cellMap[bp];
                int spanStart = col;
                while (col < bitsPerRow)
                {
                    int bp2 = rowStart + col;
                    if (bp2 > maxBit) break;
                    if (cellMap[bp2] != field) break;
                    if (field != null && bp2 > field.EndBit) break;
                    col++;
                }
                int spanBits = col - spanStart;
                if (field != null)
                {
                    int needed = (field.Name.Length + spanBits) / spanBits;
                    cellWidth = Math.Max(cellWidth, needed);
                }
            }
        }
        return cellWidth;
    }

    /// <summary>
    /// Renders multiple struct sections as a unified diagram list with consistent cell widths.
    /// The widest field name across all sections determines the scale for the entire output.
    /// </summary>
    /// <param name="sections">The labeled sections to render sequentially.</param>
    /// <param name="bitsPerRow">Number of bits per diagram row.</param>
    /// <param name="includeDescriptions">When true, appends field description legends.</param>
    /// <param name="showByteOffset">When true, shows hex byte offsets at the left.</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> RenderList(ReadOnlySpan<DiagramSection> sections, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true)
    {
        if (sections.Length == 0) return ["(no sections)"];
        if (bitsPerRow < 1) bitsPerRow = 32;

        // Compute shared cell width across all sections
        int sharedCellWidth = 2;
        foreach (var section in sections)
        {
            int w = ComputeMinCellWidth(section.Fields, bitsPerRow);
            sharedCellWidth = Math.Max(sharedCellWidth, w);
        }

        var lines = new List<string>();
        foreach (var section in sections)
        {
            if (section.Label.Length > 0)
            {
                if (lines.Count > 0) lines.Add("");
                lines.Add(section.Label);
            }
            lines.AddRange(Render(section.Fields, bitsPerRow, includeDescriptions, showByteOffset, sharedCellWidth));
        }
        return lines;
    }

    /// <summary>
    /// Renders multiple struct sections as a single string with consistent cell widths.
    /// </summary>
    public static string RenderListToString(ReadOnlySpan<DiagramSection> sections, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true)
    {
        var lines = RenderList(sections, bitsPerRow, includeDescriptions, showByteOffset);
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildSeparator(int bitsPerRow, int cellWidth)
    {
        var sb = new StringBuilder(1 + bitsPerRow * cellWidth);
        sb.Append('+');
        for (int i = 0; i < bitsPerRow; i++)
        {
            sb.Append('-', cellWidth - 1);
            sb.Append('+');
        }
        return sb.ToString();
    }
}
