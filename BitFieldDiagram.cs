using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Stardust.Utilities;

using static Result<string>;

/// <summary>
/// Generates RFC 2360-style ASCII diagrams from <see cref="BitFieldInfo"/> metadata.
/// Works with any <c>[BitFields]</c> or <c>[BitFieldsView]</c> struct that exposes a
/// static <c>Fields</c> property.
/// </summary>
/// <example>
/// <code>
/// // Generate diagram from any BitFields/BitFieldsView struct:
/// var diagram = new BitFieldDiagram(typeof(IPv4HeaderView));
/// string output = diagram.RenderToString().Value;
///
/// // With descriptions legend:
/// var diagram = new BitFieldDiagram(typeof(CpuStatusRegister), includeDescriptions: true);
/// string output = diagram.RenderToString().Value;
///
/// // Custom width (8 or 16 bits per row):
/// var diagram = new BitFieldDiagram(typeof(CpuStatusRegister), bitsPerRow: 8);
/// string output = diagram.RenderToString().Value;
///
/// // Render multiple structs as a unified list with consistent scale:
/// var diagram = new BitFieldDiagram(
///     [typeof(M68020DataRegisters), typeof(M68020SR)],
///     description: "Register Set");
/// string output = diagram.RenderToString().Value;
/// </code>
/// </example>

/// <summary>
/// Describes a labeled section for multi-struct diagram rendering via
/// <see cref="BitFieldDiagram.RenderList"/> and <see cref="BitFieldDiagram.RenderListToString"/>.
/// </summary>
/// <param name="Label">Section heading displayed above the diagram (empty string for no heading).</param>
/// <param name="Fields">The field metadata array (from the generated <c>Fields</c> property).</param>
[Obsolete("Use the Type-based RenderList/RenderListToString overloads instead. Set Description on [BitFields]/[BitFieldsView] attributes to provide section labels.")]
public readonly record struct DiagramSection(string Label, BitFieldInfo[] Fields);

public class BitFieldDiagram
{
    /// <summary>
    /// Create a new diagram instance with an empty list of structs (to be added later using <see cref="AddStruct(Type)"/>).
    /// </summary>
    /// <param name="description">Defaults to null</param>
    /// <param name="commentPrefix">Defaults to null</param>
    /// <param name="bitsPerRow">Defaults to 32</param>
    /// <param name="includeDescriptions">Defaults to true</param>
    /// <param name="showByteOffset">Defaults to false</param>
    /// <param name="descriptionResourceType">Defaults to null</param>
    public BitFieldDiagram(string? description = null, string? commentPrefix = null, int bitsPerRow = 32, bool includeDescriptions = true, bool showByteOffset = false, Type? descriptionResourceType = null)
    {
        BitsPerRow = bitsPerRow;
        IncludeDescriptions = includeDescriptions;
        ShowByteOffset = showByteOffset;
        CommentPrefix = commentPrefix;
        Description = description;
        DescriptionResourceType = descriptionResourceType;
    }

    /// <summary>
    /// Create a new diagram with a list of structs to render.
    /// </summary>
    /// <param name="structs"></param>
    /// <param name="description"></param>
    /// <param name="commentPrefix"></param>
    /// <param name="bitsPerRow"></param>
    /// <param name="includeDescriptions"></param>
    /// <param name="showByteOffset"></param>
    /// <param name="descriptionResourceType"></param>
    /// <exception cref="ArgumentException"></exception>
    public BitFieldDiagram(ReadOnlySpan<Type> structs, string? description = null, string? commentPrefix = null, int bitsPerRow = 32, bool includeDescriptions = true, bool showByteOffset = false, Type? descriptionResourceType = null)
        : this(description, commentPrefix, bitsPerRow, includeDescriptions, showByteOffset, descriptionResourceType)
    {
        foreach (var field in structs)
        {
            AddStruct(field).OnFailure(err => throw new ArgumentException($"Failed to add struct type '{field.Name}': {err}"));
        }
    }

