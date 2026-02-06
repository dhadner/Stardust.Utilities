namespace Stardust.Utilities;

/// <summary>
/// Specifies how undefined bits (bits not covered by any field or flag) are handled
/// in a <see cref="BitFieldsAttribute"/> struct.
/// </summary>
public enum UndefinedBitsMustBe
{
    /// <summary>
    /// Undefined bits are preserved as raw data. This is the default behavior.
    /// Useful for hardware registers where undefined bits may have external meaning.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Undefined bits are always masked to zero.
    /// Useful for protocol headers where clean, deterministic values are required
    /// or where reserved bits must be set to zero.
    /// </summary>
    Zeroes = 1,

    /// <summary>
    /// Undefined bits are always set to one.
    /// Useful for protocols or hardware that expect high bits in reserved regions.
    /// </summary>
    Ones = 2
}
