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
/// </code>
/// </example>
public static class BitFieldDiagram
{
    /// <summary>
    /// Renders an RFC-style ASCII bit field diagram from the given field metadata.
    /// </summary>
    /// <param name="fields">The field metadata list (from the generated <c>Fields</c> property).</param>
    /// <param name="bitsPerRow">Number of bits per diagram row. Defaults to 32. Common values: 8, 16, 32.</param>
    /// <param name="includeDescriptions">When true, appends a legend with field descriptions below the diagram.</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> Render(ReadOnlySpan<BitFieldInfo> fields, int bitsPerRow = 32, bool includeDescriptions = false)
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
        int cellWidth = 2;
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
        const int gutterWidth = 7;
        string gutterBlank = new string(' ', gutterWidth);
        int lineWidth = 1 + bitsPerRow * cellWidth;

        for (int row = 0; row < totalRows; row++)
        {
            int rowStartBit = row * bitsPerRow;
            int rowEndBit = Math.Min(rowStartBit + bitsPerRow - 1, maxBit);
            int rowCols = rowEndBit - rowStartBit + 1;
            int byteOffset = rowStartBit / 8;
            string offsetLabel = $"0x{byteOffset:X2}".PadRight(gutterWidth);

            // Bit-position header rows (first row only)
            if (row == 0)
            {
                // Place digits at the center of each cell (only for columns in this row)
                int headerWidth = gutterWidth + 1 + rowCols * cellWidth;
                var tensChars = new char[headerWidth];
                var onesChars = new char[headerWidth];
                Array.Fill(tensChars, ' ');
                Array.Fill(onesChars, ' ');
                for (int col = 0; col < rowCols; col++)
                {
                    int center = gutterWidth + col * cellWidth + cellWidth / 2;
                    if (center < onesChars.Length)
                    {
                        onesChars[center] = (char)('0' + (col % 10));
                        if (col % 10 == 0)
                            tensChars[center] = (char)('0' + (col / 10));
                    }
                }
                lines.Add(new string(tensChars).TrimEnd());
                lines.Add(new string(onesChars).TrimEnd());
            }

            // Top separator only for the first row; subsequent rows share the previous bottom separator
            if (row == 0)
                lines.Add(gutterBlank + BuildSeparator(rowCols, cellWidth));

            // Field content row with byte offset label
            var contentLine = new StringBuilder(gutterWidth + lineWidth);
            contentLine.Append(offsetLabel);
            contentLine.Append('|');
            int col2 = 0;
            while (col2 < rowCols)
            {
                int bitPos = rowStartBit + col2;
                var field = cellMap[bitPos];
                int spanStart = col2;
                while (col2 < rowCols)
                {
                    int bp = rowStartBit + col2;
                    if (cellMap[bp] != field) break;
                    if (field != null && bp > field.EndBit) break;
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
        }

        // Descriptions legend
        if (includeDescriptions || hasUndefined)
        {
            bool hasAny = false;
            if (hasUndefined)
            {
                lines.Add("");
                lines.Add("  U/Undefined = bits not defined in the struct");
                hasAny = true;
            }
            if (includeDescriptions)
            {
                foreach (var f in fields)
                {
                    var desc = f.GetDescription();
                    if (desc != null)
                    {
                        if (!hasAny)
                        {
                            lines.Add("");
                            hasAny = true;
                        }
                        string bits = f.IsFlag
                            ? $"bit {f.StartBit}"
                            : $"bits {f.StartBit}-{f.EndBit}";
                        lines.Add($"  {f.Name} ({bits}): {desc}");
                    }
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// Renders the diagram as a single string with newlines.
    /// </summary>
    public static string RenderToString(ReadOnlySpan<BitFieldInfo> fields, int bitsPerRow = 32, bool includeDescriptions = false)
    {
        var lines = Render(fields, bitsPerRow, includeDescriptions);
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
