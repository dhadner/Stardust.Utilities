using System;
using System.Collections.Generic;

namespace Stardust.Generators;

/// <summary>
/// Describes a nested [BitFieldsView] property within another [BitFieldsView].
/// </summary>
internal sealed class SubViewInfo
{
    public string Name { get; }
    public string ViewTypeName { get; }
    public int StartBit { get; }
    public int EndBit { get; }

    /// <summary>Byte offset into the outer buffer (startBit / 8).</summary>
    public int ByteOffset => StartBit / 8;

    /// <summary>Bit offset within that byte (startBit % 8). Zero for byte-aligned sub-views.</summary>
    public int BitOffset => StartBit % 8;

    public SubViewInfo(string name, string viewTypeName, int startBit, int endBit)
    {
        Name = name;
        ViewTypeName = viewTypeName;
        StartBit = startBit;
        EndBit = endBit;
    }
}

/// <summary>
/// Describes a [BitFieldsView]-attributed record struct and its fields/flags.
/// </summary>
internal sealed class BitFieldsViewInfo
{
    public string TypeName { get; }
    public string? Namespace { get; }
    public string Accessibility { get; }
    public ByteOrder ByteOrder { get; }
    public BitOrder BitOrder { get; }
    public List<BitFieldInfo> Fields { get; }
    public List<BitFlagInfo> Flags { get; }
    public List<SubViewInfo> SubViews { get; }
    /// <summary>
    /// List of containing types from outermost to innermost.
    /// Each tuple contains (TypeKind, TypeName, Accessibility).
    /// </summary>
    public List<(string Kind, string Name, string Accessibility)> ContainingTypes { get; }

    /// <summary>
    /// Minimum number of bytes required in the backing buffer,
    /// computed from the highest bit position across all fields and flags.
    /// </summary>
    public int MinBytes { get; }

    /// <summary>Optional struct-level description from the [BitFieldsView] attribute.</summary>
    public string? Description { get; }

    /// <summary>
    /// An optional resource type for the Description property, allowing localization of struct descriptions in BitFieldDiagram.
    /// </summary>
    public Type? DescriptionResourceType { get; set; }

    public BitFieldsViewInfo(
        string typeName,
        string? ns,
        string accessibility,
        ByteOrder byteOrder,
        BitOrder bitOrder,
        List<BitFieldInfo> fields,
        List<BitFlagInfo> flags,
        List<SubViewInfo> subViews,
        List<(string Kind, string Name, string Accessibility)> containingTypes,
        int minBytes,
        string? description = null,
        Type? descriptionResourceType = null)
    {
        TypeName = typeName;
        Namespace = ns;
        Accessibility = accessibility;
        ByteOrder = byteOrder;
        BitOrder = bitOrder;
        Fields = fields;
        Flags = flags;
        SubViews = subViews;
        ContainingTypes = containingTypes;
        MinBytes = minBytes;
        Description = description;
        DescriptionResourceType = descriptionResourceType;
    }
}