    /// <summary>
    /// Create a new diagram with a list of structs to render.
    /// </summary>
    /// <param name="structs"></param>
    /// <param name="description"></param>
    /// <param name="commentPrefix"></param>
    /// <param name="bitsPerRow"></param>
    /// <param name="includeDescriptions"></param>
    /// <param name="showByteOffset"></param>
    /// <param name="descriptionResourceType"></param>
    /// <exception cref="ArgumentException"></exception>
    public BitFieldDiagram(IEnumerable<Type> structs, string? description = null, string? commentPrefix = null, int bitsPerRow = 32, bool includeDescriptions = true, bool showByteOffset = false, Type? descriptionResourceType = null)
        : this(description, commentPrefix, bitsPerRow, includeDescriptions, showByteOffset, descriptionResourceType)
    {
        foreach (var field in structs)
        {
            AddStruct(field).OnFailure(err => throw new ArgumentException($"Failed to add struct type '{field.Name}': {err}"));
        }
    }

    /// <summary>
    /// Create a new diagram with struct to render.
    /// </summary>
    /// <param name="bitStruct"></param>
    /// <param name="description"></param>
    /// <param name="commentPrefix"></param>
    /// <param name="bitsPerRow"></param>
    /// <param name="includeDescriptions"></param>
    /// <param name="showByteOffset"></param>
    /// <param name="descriptionResourceType"></param>
    /// <exception cref="ArgumentException"></exception>
    public BitFieldDiagram(Type bitStruct, string? description = null, string? commentPrefix = null, int bitsPerRow = 32, bool includeDescriptions = true, bool showByteOffset = false, Type? descriptionResourceType = null)
        : this([bitStruct], description, commentPrefix, bitsPerRow, includeDescriptions, showByteOffset, descriptionResourceType)
    {
    }

    /// <summary>
    /// Bits per row.
    /// </summary>
    public virtual int BitsPerRow { get; set; } = 32;
    /// <summary>
    /// True to include descriptions.
    /// </summary>
    public virtual bool IncludeDescriptions { get; set; } = false;
    /// <summary>
    /// True to show byte offset to left of each row.
    /// </summary>
    public virtual bool ShowByteOffset { get; set; } = false;
    /// <summary>
    /// Comment prefix (if any) that will be prepended to the left of each line.
    /// </summary>
    public virtual string? CommentPrefix { get; set; }
    /// <summary>
    /// Description/Caption for the diagram.  If <see cref="DescriptionResourceType"/> is present,
    /// this is the key used to retrieve the localized resource string.
    /// </summary>
    public virtual string? Description { get; set; }
    /// <summary>
    /// Optional string resource provider.  If present, the <see cref="Description"/> is the key used to retrieve the
    /// localized resource string.  This line (or lines if it contains newlines) is prepended to the list of lines
    /// in the diagram, with the optional <see cref="CommentPrefix"/> prepended if present.
    /// </summary>
    public virtual Type? DescriptionResourceType { get; set; }


