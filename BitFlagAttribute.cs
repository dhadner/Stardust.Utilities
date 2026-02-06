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
    /// Normally set to <see cref="MustBe.Any"/> and has no effect.
    /// When set to <see cref="MustBe.Zero"/> or <see cref="MustBe.One"/>, it overrides the
    /// flag's value. On write and during conversion of the underlying BitFields struct to/from other 
    /// types, the flag's bit will be forced to zero or one respectively.
    /// </summary>
    public MustBe ValueOverride { get; }

    /// <summary>
    /// Creates a new bit flag attribute.
    /// </summary>
    /// <param name="bit">The bit position (0-based).</param>
    public BitFlagAttribute(int bit, MustBe mustBe = MustBe.Any)
    {
        Bit = bit;
        ValueOverride = mustBe;
    }
}
