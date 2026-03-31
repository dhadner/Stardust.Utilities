using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stardust.Generators;

/// <summary>
/// Describes a nested record struct view property within another record struct view.
/// </summary>
internal sealed class SubViewInfo
{
    public string Name { get; }
    public string ViewTypeName { get; }
    public int Start { get; }
    public int End { get; }

    /// <summary>Byte offset into the outer buffer (start / 8).</summary>
    public int ByteOffset => Start / 8;

    /// <summary>Bit offset within that byte (start % 8). Zero for byte-aligned sub-views.</summary>
    public int BitOffset => Start % 8;

    public SubViewInfo(string name, string viewTypeName, int start, int end)
    {
        Name = name;
        ViewTypeName = viewTypeName;
        Start = start;
        End = end;
    }
}

/// <summary>
/// Describes a <c>[BitFields]</c>-attributed record struct (view) and its fields/flags.
/// </summary>
internal sealed class RecordStructViewInfo
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

    /// <summary>Optional struct-level description from the <c>[BitFields]</c> attribute.</summary>
    public string? Description { get; }

    /// <summary>
    /// An optional resource type for the Description property, allowing localization of struct descriptions in BitFieldDiagram.
    /// </summary>
    public Type? DescriptionResourceType { get; set; }

    /// <summary>
    /// Properties that have [BitField] or [BitFlag] attributes but are missing the <c>partial</c> keyword.
    /// These are not added to <see cref="Fields"/> or <see cref="Flags"/> and are reported as SD0004 errors.
    /// </summary>
    public List<NonPartialPropertyInfo> NonPartialProperties { get; }

    /// <summary>
    /// Property-level diagnostics (SD0015–SD0019) collected during attribute parsing.
    /// Reported in the Execute phase via <c>context.ReportDiagnostic</c>.
    /// </summary>
    public List<PropertyDiagnosticInfo> PropertyDiagnostics { get; }

    public RecordStructViewInfo(
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
        Type? descriptionResourceType = null,
        List<NonPartialPropertyInfo>? nonPartialProperties = null,
        List<PropertyDiagnosticInfo>? propertyDiagnostics = null)
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
        NonPartialProperties = nonPartialProperties ?? new List<NonPartialPropertyInfo>();
        PropertyDiagnostics = propertyDiagnostics ?? new List<PropertyDiagnosticInfo>();
    }
}