    /// <summary>
    /// Returns the resolved description string. When <see cref="DescriptionResourceType"/> is set,
    /// looks up <see cref="Description"/> as a resource key using the type's <c>ResourceManager</c>.
    /// Otherwise returns <see cref="Description"/> as a literal string.
    /// </summary>
    /// <param name="culture">
    /// The culture to use for resource lookup. Defaults to <see cref="CultureInfo.CurrentUICulture"/> when null.
    /// </param>
    /// <returns>The resolved description, or null if no description was specified.</returns>
    public virtual string? GetDescription(CultureInfo? culture = null)
    {
        if (Description is null)
            return null;

        if (DescriptionResourceType is null)
            return Description;

        var prop = DescriptionResourceType.GetProperty(
            "ResourceManager",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (prop?.GetValue(null) is ResourceManager rm)
            return rm.GetString(Description, culture ?? CultureInfo.CurrentUICulture) ?? Description;

        return Description;
    }

    /// <summary>
    /// List of [BitFIelds], [BitFieldsView] or a mixture of the two.
    /// </summary>
    public virtual List<Type> Structs { get; } = [];

    /// <summary>
    /// Add structs to the diagram.  Each type must be decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c> 
    /// and have a static <c>Fields</c> property.
    /// </summary>
    /// <param name="structTypes"></param>
    /// <returns></returns>
    public virtual Result<string> AddStructs(ReadOnlySpan<Type> structTypes)
    {
        foreach (var structType in structTypes)
        {
            var result = AddStruct(structType);
            if (result.IsFailure) return result;
        }

        return Ok();
    }

    /// <summary>
    /// Add structs to the diagram.  Each type must be decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c> 
    /// and have a static <c>Fields</c> property.
    /// </summary>
    /// <param name="structTypes"></param>
    /// <returns></returns>
    public virtual Result<string> AddStructs(IEnumerable<Type> structTypes)
    {
        if (structTypes == null) return Err("structTypes is null");

        foreach (var structType in structTypes)
        {
            var result = AddStruct(structType);
            if (result.IsFailure) return result;
        }

        return Ok();
    }

    /// <summary>
    /// Adds a struct type to the diagram.
    /// </summary>
    /// <param name="structType">The struct type to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public virtual Result<string> AddStruct(Type structType)
    {
        if (structType == null) return Err("structType is null");
        if (!structType.IsBitsType()) return Err($"Struct '{structType.Name}' is not a valid [BitFields] or [BitFieldsView] type.");

        var fieldInfoResult = structType.GetFieldInfo();
        if (fieldInfoResult.IsFailure) return Err(fieldInfoResult.Error);
        if (fieldInfoResult.Value.Length == 0) return Err($"Struct '{structType.Name}' has no fieldInfos.");

        // Add the struct type to the list for later rendering
        Structs.Add(structType);
        return Ok();
    }

    public virtual Result<string, string> RenderToString()
    {
        var result = Render();
        if (result.IsFailure) return Result<string, string>.Err(result.Error);

        return Ok(string.Join(Environment.NewLine, result.Value));
    }

    public virtual Result<List<string>,string> Render()
    {
        if (Structs.Count == 0) return Result<List<string>, string>.Err(FormatLine("(no bitStruct)", CommentPrefix));

        // Resolve the description (handles resource lookup via DescriptionResourceType)
        string? resolvedDescription = GetDescription();

        // Render all added structs as a unified diagram.
        // RenderList emits the description once as a top-level title.
        var lines = RenderList([.. Structs], resolvedDescription, BitsPerRow, IncludeDescriptions, ShowByteOffset, CommentPrefix);

        return Ok(lines);
    }

    /// <summary>
    /// Renders an RFC-style ASCII bit field diagram from the given field metadata.
    /// Bit order is detected automatically from field metadata: MSB-first fields (RFC convention)
    /// show bit 0 on the left; LSB-first fields (hardware convention) show bit 0 on the right.
    /// When adjacent fields have different bit orders, a new section with fresh headers is emitted.
    /// </summary>
    /// <param name="fields">The field metadata list (from the generated <c>Fields</c> property).</param>
    /// <param name="description">Optional description.  Ignored if <see cref="IncludeDescriptions"/> is false.</param>
    /// <param name="bitsPerRow">Number of bits per diagram row. Defaults to 32. Common values: 8, 16, 32.</param>
    /// <param name="includeDescriptions">When true, appends a legend with field descriptions below the diagram.</param>
    /// <param name="showByteOffset">When true, shows hex byte offset (e.g., 0x00) at the left of each content row.</param>
    /// <param name="minCellWidth">Minimum cell width (characters per bit column). When 0, computed automatically.
    /// Used internally by <see cref="RenderList"/> to enforce consistent scale across sections.</param>
    /// <param name="commentPrefix">When non-null, prepended to every output line (e.g., <c>"// "</c> or <c>"/// "</c>).</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> Render(ReadOnlySpan<BitFieldInfo> fields, string? description = null, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, int minCellWidth = 0, string? commentPrefix = null)
        => RenderCore(fields, description, bitsPerRow, includeDescriptions, showByteOffset, minCellWidth, commentPrefix, emitStructDescription: true);

    /// <summary>Core rendering logic with control over StructDescription emission.</summary>
    private static List<string> RenderCore(ReadOnlySpan<BitFieldInfo> fields, string? description, int bitsPerRow, bool includeDescriptions, bool showByteOffset, int minCellWidth, string? commentPrefix, bool emitStructDescription)
    {
        if (fields.Length == 0)
        {
            return [FormatLine("(no fields)", commentPrefix)];
        }

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

        // Build layered cell maps: layer 0 holds non-overlapping fields (first-writer-wins),
        // subsequent layers hold fields displaced by overlaps.
        var layers = BuildLayers(fields, maxBit + 1);
        var cellMap = layers[0];

        // Pre-scan all spans across all layers to compute minimum cell width
        // that fits every named field label at its actual (per-layer) span width.
        int cellWidth = Math.Max(2, minCellWidth);
        bool hasUndefined = false;
        for (int li = 0; li < layers.Count; li++)
        {
            var layerMap = layers[li];
            for (int row = 0; row < totalRows; row++)
            {
                int rowStart = row * bitsPerRow;
                int col = 0;
                while (col < bitsPerRow)
                {
                    int bp = rowStart + col;
                    if (bp > maxBit) break;
                    var field = layerMap[bp];
                    int spanStart = col;
                    while (col < bitsPerRow)
                    {
                        int bp2 = rowStart + col;
                        if (bp2 > maxBit) break;
                        if (layerMap[bp2] != field) break;
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
                    else if (li == 0)
                    {
                        hasUndefined = true;
                    }
                }
            }
        }

        var lines = new List<string>();
        
        // Emit struct-level title as a header when provided.
        if (includeDescriptions && description != null)
        {
            lines.Add(FormatLine(description, commentPrefix));
        }

        // Emit struct-level description as a header when descriptions are enabled
        // and the caller hasn't already handled section labeling (emitStructDescription).
        // Split on embedded newlines so every visual line is a separate entry
        // (required for comment-prefix to be applied to each line).
        if (emitStructDescription && includeDescriptions && fields.Length > 0 && fields[0].StructDescription is { } structDesc)
        {
            foreach (var descLine in structDesc.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
                lines.Add(descLine);
        }

        int gutterWidth = showByteOffset ? 7 : 0;
        string gutterBlank = new(' ', gutterWidth);
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
                // Only show the tens-digit line when at least one bit number >= 10
                int maxBitNum = reversed ? (rowCols - 1) : rowCols - 1;
                if (maxBitNum >= 10)
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

            // Overlay layers (for overlapping fields shown as stacked alternate rows)
            for (int li = 1; li < layers.Count; li++)
            {
                var overlayMap = layers[li];

                // Check if this overlay layer has any content in this row
                bool hasOverlayContent = false;
                for (int b = rowStartBit; b <= rowEndBit; b++)
                {
                    if (b < overlayMap.Length && overlayMap[b] != null)
                    {
                        hasOverlayContent = true;
                        break;
                    }
                }
                if (!hasOverlayContent) continue;

                // Hybrid separator: dashed where overlay has content, solid elsewhere
                lines.Add(gutterBlank + BuildHybridSeparator(rowCols, cellWidth, overlayMap, rowStartBit, reversed, maxBit));

                // Overlay content row
                var overlayLine = new StringBuilder(gutterWidth + lineWidth);
                overlayLine.Append(gutterBlank);
                overlayLine.Append('|');
                int oc = 0;
                while (oc < rowCols)
                {
                    int bitPos = reversed
                        ? rowStartBit + rowCols - 1 - oc
                        : rowStartBit + oc;
                    var field = (bitPos <= maxBit && bitPos < overlayMap.Length) ? overlayMap[bitPos] : null;
                    int spanStart = oc;
                    while (oc < rowCols)
                    {
                        int bp = reversed
                            ? rowStartBit + rowCols - 1 - oc
                            : rowStartBit + oc;
                        var current = (bp <= maxBit && bp < overlayMap.Length) ? overlayMap[bp] : null;
                        if (current != field) break;
                        if (field != null && (reversed ? bp < field.StartBit : bp > field.EndBit)) break;
                        oc++;
                    }
                    int spanLen = oc - spanStart;
                    int spanCharWidth = spanLen * cellWidth - 1;

                    string label = field != null ? field.Name : "";
                    if (label.Length > spanCharWidth)
                        label = label[..spanCharWidth];

                    int totalPad = spanCharWidth - label.Length;
                    int leftPad = totalPad / 2;
                    int rightPad = totalPad - leftPad;
                    overlayLine.Append(' ', leftPad);
                    overlayLine.Append(label);
                    overlayLine.Append(' ', rightPad);
                    overlayLine.Append('|');
                }
                lines.Add(overlayLine.ToString());
            }

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
                        MustBe.Zero => "must be 0",
                        MustBe.One => "must be 1",
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
                        // Split on embedded newlines so each display line is a separate entry
                        string[] descLines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
                        lines.Add($"  {f.Name}: {descLines[0]}");
                        for (int d = 1; d < descLines.Length; d++)
                            lines.Add($"    {descLines[d]}");
                    }
                }
            }
            if (hasUndefined)
            {
                lines.Add("");
                UndefinedBitsMustBe undefinedMode = fields.Length > 0 ? fields[0].StructUndefinedMustBe : UndefinedBitsMustBe.Any;
                string undefinedLegend = undefinedMode switch
                {
                    UndefinedBitsMustBe.Zeroes => "  U/Undefined = bits not defined in the struct (must be 0)",
                    UndefinedBitsMustBe.Ones => "  U/Undefined = bits not defined in the struct (must be 1)",
                    _ => "  U/Undefined = bits not defined in the struct"
                };
                lines.Add(undefinedLegend);
            }

        }

