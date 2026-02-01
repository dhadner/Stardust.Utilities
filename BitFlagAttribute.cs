namespace Stardust.Utilities;

/// <summary>
/// Marks a property as a single-bit flag at the specified bit position.
/// </summary>
/// <remarks>
/// <para>
/// The property must be declared as <c>public partial bool</c>.
/// The generator will implement the getter/setter using inline bit manipulation.
/// </para>
/// <example>
/// <code>
/// [BitFields(typeof(byte))]
/// public partial struct StatusRegister
/// {
///     [BitFlag(0)] public partial bool Ready { get; set; }
///     [BitFlag(1)] public partial bool Error { get; set; }
///     [BitFlag(7)] public partial bool Busy { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BitFlagAttribute : Attribute
{
    /// <summary>
    /// The bit position (0-based).
    /// </summary>
    public int Bit { get; }

    /// <summary>
    /// Creates a new bit flag attribute.
    /// </summary>
    /// <param name="bit">The bit position (0-based).</param>
    public BitFlagAttribute(int bit)
    {
        Bit = bit;
    }
}
