namespace Stardust.Utilities;

/// <summary>
/// Marks a property as a multi-bit field spanning from startBit to endBit (inclusive).
/// </summary>
/// <remarks>
/// <para>
/// Uses Rust-style inclusive bit range syntax where the width is calculated as 
/// (endBit - startBit + 1). For example, <c>[BitField(0, 2)]</c> defines a 3-bit 
/// field spanning bits 0, 1, and 2.
/// </para>
/// <para>
/// The property must be declared as <c>public partial {type}</c> where type is
/// byte, ushort, uint, or ulong. The generator will implement the getter/setter
/// using inline bit manipulation.
/// </para>
/// <example>
/// <code>
/// [BitFields(typeof(byte))]
/// public partial struct RegisterA
/// {
///     // 3-bit field at bits 0, 1, 2 (like Rust's 0..=2)
///     [BitField(0, 2)] public partial byte Sound { get; set; }
///     
///     // Single bit at position 3 (width = 1)
///     [BitField(3, 3)] public partial byte Flag { get; set; }
///     
///     // 4-bit field at bits 4, 5, 6, 7 (like Rust's 4..=7)
///     [BitField(4, 7)] public partial byte Mode { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BitFieldAttribute : Attribute
{
    /// <summary>
    /// The starting bit position (0-based, inclusive).
    /// </summary>
    public int StartBit { get; }

    /// <summary>
    /// The ending bit position (0-based, inclusive).
    /// </summary>
    public int EndBit { get; }

    /// <summary>
    /// Creates a new bit field attribute with Rust-style inclusive bit range.
    /// </summary>
    /// <param name="startBit">The starting bit position (0-based, inclusive).</param>
    /// <param name="endBit">The ending bit position (0-based, inclusive). Must be >= startBit.</param>
    /// <exception cref="ArgumentException">Thrown when endBit is less than startBit.</exception>
    public BitFieldAttribute(int startBit, int endBit)
    {
        if (endBit < startBit)
            throw new ArgumentException($"endBit ({endBit}) must be >= startBit ({startBit})", nameof(endBit));
        
        StartBit = startBit;
        EndBit = endBit;
    }
}