        if (commentPrefix != null)
        {
            for (int i = 0; i < lines.Count; i++)
                lines[i] = FormatLine(lines[i], commentPrefix);
        }

        return lines;
    }

    /// <summary>
    /// Renders the diagram as a single string with newlines.
    /// </summary>
    public static string RenderToString(ReadOnlySpan<BitFieldInfo> fields, string? title = null, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, int minCellWidth = 0, string? commentPrefix = null)
    {
        var lines = Render(fields, title, bitsPerRow, includeDescriptions, showByteOffset, minCellWidth, commentPrefix);
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

        var layers = BuildLayers(fields, maxBit + 1);

        int cellWidth = 2;
        foreach (var layerMap in layers)
        {
            for (int row = 0; row < totalRows; row++)
            {
                int rowStart = row * bitsPerRow;
                int col = 0;
                while (col < bitsPerRow)
                {
                    int bp = rowStart + col;
                    if (bp > maxBit) break;
                    var field = layerMap[bp];
                    int spanStart = col;
                    while (col < bitsPerRow)
                    {
                        int bp2 = rowStart + col;
                        if (bp2 > maxBit) break;
                        if (layerMap[bp2] != field) break;
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
        }
        return cellWidth;
    }

    /// <summary>
    /// Renders an RFC-style ASCII bit field diagram for the specified <c>[BitFields]</c> or <c>[BitFieldsView]</c> type.
    /// The type must have a static <c>Fields</c> property returning <c>ReadOnlySpan&lt;BitFieldInfo&gt;</c>.
    /// </summary>
    /// <param name="bitFieldsType">The struct type decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c>.</param>
    /// <param name="bitsPerRow">Number of bits per diagram row.</param>
    /// <param name="includeDescriptions">When true, appends a legend with field descriptions below the diagram.</param>
    /// <param name="showByteOffset">When true, shows hex byte offset at the left of each content row.</param>
    /// <param name="commentPrefix">When non-null, prepended to every output line.</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> Render(Type bitFieldsType, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        var fieldsResult = bitFieldsType.GetFieldInfo();
        var fields = fieldsResult.IsSuccess ? fieldsResult.Value : [];
        // StructDescription is already available in fields[0].StructDescription and
        // emitted by the fields-based Render when includeDescriptions is true.
        return Render(fields, null, bitsPerRow, includeDescriptions, showByteOffset, 0, commentPrefix);
    }

    /// <summary>
    /// Renders the diagram for the specified type as a single string with newlines.
    /// </summary>
    public static string RenderToString(Type bitFieldsType, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        var lines = Render(bitFieldsType, bitsPerRow, includeDescriptions, showByteOffset, commentPrefix);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Renders multiple <c>[BitFields]</c> or <c>[BitFieldsView]</c> types as a unified diagram
    /// with consistent cell widths. Each type's <c>StructDescription</c> (or simple type name when
    /// no description is set) is shown as a section heading.
    /// </summary>
    /// <param name="bitFieldsTypes"></param>
    /// <param name="description"></param>
    /// <param name="bitsPerRow">Number of bits per diagram row.</param>
    /// <param name="includeDescriptions">When true, appends field description legends.</param>
    /// <param name="showByteOffset">When true, shows hex byte offsets at the left.</param>
    /// <param name="commentPrefix">When non-null, prepended to every output line.</param>
    /// <returns>A list of strings, one per output line.</returns>
    public static List<string> RenderList(ReadOnlySpan<Type> bitFieldsTypes, string? description = null, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        if (bitFieldsTypes.Length == 0) return [FormatLine("(no types)", commentPrefix)];
        if (bitsPerRow < 1) bitsPerRow = 32;

        var allFields = new BitFieldInfo[bitFieldsTypes.Length][];
        int sharedCellWidth = 2;
        for (int i = 0; i < bitFieldsTypes.Length; i++)
        {
            var result = bitFieldsTypes[i].GetFieldInfo();
            allFields[i] = result.IsSuccess ? result.Value : [];
            int w = ComputeMinCellWidth(allFields[i], bitsPerRow);
            sharedCellWidth = Math.Max(sharedCellWidth, w);
        }

        var lines = new List<string>();
        if (description != null)
        {
            lines.Add(FormatLine(description, commentPrefix));
        }
        for (int i = 0; i < allFields.Length; i++)
        {
            var fields = allFields[i];

            if (lines.Count > 0) lines.Add(FormatLine("", commentPrefix));

            if (includeDescriptions)
            {
                string? structDesc = fields.Length > 0 ? fields[0].StructDescription : null;

                if (description == null)
                {
                    // No top-level title: emit type name as section heading.
                    // StructDescription (if any) follows on the next line.
                    lines.Add(FormatLine(bitFieldsTypes[i].Name, commentPrefix));
                    if (structDesc != null)
                    {
                        foreach (var descLine in structDesc.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
                            lines.Add(FormatLine(descLine, commentPrefix));
                    }
                }
                else if (structDesc != null && structDesc != description)
                {
                    // Top-level title exists and StructDescription differs: emit as section heading.
                    foreach (var descLine in structDesc.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
                        lines.Add(FormatLine(descLine, commentPrefix));
                }
                // else: structDesc matches top-level description or is null → skip to avoid duplication
            }
            lines.AddRange(RenderCore(fields, null, bitsPerRow, includeDescriptions, showByteOffset, sharedCellWidth, commentPrefix, emitStructDescription: false));
        }
        return lines;
    }

    /// <summary>
    /// Renders multiple types as a single string with consistent cell widths.
    /// </summary>
    public static string RenderListToString(ReadOnlySpan<Type> bitFieldsTypes, string? title = null, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        var lines = RenderList(bitFieldsTypes, title, bitsPerRow, includeDescriptions, showByteOffset, commentPrefix);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Renders multiple struct sections as a unified diagram list with consistent cell widths.
    /// The widest field name across all sections determines the scale for the entire output.
    /// </summary>
    /// <param name="sections">The labeled sections to render sequentially.</param>
    /// <param name="bitsPerRow">Number of bits per diagram row.</param>
    /// <param name="includeDescriptions">When true, appends field description legends.</param>
    /// <param name="showByteOffset">When true, shows hex byte offsets at the left.</param>
    /// <param name="commentPrefix">When non-null, prepended to every output line (e.g., <c>"// "</c> or <c>"/// "</c>).</param>
    /// <returns>A list of strings, one per output line.</returns>
    [Obsolete("Use RenderList(bitsPerRow, includeDescriptions, showByteOffset, commentPrefix, params Type[]) instead. Set Description on [BitFields]/[BitFieldsView] attributes to provide section labels.")]
    public static List<string> RenderList(ReadOnlySpan<DiagramSection> sections, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        if (sections.Length == 0) return [FormatLine("(no sections)", commentPrefix)];
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
                if (lines.Count > 0) lines.Add(FormatLine("", commentPrefix));
                lines.Add(FormatLine(section.Label, commentPrefix));
            }
            lines.AddRange(Render(section.Fields, null, bitsPerRow, includeDescriptions, showByteOffset, sharedCellWidth, commentPrefix));
        }
        return lines;
    }

    /// <summary>
    /// Renders multiple struct sections as a single string with consistent cell widths.
    /// </summary>
    [Obsolete("Use RenderListToString(bitsPerRow, includeDescriptions, showByteOffset, commentPrefix, params Type[]) instead.")]
    public static string RenderListToString(ReadOnlySpan<DiagramSection> sections, int bitsPerRow = 32, bool includeDescriptions = false, bool showByteOffset = true, string? commentPrefix = null)
    {
        var lines = RenderList(sections, bitsPerRow, includeDescriptions, showByteOffset, commentPrefix);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Retrieves the <c>Fields</c> metadata from a <c>[BitFields]</c> or <c>[BitFieldsView]</c> type.
    /// </summary>
    /// <param name="bitFieldsType">A struct type decorated with <c>[BitFields]</c> or <c>[BitFieldsView]</c>.</param>
    /// <returns>A successful result containing the field metadata array, or an error string on failure.</returns>
    [Obsolete("Use the extension method type.GetFieldInfo() from Stardust.Utilities.Extensions instead.")]
    public static Result<BitFieldInfo[], string> GetFieldInfo(Type bitFieldsType) =>
        bitFieldsType.GetFieldInfo();

    /// <summary>
    /// Formats the specified string by prepending an optional prefix to each line.
    /// </summary>
    /// <remarks>Each line in the input string is processed individually. If the prefix parameter is null,
    /// lines are returned without any prefix.</remarks>
    /// <param name="line">The string to be formatted. May contain multiple lines separated by newline characters.</param>
    /// <param name="prefix">An optional prefix to prepend to each line. If null, no prefix is added.</param>
    /// <returns>A formatted string in which each line is prefixed by the specified prefix, if provided.</returns>
    private static string FormatLine(string line, string? prefix)
    {
        string[] lines = line.Split(Environment.NewLine);
        var sb = new StringBuilder();
        bool first = true;
        foreach (var ln in lines)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.AppendLine(ln);
            }
            if (prefix != null)
            {
                sb.Append(prefix);
            }
            sb.Append(ln);
        }
        return sb.ToString();
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

    /// <summary>
    /// Builds a separator where bit positions covered by the overlay layer use dashed lines
    /// (alternating dash-space) and uncovered positions use solid dashes.
    /// </summary>
    private static string BuildHybridSeparator(int cols, int cellWidth, BitFieldInfo?[] overlayMap, int rowStartBit, bool reversed, int maxBit)
    {
        var sb = new StringBuilder(1 + cols * cellWidth);
        sb.Append('+');
        for (int col = 0; col < cols; col++)
        {
            int bitPos = reversed
                ? rowStartBit + cols - 1 - col
                : rowStartBit + col;
            bool isDashed = bitPos <= maxBit && bitPos < overlayMap.Length && overlayMap[bitPos] != null;
            if (isDashed)
            {
                for (int c = 0; c < cellWidth - 1; c++)
                    sb.Append(c % 2 == 0 ? '-' : ' ');
            }
            else
            {
                sb.Append('-', cellWidth - 1);
            }
            sb.Append('+');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Assigns each field to the lowest available layer where none of its bits are already claimed.
    /// Layer 0 holds non-overlapping fields (first-writer-wins); displaced fields go to higher layers.
    /// </summary>
    private static List<BitFieldInfo?[]> BuildLayers(ReadOnlySpan<BitFieldInfo> fields, int mapSize)
    {
        var layers = new List<BitFieldInfo?[]> { new BitFieldInfo?[mapSize] };
        foreach (var f in fields)
        {
            int targetLayer = -1;
            for (int li = 0; li < layers.Count; li++)
            {
                bool canFit = true;
                for (int b = f.StartBit; b <= f.EndBit && b < mapSize; b++)
                {
                    if (layers[li][b] != null)
                    {
                        canFit = false;
                        break;
                    }
                }
                if (canFit)
                {
                    targetLayer = li;
                    break;
                }
            }
            if (targetLayer < 0)
            {
                targetLayer = layers.Count;
                layers.Add(new BitFieldInfo?[mapSize]);
            }
            for (int b = f.StartBit; b <= f.EndBit && b < mapSize; b++)
                layers[targetLayer][b] = f;
        }
        return layers;
    }
}
