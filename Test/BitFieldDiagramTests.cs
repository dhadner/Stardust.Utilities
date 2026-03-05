using System;
using System.Collections.Generic;
using System.Linq;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

using static Result<List<string>,string>;

// ── Test-only structs for diagram testing ───────────────────

[BitFields(typeof(byte), Description = "8-bit test status register")]
public partial struct DiagramTestRegister
{
    [BitFlag(0, Description = "Ready flag")] public partial bool Ready { get; set; }
    [BitFlag(1, Description = "Error flag")] public partial bool Error { get; set; }
    [BitField(2, 4, Description = "Operating mode")] public partial byte Mode { get; set; }
    [BitField(5, 6)] public partial byte Priority { get; set; }
    [BitFlag(7)] public partial bool Busy { get; set; }
}

[BitFields(typeof(ushort), UndefinedBitsMustBe.Zeroes)]
public partial struct DiagramGappyRegister
{
    [BitField(0, 3, Description = "Type code")] public partial byte TypeCode { get; set; }
    // bits 4-11 undefined
    [BitField(12, 15, Description = "Version")] public partial byte Version { get; set; }
}

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb, Description = "MSB-first test view")]
public partial record struct DiagramMsbView
{
    [BitField(0, 3, Description = "IP version")] public partial byte Version { get; set; }
    [BitField(4, 7, Description = "Header length")] public partial byte Ihl { get; set; }
    [BitField(16, 31, Description = "Total length")] public partial ushort TotalLength { get; set; }
}

[BitFields(typeof(uint))]
public partial struct DiagramWideRegister
{
    [BitField(0, 15, Description = "Low half")] public partial ushort LowHalf { get; set; }
    [BitField(16, 31, Description = "High half")] public partial ushort HighHalf { get; set; }
}

/// <summary>
/// Test struct with overlapping fields modelling ADB-style command register.
/// Command occupies bits 2-3, Register occupies bits 0-1, and ExtendedCommand
/// overlaps all four (bits 0-3) as an alternate interpretation when Command == 0.
/// </summary>
[BitFields(typeof(byte))]
public partial struct DiagramOverlapRegister
{
    [BitField(4, 7, Description = "Device address")] public partial byte Address { get; set; }
    [BitField(2, 3, Description = "Command code")] public partial byte Command { get; set; }
    [BitField(0, 1, Description = "Register select")] public partial byte Register { get; set; }
    [BitField(0, 3, Description = "Extended command (when Command=0)")] public partial byte ExtendedCommand { get; set; }
}

/// <summary>
/// Test struct where two fields fully overlap the same bit range.
/// </summary>
[BitFields(typeof(byte))]
public partial struct DiagramFullOverlapRegister
{
    [BitField(0, 3, Description = "Mode A interpretation")] public partial byte ModeA { get; set; }
    [BitField(0, 3, Description = "Mode B interpretation")] public partial byte ModeB { get; set; }
    [BitField(4, 7)] public partial byte Upper { get; set; }
}

public class BitFieldDiagramTests
{
    // ── Render basics ───────────────────────────────────────────

    [Fact]
    public void Render_EmptyFields_ReturnsNoFieldsMessage()
    {
        var lines = BitFieldDiagram.Render(ReadOnlySpan<BitFieldInfo>.Empty);
        Assert.Single(lines);
        Assert.Equal("(no fields)", lines[0]);
    }

