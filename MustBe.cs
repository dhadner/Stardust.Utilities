namespace Stardust.Utilities;

/// <summary>
/// Specifies how undefined, reserved, or override bits are handled for BitField and
/// BitFlag properties.
/// </summary>
public enum MustBe
{
    /// <summary>
    /// Bits are preserved as raw data. This is the default behavior.
    /// Useful for hardware registers where bits may have external meaning.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Bits are always masked to zero.
    /// Useful for protocol headers where clean, deterministic values are required
    /// or where reserved bits must be set to zero.
    /// </summary>
    Zero = 1,

    /// <summary>
    /// Bits are always set to one.
    /// Useful for protocols or hardware that expect high bits in reserved regions.
    /// </summary>
    One = 2,

    /// <summary>
    /// Alias for <see cref="One"/> for better readability with multi-bit fields.
    /// </summary>
    Ones = One
}
