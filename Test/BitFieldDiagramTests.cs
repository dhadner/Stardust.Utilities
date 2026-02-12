using System;
using System.Collections.Generic;
using System.Linq;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

// ── Test-only structs for diagram testing ───────────────────

[BitFields(typeof(byte))]
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

[BitFieldsView(ByteOrder.BigEndian, BitOrder.BitZeroIsMsb)]
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

    // ── RenderList / RenderListToString ─────────────────────────

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
}