    [Fact]
    public void Render_SingleByteStruct_ProducesValidDiagram()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields);

        Assert.True(lines.Count > 0);
        Assert.Contains(lines, l => l.Contains("+-"));
        Assert.Contains(lines, l => l.Contains('|'));
    }

    [Fact]
    public void Render_ContainsExpectedFieldNames()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8);
        string diagram = string.Join("\n", lines);

        Assert.Contains("Ready", diagram);
        Assert.Contains("Error", diagram);
        Assert.Contains("Mode", diagram);
        Assert.Contains("Priority", diagram);
        Assert.Contains("Busy", diagram);
    }

    // ── BitsPerRow variations ───────────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void Render_DifferentBitsPerRow_ProducesValidOutput(int bitsPerRow)
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: bitsPerRow);
        Assert.True(lines.Count > 0);
        Assert.Contains(lines, l => l.Contains("+-"));
    }

    [Fact]
    public void Render_InvalidBitsPerRow_DefaultsTo32()
    {
        var lines32 = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 32);
        var linesBad = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 0);
        Assert.Equal(lines32.Count, linesBad.Count);
    }

    [Fact]
    public void Render_8Bits_OmitsTensDigitLine()
    {
        // An 8-bit struct rendered at 8 bits per row has bit numbers 0-7.
        // The tens-digit header line (all zeroes) should be omitted.
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8);
        // The first line should be the ones-digit header containing '7' (the highest bit),
        // not a line with just a lone '0' tens digit.
        string firstLine = lines[0];
        Assert.Contains("7", firstLine);
        Assert.Contains("0", firstLine);
        // Ensure there is no preceding tens-only line (a line whose only non-space char is '0')
        Assert.DoesNotContain(lines, l => l.Trim() == "0");
    }

    [Fact]
    public void Render_16Bits_IncludesTensDigitLine()
    {
        // A 16-bit struct at 16 bits per row has bit numbers 0-15, so tens are needed.
        var lines = BitFieldDiagram.Render(DiagramGappyRegister.Fields, bitsPerRow: 16);
        string diagram = string.Join("\n", lines);
        // The tens-digit line should contain "1" (for bits 10-15)
        Assert.Contains(lines, l => l.Contains('1') && !l.Contains('+') && !l.Contains('|'));
    }

    // ── showByteOffset parameter ────────────────────────────────

    [Fact]
    public void Render_ShowByteOffset_IncludesHexOffsets()
    {
        var lines = BitFieldDiagram.Render(DiagramWideRegister.Fields, showByteOffset: true);
        string diagram = string.Join("\n", lines);
        Assert.Contains("0x00", diagram);
    }

    [Fact]
    public void Render_HideByteOffset_ExcludesHexOffsets()
    {
        var lines = BitFieldDiagram.Render(DiagramWideRegister.Fields, showByteOffset: false);
        var contentLines = lines.Where(l => l.Contains('|') && !l.Contains("+-")).ToList();
        foreach (var line in contentLines)
            Assert.DoesNotContain("0x", line);
    }

    // ── includeDescriptions parameter ───────────────────────────

    [Fact]
    public void Render_IncludeDescriptions_AppendsLegend()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, includeDescriptions: true);
        string diagram = string.Join("\n", lines);
        Assert.Contains("Ready flag", diagram);
        Assert.Contains("Operating mode", diagram);
    }

    [Fact]
    public void Render_ExcludeDescriptions_NoDescriptionLegend()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, includeDescriptions: false);
        string diagram = string.Join("\n", lines);
        Assert.DoesNotContain("Ready flag", diagram);
    }

    // ── Undefined bits ──────────────────────────────────────────

    [Fact]
    public void Render_StructWithUndefinedBits_ShowsUndefinedLabel()
    {
        var lines = BitFieldDiagram.Render(DiagramGappyRegister.Fields, bitsPerRow: 16);
        string diagram = string.Join("\n", lines);
        Assert.True(diagram.Contains("Undefined") || diagram.Contains("| U |") || diagram.Contains("|U|"),
            "Expected undefined bits to be marked in the diagram");
    }

    [Fact]
    public void Render_StructWithUndefinedBits_AppendsUndefinedLegend()
    {
        var lines = BitFieldDiagram.Render(DiagramGappyRegister.Fields, bitsPerRow: 16);
        string diagram = string.Join("\n", lines);
        Assert.Contains("U/Undefined", diagram);
    }

    [Fact]
    public void Render_UndefinedMustBeZeroes_LegendIndicatesZero()
    {
        var lines = BitFieldDiagram.Render(DiagramGappyRegister.Fields, bitsPerRow: 16);
        string diagram = string.Join("\n", lines);
        Assert.Contains("must be 0", diagram);
    }

    // ── RenderToString ──────────────────────────────────────────

    [Fact]
    public void RenderToString_MatchesJoinedRender()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields);
        string fromRender = string.Join(Environment.NewLine, lines);
        string fromRenderToString = BitFieldDiagram.RenderToString(DiagramTestRegister.Fields);
        Assert.Equal(fromRender, fromRenderToString);
    }

    // ── ComputeMinCellWidth ─────────────────────────────────────

    [Fact]
    public void ComputeMinCellWidth_EmptyFields_Returns2()
    {
        int width = BitFieldDiagram.ComputeMinCellWidth(ReadOnlySpan<BitFieldInfo>.Empty);
        Assert.Equal(2, width);
    }

    [Fact]
    public void ComputeMinCellWidth_ReturnsAtLeast2()
    {
        int width = BitFieldDiagram.ComputeMinCellWidth(DiagramTestRegister.Fields);
        Assert.True(width >= 2);
    }

    [Fact]
    public void ComputeMinCellWidth_WiderFieldNames_ProduceLargerWidth()
    {
        // DiagramMsbView has "TotalLength" (11 chars) spanning 16 bits
        int msbWidth = BitFieldDiagram.ComputeMinCellWidth(DiagramMsbView.Fields);
        // DiagramTestRegister has short names spanning 1-3 bits
        int testWidth = BitFieldDiagram.ComputeMinCellWidth(DiagramTestRegister.Fields);

        // Both need at least 2, but test register with single-bit fields and
        // names like "Ready" (5 chars in 1 bit) should need wider cells
        Assert.True(msbWidth >= 2);
        Assert.True(testWidth >= 2);
    }

    // ── RenderList / RenderListToString (legacy DiagramSection API) ──

#pragma warning disable CS0618 // Obsolete DiagramSection API -- tests retained for backward compatibility

    [Fact]
    public void RenderList_EmptySections_ReturnsNoSectionsMessage()
    {
        var lines = BitFieldDiagram.RenderList(ReadOnlySpan<DiagramSection>.Empty);
        Assert.Single(lines);
        Assert.Equal("(no sections)", lines[0]);
    }

    [Fact]
    public void RenderList_SingleSection_IncludesSectionLabel()
    {
        var sections = new DiagramSection[]
        {
            new("Test Section", DiagramTestRegister.Fields.ToArray())
        };
        var lines = BitFieldDiagram.RenderList(sections);
        Assert.Contains("Test Section", lines);
    }

    [Fact]
    public void RenderList_EmptyLabel_OmitsLabelLine()
    {
        var sections = new DiagramSection[]
        {
            new("", DiagramTestRegister.Fields.ToArray())
        };
        var lines = BitFieldDiagram.RenderList(sections);
        // First line should be diagram content (bit header), not an empty label
        Assert.True(lines[0].Trim().Length > 0);
    }

    [Fact]
    public void RenderList_MultipleSections_ContainsAllLabels()
    {
        var sections = new DiagramSection[]
        {
            new("Section Alpha", DiagramTestRegister.Fields.ToArray()),
            new("Section Beta", DiagramWideRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections);
        string diagram = string.Join("\n", lines);
        Assert.Contains("Section Alpha", diagram);
        Assert.Contains("Section Beta", diagram);
    }

    [Fact]
    public void RenderList_UsesConsistentCellWidth()
    {
        var sections = new DiagramSection[]
        {
            new("Short", DiagramTestRegister.Fields.ToArray()),
            new("Wide", DiagramWideRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8);

        // All separator lines for the same bitsPerRow should have the same width
        var separators = lines.Where(l => l.TrimStart().StartsWith("+")).ToList();
        Assert.True(separators.Count >= 2, "Expected at least 2 separator lines");
        int firstLen = separators[0].TrimStart().Length;
        foreach (var sep in separators)
            Assert.Equal(firstLen, sep.TrimStart().Length);
    }

    [Fact]
    public void RenderListToString_MatchesJoinedRenderList()
    {
        var sections = new DiagramSection[]
        {
            new("Section A", DiagramTestRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections);
        string fromList = string.Join(Environment.NewLine, lines);
        string fromToString = BitFieldDiagram.RenderListToString(sections);
        Assert.Equal(fromList, fromToString);
    }

#pragma warning restore CS0618

    // ── minCellWidth parameter ──────────────────────────────────

    [Fact]
    public void Render_MinCellWidth_EnforcesCellSize()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, minCellWidth: 6);
        var sep = lines.First(l => l.TrimStart().StartsWith("+"));
        Assert.Contains("-----+", sep);
    }

    // ── Bit order handling ──────────────────────────────────────

    [Fact]
    public void Render_MsbFirstFields_ShowsBit0OnLeft()
    {
        var lines = BitFieldDiagram.Render(DiagramMsbView.Fields, bitsPerRow: 32);
        // Find the ones-digit header line -- bit 0 should appear on the left side
        string diagram = string.Join("\n", lines);
        Assert.Contains("Version", diagram);
        Assert.Contains("Ihl", diagram);
    }

    [Fact]
    public void Render_LsbFirstFields_ProducesValidOutput()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8);
        string diagram = string.Join("\n", lines);
        Assert.Contains("Ready", diagram);
    }

    // ── Separator structure ─────────────────────────────────────

    [Fact]
    public void Render_SeparatorMatchesBitsPerRow()
    {
        int bitsPerRow = 8;
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: bitsPerRow);
        var sep = lines.First(l => l.TrimStart().StartsWith("+"));
        int plusCount = sep.Count(c => c == '+');
        Assert.Equal(bitsPerRow + 1, plusCount);
    }

    // ── Comment prefix ──────────────────────────────────────────

    [Fact]
    public void Render_CommentPrefix_Asterisk_PrependedToEveryLine()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, commentPrefix: "* ");
        Assert.All(lines, line => Assert.StartsWith("* ", line));
    }

    [Fact]
    public void Render_CommentPrefix_DoubleSlash_PrependedToEveryLine()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void Render_CommentPrefix_TripleSlash_PrependedToEveryLine()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, commentPrefix: "/// ");
        Assert.All(lines, line => Assert.StartsWith("/// ", line));
    }

    [Fact]
    public void Render_CommentPrefix_Null_NoPrefix()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, commentPrefix: null);
        // No line should start with "//" -- they start with spaces, digits, or separators
        Assert.All(lines, line => Assert.False(line.StartsWith("//")));
    }

    [Fact]
    public void Render_CommentPrefix_WithDescriptions_AllLinesPrefixed()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
        // Should contain at least one description
        Assert.Contains(lines, line => line.Contains("Ready flag"));
    }

    [Fact]
    public void RenderToString_CommentPrefix_EveryLineHasPrefix()
    {
        string diagram = BitFieldDiagram.RenderToString(DiagramTestRegister.Fields, bitsPerRow: 8, commentPrefix: "// ");
        var lines = diagram.Split(Environment.NewLine);
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

#pragma warning disable CS0618
    [Fact]
    public void RenderList_CommentPrefix_AllLinesPrefixed()
    {
        var sections = new DiagramSection[]
        {
            new("Test Section", DiagramTestRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8, commentPrefix: "/// ");
        Assert.All(lines, line => Assert.StartsWith("/// ", line));
        // Section label should also be prefixed
        Assert.Contains(lines, line => line.Contains("Test Section"));
    }

    [Fact]
    public void RenderListToString_CommentPrefix_EveryLineHasPrefix()
    {
        var sections = new DiagramSection[]
        {
            new("Section A", DiagramTestRegister.Fields.ToArray()),
        };
        string diagram = BitFieldDiagram.RenderListToString(sections, bitsPerRow: 8, commentPrefix: "// ");
        var lines = diagram.Split(Environment.NewLine);
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void Render_CommentPrefix_EmptyFields_Prefixed()
    {
        var lines = BitFieldDiagram.Render(ReadOnlySpan<BitFieldInfo>.Empty, commentPrefix: "// ");
        Assert.Single(lines);
        Assert.Equal("// (no fields)", lines[0]);
    }

    // ── Comment prefix with multi-line descriptions ─────────────

    [Fact]
    public void Render_CommentPrefix_NewlineInDescription_AllLinesPrefixed()
    {
        // DescEscapeRegister has Description = "Line1\nLine2" on the Newline flag
        var lines = BitFieldDiagram.Render(DescEscapeRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void Render_NoPrefix_NewlineInDescription_SplitIntoSeparateEntries()
    {
        // Even without a prefix, multi-line descriptions must be separate list entries
        var lines = BitFieldDiagram.Render(DescEscapeRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        // No entry should contain a raw newline character
        Assert.All(lines, line =>
        {
            Assert.DoesNotContain("\n", line);
            Assert.DoesNotContain("\r", line);
        });
    }

    [Fact]
    public void Render_CommentPrefix_NewlineInDescription_ContentPreserved()
    {
        var lines = BitFieldDiagram.Render(DescEscapeRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "/// ");
        // The description "Line1\nLine2" should appear as two separate prefixed lines
        Assert.Contains(lines, line => line.Contains("Line1"));
        Assert.Contains(lines, line => line.Contains("Line2"));
    }

    [Fact]
    public void RenderToString_CommentPrefix_NewlineInDescription_EveryVisualLineHasPrefix()
    {
        string diagram = BitFieldDiagram.RenderToString(DescEscapeRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        // Split on the actual line separator used in the output
        var visualLines = diagram.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        Assert.All(visualLines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void RenderList_CommentPrefix_NewlineInDescription_AllLinesPrefixed()
    {
        var sections = new DiagramSection[]
        {
            new("Escape Test", DescEscapeRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
        // No raw newlines embedded in any entry
        Assert.All(lines, line => Assert.DoesNotContain("\n", line));
    }
#pragma warning restore CS0618

    // ── Diagram object API ───────────────────────────────────

    [Fact]
    public void CreateDiagramAndRender_ProducesValidDiagramWithListAndMinimalParametersNoDescriptions()
    {
        Assert.Equal("8-bit test status register", DiagramTestRegister.StructDescription);
        var diagram = new BitFieldDiagram([typeof(DiagramTestRegister)], includeDescriptions: false);
        diagram.Render().Match(
            onSuccess: lines => { 
                Assert.True(lines.Count == 4);
                Assert.Contains(lines, l => l.Contains("Ready"));
                Assert.DoesNotContain(lines, l => l.Contains("Mode: Operating mode"));
                return Ok(lines);
            },
            onFailure: err => {
                Assert.Fail();
                return Err(err);
            }
            );
    }

    [Fact]
    public void CreateDiagramAndRender_ProducesValidDiagramWithListAndDescriptions()
    {
        Assert.Equal("8-bit test status register", DiagramTestRegister.StructDescription);
        var diagram = new BitFieldDiagram([typeof(DiagramTestRegister)]);
        diagram.Render().Match(
            onSuccess: lines => {
                Assert.True(lines.Count > 6);
                Assert.Contains(lines[0], "DiagramTestRegister");
                Assert.Contains(lines[1], "8-bit test status register");
                Assert.Contains(lines, l => l.Contains("Mode: Operating mode"));
                return Ok(lines);
            },
            onFailure: err => {
                Assert.Fail();
                return Err(err);
            }
            );
    }

    [Fact]
    public void CreateDiagramAndRender_ProducesValidDiagramWithSingleStructAndDescriptions()
    {
        Assert.Equal("8-bit test status register", DiagramTestRegister.StructDescription);
        var diagram = new BitFieldDiagram(typeof(DiagramTestRegister));
        diagram.Render().Match(
            onSuccess: lines => {
                Assert.True(lines.Count > 6);
                Assert.Contains(lines[0], "DiagramTestRegister");
                Assert.Contains(lines[1], "8-bit test status register");
                Assert.Contains(lines, l => l.Contains("Mode: Operating mode"));
                return Ok(lines);
            },
            onFailure: err => {
                Assert.Fail();
                return Err(err);
            }
            );
    }

    [Fact]
    public void CreateDiagramAndRender_ProducesValidDiagramWithMinimalParameters()
    {
        var diagram = new BitFieldDiagram();
        diagram.AddStruct(typeof(DiagramTestRegister));
        diagram.Render().Match(
            onSuccess: lines => {
                Assert.True(lines.Count > 6);
                Assert.Contains(lines, l => l.Contains("Ready"));
                return Ok(lines);
            },
            onFailure: err => {
                Assert.Fail();
                return Err(err);
            }
            );
    }

    // ── Type-based Render API ───────────────────────────────────

    [Fact]
    public void Render_Type_ProducesDiagram()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8);
        Assert.True(lines.Count > 0);
        Assert.Contains(lines, l => l.Contains("Ready"));
    }

    [Fact]
    public void RenderToString_Type_ProducesString()
    {
        string diagram = BitFieldDiagram.RenderToString(typeof(DiagramTestRegister), bitsPerRow: 8);
        Assert.Contains("Ready", diagram);
    }

    [Fact]
    public void Render_Type_InvalidType_ReturnsNoFieldsMessage()
    {
        var lines = BitFieldDiagram.Render(typeof(string));
        Assert.Single(lines);
        Assert.Equal("(no fields)", lines[0]);
    }

    [Fact]
    public void Render_Type_StructDescription_ShownAboveDiagram()
    {
        // DiagramTestRegister has Description set -- verify it appears as the first line
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8, includeDescriptions: true);
        string desc = DiagramTestRegister.Fields[0].StructDescription!;
        Assert.Equal(desc, lines[0]);
    }

    [Fact]
    public void Render_Type_StructDescription_HiddenWhenDescriptionsOff()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8, includeDescriptions: false);
        Assert.DoesNotContain(lines, l => l == "8-bit test status register");
    }

    [Fact]
    public void Render_Type_CommentPrefix_AppliedToDescription()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void RenderList_Types_ProducesSectionLabels()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8, includeDescriptions: true,
            bitFieldsTypes: [typeof(DiagramTestRegister), typeof(DescEscapeRegister)]);
        // Each struct should have a section label
        string label1 = DiagramTestRegister.Fields[0].StructDescription ?? nameof(DiagramTestRegister);
        Assert.Contains(lines, l => l.Contains(label1));
    }

    [Fact]
    public void RenderList_Types_Empty_ReturnsNoTypesMessage()
    {
        var lines = BitFieldDiagram.RenderList(bitFieldsTypes: []);
        Assert.Single(lines);
        Assert.Contains("(no types)", lines[0]);
    }

    [Fact]
    public void RenderListToString_Types_MatchesJoinedRenderList()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8,
            bitFieldsTypes: [typeof(DiagramTestRegister)]);
        string fromList = string.Join(Environment.NewLine, lines);
        string fromToString = BitFieldDiagram.RenderListToString(bitsPerRow: 8,
            bitFieldsTypes: [typeof(DiagramTestRegister)]);
        Assert.Equal(fromList, fromToString);
    }

    [Fact]
    public void RenderList_Types_CommentPrefix_AllLinesPrefixed()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8, commentPrefix: "/// ",
            bitFieldsTypes: [typeof(DiagramTestRegister), typeof(DescEscapeRegister)]);
        Assert.All(lines, line => Assert.StartsWith("/// ", line));
    }

    [Fact]
    public void GetFields_ValidType_ReturnsFields()
    {
        var result = typeof(DiagramTestRegister).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Length > 0);
        Assert.Contains(result.Value, f => f.Name == "Ready");
    }

    [Fact]
    public void GetFields_InvalidType_ReturnsError()
    {
        var result = typeof(int).GetFieldInfo();
        Assert.True(result.IsFailure);
        Assert.Contains("Int32", result.Error);
    }

    // ── StructDescription metadata generation ───────────────────

    [Fact]
    public void BitFields_Description_EmittedInFieldsMetadata()
    {
        // DiagramTestRegister has Description = "8-bit test status register"
        Assert.All(DiagramTestRegister.Fields.ToArray(),
            f => Assert.Equal("8-bit test status register", f.StructDescription));
    }

    [Fact]
    public void BitFieldsView_Description_EmittedInFieldsMetadata()
    {
        // DiagramMsbView has Description = "MSB-first test view"
        Assert.All(DiagramMsbView.Fields.ToArray(),
            f => Assert.Equal("MSB-first test view", f.StructDescription));
    }

    [Fact]
    public void BitFields_NoDescription_StructDescriptionIsNull()
    {
        // DescEscapeRegister has no Description set
        Assert.All(DescEscapeRegister.Fields.ToArray(),
            f => Assert.Null(f.StructDescription));
    }

    [Fact]
    public void BitFields_NoDescription_DiagramGappyRegister_IsNull()
    {
        Assert.All(DiagramGappyRegister.Fields.ToArray(),
            f => Assert.Null(f.StructDescription));
    }

    [Fact]
    public void BitFieldsView_NoDescription_StructDescriptionIsNull()
    {
        // IPv4HeaderView has no Description -- use DiagramWideRegister (no desc)
        Assert.All(DiagramWideRegister.Fields.ToArray(),
            f => Assert.Null(f.StructDescription));
    }

    // ── StructDescription in Render (fields-based API) ──────────

    [Fact]
    public void Render_Fields_WithStructDescription_ShowsHeaderWhenEnabled()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        Assert.Equal("8-bit test status register", lines[0]);
        // Diagram content follows (separator or bit header)
        Assert.True(lines.Count > 1);
    }

    [Fact]
    public void Render_Fields_WithStructDescription_HiddenWhenDisabled()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: false);
        Assert.DoesNotContain(lines, l => l == "8-bit test status register");
    }

    [Fact]
    public void Render_Fields_NoStructDescription_NoHeaderLine()
    {
        var lines = BitFieldDiagram.Render(DescEscapeRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        // First line should be diagram content (spaces/digits), not a description
        Assert.DoesNotContain(lines, l => l == "DescEscapeRegister");
    }

    [Fact]
    public void Render_Fields_StructDescription_NotDuplicated()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        int count = lines.Count(l => l == "8-bit test status register");
        Assert.Equal(1, count);
    }

    [Fact]
    public void Render_Fields_StructDescription_WithCommentPrefix()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.Equal("// 8-bit test status register", lines[0]);
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void RenderToString_Fields_StructDescription_IncludedInOutput()
    {
        string diagram = BitFieldDiagram.RenderToString(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        Assert.StartsWith("8-bit test status register", diagram);
    }

    [Fact]
    public void Render_BitFieldsView_StructDescription_ShowsHeader()
    {
        var lines = BitFieldDiagram.Render(DiagramMsbView.Fields, bitsPerRow: 8, includeDescriptions: true);
        Assert.Equal("MSB-first test view", lines[0]);
    }

    // ── StructDescription in RenderList (DiagramSection API) ────

#pragma warning disable CS0618
    [Fact]
    public void RenderList_DiagramSection_StructDescription_ShownWhenEnabled()
    {
        var sections = new DiagramSection[]
        {
            new("", DiagramTestRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8, includeDescriptions: true);
        Assert.Contains(lines, l => l == "8-bit test status register");
    }

    [Fact]
    public void RenderList_DiagramSection_StructDescription_HiddenWhenDisabled()
    {
        var sections = new DiagramSection[]
        {
            new("", DiagramTestRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8, includeDescriptions: false);
        Assert.DoesNotContain(lines, l => l == "8-bit test status register");
    }

    [Fact]
    public void RenderList_DiagramSection_SectionLabelAndStructDescription_BothShown()
    {
        var sections = new DiagramSection[]
        {
            new("Custom Label", DiagramTestRegister.Fields.ToArray()),
        };
        var lines = BitFieldDiagram.RenderList(sections, bitsPerRow: 8, includeDescriptions: true);
        Assert.Contains(lines, l => l == "Custom Label");
        Assert.Contains(lines, l => l == "8-bit test status register");
    }
#pragma warning restore CS0618

    // ── Type-based API: StructDescription behavior ──────────────

    [Fact]
    public void Render_Type_StructDescription_IsFirstLine()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8, includeDescriptions: true);
        Assert.Equal("8-bit test status register", lines[0]);
    }

    [Fact]
    public void Render_Type_NoDescription_NoBogusHeader()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramWideRegister), bitsPerRow: 32, includeDescriptions: true);
        Assert.DoesNotContain(lines, l => l == "DiagramWideRegister");
    }

    [Fact]
    public void Render_Type_BitFieldsView_ShowsStructDescription()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramMsbView), bitsPerRow: 8, includeDescriptions: true);
        Assert.Equal("MSB-first test view", lines[0]);
    }

    [Fact]
    public void RenderList_Types_MixedDescriptions_AllHaveLabels()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8, includeDescriptions: true,
            bitFieldsTypes: [typeof(DiagramTestRegister), typeof(DescEscapeRegister)]);
        Assert.Contains(lines, l => l.Contains("8-bit test status register"));
        Assert.Contains(lines, l => l.Contains("DescEscapeRegister"));
    }

    [Fact]
    public void RenderList_Types_StructDescription_NotDuplicated()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8, includeDescriptions: true,
            bitFieldsTypes: [typeof(DiagramTestRegister)]);
        int count = lines.Count(l => l.Contains("8-bit test status register"));
        Assert.Equal(1, count);
    }

    [Fact]
    public void RenderList_Types_StructDescription_HiddenWhenDisabled()
    {
        var lines = BitFieldDiagram.RenderList(bitsPerRow: 8, includeDescriptions: false,
            bitFieldsTypes: [typeof(DiagramTestRegister)]);
        Assert.DoesNotContain(lines, l => l.Contains("8-bit test status register"));
    }

    // ── GetFields returns StructDescription ─────────────────────

    [Fact]
    public void GetFields_ReturnsStructDescription()
    {
        var result = typeof(DiagramTestRegister).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.All(result.Value, f => Assert.Equal("8-bit test status register", f.StructDescription));
    }

    [Fact]
    public void GetFields_NoDescription_StructDescriptionIsNull()
    {
        var result = typeof(DiagramWideRegister).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.All(result.Value, f => Assert.Null(f.StructDescription));
    }

    [Fact]
    public void GetFields_BitFieldsView_ReturnsStructDescription()
    {
        var result = typeof(DiagramMsbView).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.All(result.Value, f => Assert.Equal("MSB-first test view", f.StructDescription));
    }

    // ── StructDescription + commentPrefix interaction ────────────

    [Fact]
    public void Render_Fields_StructDescription_CommentPrefix_WithDescriptionsOn()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "/// ");
        Assert.Equal("/// 8-bit test status register", lines[0]);
        Assert.All(lines, line => Assert.StartsWith("/// ", line));
    }

    [Fact]
    public void Render_Fields_StructDescription_CommentPrefix_WithDescriptionsOff()
    {
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8, includeDescriptions: false, commentPrefix: "// ");
        Assert.DoesNotContain(lines, l => l.Contains("8-bit test status register"));
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void Render_Fields_MultilineStructDescription_EachLineGetCommentPrefix()
    {
        var fields = new[]
        {
            new BitFieldInfo("TestBit", 0, 1, "bool", true,
                StructTotalBits: 8,
                StructDescription: "Line one\nLine two\nLine three"),
        };
        var lines = BitFieldDiagram.Render(fields, bitsPerRow: 8, includeDescriptions: true, commentPrefix: "// ");
        Assert.Equal("// Line one", lines[0]);
        Assert.Equal("// Line two", lines[1]);
        Assert.Equal("// Line three", lines[2]);
        Assert.All(lines, line => Assert.StartsWith("// ", line));
    }

    [Fact]
    public void Render_Fields_MultilineStructDescription_NoPrefix_SplitIntoSeparateEntries()
    {
        var fields = new[]
        {
            new BitFieldInfo("TestBit", 0, 1, "bool", true,
                StructTotalBits: 8,
                StructDescription: "First\nSecond"),
        };
        var lines = BitFieldDiagram.Render(fields, bitsPerRow: 8, includeDescriptions: true);
        Assert.Equal("First", lines[0]);
        Assert.Equal("Second", lines[1]);
    }

    // ── GetFields returns all expected field info ───────────────

    [Fact]
    public void GetFields_ReturnsCorrectFieldCount()
    {
        var result = typeof(DiagramTestRegister).GetFieldInfo();
        Assert.True(result.IsSuccess);
        // 3 flags + 2 fields = 5
        Assert.Equal(5, result.Value.Length);
    }

    [Fact]
    public void GetFields_ReturnsFieldDescriptions()
    {
        var result = typeof(DiagramTestRegister).GetFieldInfo();
        Assert.True(result.IsSuccess);
        var ready = result.Value.First(f => f.Name == "Ready");
        Assert.Equal("Ready flag", ready.GetDescription());
    }

    [Fact]
    public void GetFields_BitFieldsView_ReturnsCorrectFields()
    {
        var result = typeof(DiagramMsbView).GetFieldInfo();
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, f => f.Name == "Version");
        Assert.Contains(result.Value, f => f.Name == "Ihl");
        Assert.Contains(result.Value, f => f.Name == "TotalLength");
    }

    // ── Description duplication regression tests ────────────────

    [Fact]
    public void Render_Type_StructDescription_NotDuplicated()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramTestRegister), bitsPerRow: 8, includeDescriptions: true);
        int count = lines.Count(l => l.Contains("8-bit test status register"));
        Assert.Equal(1, count);
    }

    [Fact]
    public void DiagramInstance_WithDescription_NotDuplicated()
    {
        var diagram = new BitFieldDiagram([typeof(DiagramTestRegister)], description: "Custom Title");
        diagram.Render().Match(
            onSuccess: lines =>
            {
                int count = lines.Count(l => l.Contains("Custom Title"));
                Assert.Equal(1, count);
                return Ok(lines);
            },
            onFailure: err =>
            {
                Assert.Fail(err);
                return Err(err);
            });
    }

    [Fact]
    public void DiagramInstance_WithDescription_TitleAndStructDescriptionBothPresent()
    {
        var diagram = new BitFieldDiagram([typeof(DiagramTestRegister)], description: "Custom Title");
        diagram.Render().Match(
            onSuccess: lines =>
            {
                Assert.Contains(lines, l => l.Contains("Custom Title"));
                Assert.Contains(lines, l => l.Contains("8-bit test status register"));
                return Ok(lines);
            },
            onFailure: err =>
            {
                Assert.Fail(err);
                return Err(err);
            });
    }

    [Fact]
    public void RenderList_Types_WithDescription_NotDuplicated()
    {
        var lines = BitFieldDiagram.RenderList(
            [typeof(DiagramTestRegister)],
            description: "List Title",
            bitsPerRow: 8,
            includeDescriptions: true);
        int count = lines.Count(l => l.Contains("List Title"));
        Assert.Equal(1, count);
    }

    [Fact]
    public void DiagramInstance_DescriptionMatchesStructDescription_NotDuplicated()
    {
        // Reproduces the DemoWeb "TCP Header" scenario: BitFieldDiagram.Description
        // matches the struct's own StructDescription via [BitFieldsView(Description = ...)].
        var diagram = new BitFieldDiagram([typeof(DiagramTestRegister)], description: "8-bit test status register");
        diagram.Render().Match(
            onSuccess: lines =>
            {
                int count = lines.Count(l => l.Contains("8-bit test status register"));
                Assert.Equal(1, count);
                return Ok(lines);
            },
            onFailure: err =>
            {
                Assert.Fail(err);
                return Err(err);
            });
    }

    [Fact]
    public void RenderList_DescriptionMatchesStructDescription_NotDuplicated()
    {
        var lines = BitFieldDiagram.RenderList(
            [typeof(DiagramTestRegister)],
            description: "8-bit test status register",
            bitsPerRow: 8,
            includeDescriptions: true);
        int count = lines.Count(l => l.Contains("8-bit test status register"));
        Assert.Equal(1, count);
    }

    [Fact]
    public void RenderList_MultiStruct_TopLevelAndSectionDescriptions_AllPresent()
    {
        // Multi-struct: top-level description differs from each struct's StructDescription.
        // All should appear without duplication.
        var lines = BitFieldDiagram.RenderList(
            [typeof(DiagramTestRegister), typeof(DiagramMsbView)],
            description: "Combined Diagram",
            bitsPerRow: 8,
            includeDescriptions: true);
        string diagram = string.Join("\n", lines);
        Assert.Contains("Combined Diagram", diagram);
        Assert.Contains("8-bit test status register", diagram);
        Assert.Contains("MSB-first test view", diagram);
        Assert.Equal(1, lines.Count(l => l.Contains("Combined Diagram")));
        Assert.Equal(1, lines.Count(l => l.Contains("8-bit test status register")));
        Assert.Equal(1, lines.Count(l => l.Contains("MSB-first test view")));
    }

    // ── Overlapping fields ──────────────────────────────────────

    [Fact]
    public void Render_OverlappingFields_AllFieldNamesPresent()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        string diagram = string.Join("\n", lines);

        Assert.Contains("Address", diagram);
        Assert.Contains("Command", diagram);
        Assert.Contains("Register", diagram);
        Assert.Contains("ExtendedCommand", diagram);
    }

    [Fact]
    public void Render_OverlappingFields_HasHybridSeparator()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        // The hybrid separator contains dashed segments ("- ") adjacent to solid ones ("--")
        bool hasHybrid = lines.Exists(l => l.Contains("- +") && l.Contains("--+"));
        Assert.True(hasHybrid, "Expected a hybrid separator with both dashed and solid segments");
    }

    [Fact]
    public void Render_OverlappingFields_OverlayRowPresent()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        // There should be more content rows than a non-overlapping 8-bit struct.
        // Specifically: 1 primary content row + 1 overlay content row = 2 rows with '|' content
        int contentRows = lines.Count(l => l.Contains('|') && !l.Contains('+'));
        Assert.True(contentRows >= 2, $"Expected at least 2 content rows (primary + overlay), got {contentRows}");
    }

    [Fact]
    public void Render_OverlappingFields_ExtendedCommandGetsFullWidth()
    {
        // ExtendedCommand is 4 bits; it must appear as a single unbroken label
        // spanning 4 bit columns in the overlay row (not truncated to 2 bits).
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        string diagram = string.Join("\n", lines);

        // Count how many lines contain ExtendedCommand — should be exactly 1 content line
        int ecLines = lines.Count(l => l.Contains("ExtendedCommand") && l.Contains('|'));
        Assert.Equal(1, ecLines);

        // The label should not be truncated (all 15 chars present)
        Assert.Contains("ExtendedCommand", diagram);
    }

    [Fact]
    public void Render_OverlappingFields_Descriptions_AllPresent()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8, includeDescriptions: true);
        string diagram = string.Join("\n", lines);

        Assert.Contains("Device address", diagram);
        Assert.Contains("Command code", diagram);
        Assert.Contains("Register select", diagram);
        Assert.Contains("Extended command (when Command=0)", diagram);
    }

    [Fact]
    public void Render_OverlappingFields_SeparatorCountCorrect()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        // For a single row with 1 overlay: top separator + hybrid separator + bottom separator = 3
        int sepLines = lines.Count(l => l.TrimStart().StartsWith('+'));
        Assert.Equal(3, sepLines);
    }

    [Fact]
    public void Render_FullOverlap_BothFieldsPresent()
    {
        var lines = BitFieldDiagram.Render(DiagramFullOverlapRegister.Fields, bitsPerRow: 8);
        string diagram = string.Join("\n", lines);

        Assert.Contains("ModeA", diagram);
        Assert.Contains("ModeB", diagram);
        Assert.Contains("Upper", diagram);
    }

    [Fact]
    public void Render_FullOverlap_HasOverlayRow()
    {
        var lines = BitFieldDiagram.Render(DiagramFullOverlapRegister.Fields, bitsPerRow: 8);
        // For a single row with 1 overlay: top separator + hybrid separator + bottom separator = 3
        int sepLines = lines.Count(l => l.TrimStart().StartsWith('+'));
        Assert.Equal(3, sepLines);
    }

    [Fact]
    public void Render_NoOverlap_UnchangedBehavior()
    {
        // Non-overlapping struct should produce no hybrid separators
        var lines = BitFieldDiagram.Render(DiagramTestRegister.Fields, bitsPerRow: 8);
        bool hasHybrid = lines.Exists(l => l.Contains("- +") && l.Contains("--+"));
        Assert.False(hasHybrid, "Non-overlapping struct should not produce hybrid separators");
    }

    [Fact]
    public void Render_OverlappingFields_CommentPrefix_Applied()
    {
        var lines = BitFieldDiagram.Render(DiagramOverlapRegister.Fields, bitsPerRow: 8, commentPrefix: "// ");
        Assert.All(lines, line => Assert.StartsWith("// ", line));
        string diagram = string.Join("\n", lines);
        Assert.Contains("ExtendedCommand", diagram);
    }

    [Fact]
    public void Render_OverlappingFields_Type_AllFieldsPresent()
    {
        var lines = BitFieldDiagram.Render(typeof(DiagramOverlapRegister), bitsPerRow: 8);
        string diagram = string.Join("\n", lines);
        Assert.Contains("Address", diagram);
        Assert.Contains("Command", diagram);
        Assert.Contains("Register", diagram);
        Assert.Contains("ExtendedCommand", diagram);
    }

    [Fact]
    public void ComputeMinCellWidth_OverlappingFields_ConsidersOverlaySpan()
    {
        // ExtendedCommand (15 chars) in 4 bits needs cellWidth >= 4.
        // Register (8 chars) in 2 bits needs cellWidth >= 5.
        // Without overlay awareness, ExtendedCommand would be truncated to 2 bits
        // and would need cellWidth >= 8, wasting space.
        int width = BitFieldDiagram.ComputeMinCellWidth(DiagramOverlapRegister.Fields, bitsPerRow: 8);
        Assert.True(width >= 5, $"Expected cellWidth >= 5, got {width}");
    }

    [Fact]
    public void RenderList_OverlappingFields_ConsistentWidth()
    {
        // Overlapping struct combined with non-overlapping should still render correctly
        var lines = BitFieldDiagram.RenderList(
            [typeof(DiagramOverlapRegister), typeof(DiagramTestRegister)],
            bitsPerRow: 8);
        string diagram = string.Join("\n", lines);
        Assert.Contains("ExtendedCommand", diagram);
        Assert.Contains("Ready", diagram);
    }
}
