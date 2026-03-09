namespace Stardust.Utilities;

/// <summary>
/// Specifies how bit positions are numbered within a <see cref="BitFieldsViewAttribute"/>
/// or a <see cref="BitFieldsAttribute"/>.
/// </summary>
public enum BitOrder
{
    /// <summary>
    /// MSB-first (RFC/network convention). Bit 0 is the most significant bit of byte 0.
    /// This matches how protocol fields are described in RFCs and network specifications.
    /// <para>Example (IPv6 header): <c>[BitField(0, 3)]</c> = Version (top nibble of byte 0).</para>
    /// </summary>
    BitZeroIsMsb = 0,

    /// <summary>
    /// RFC (network) order. Bit 0 is the most significant bit of byte 0.
    /// This matches how protocol fields are described in RFCs and network specifications.
    /// <para>Example (IPv6 header): <c>[BitField(0, 3)]</c> = Version (top nibble of byte 0).</para>
    /// </summary>
    RfcNetworkOrder = BitZeroIsMsb,

    /// <summary>
    /// LSB-first (hardware/register convention). Bit 0 is the least significant bit of byte 0.
    /// This matches the convention used by <see cref="BitFieldsAttribute"/> for hardware registers.
    /// <para>Example: <c>[BitField(0, 3)]</c> = bottom nibble of byte 0.</para>
    /// </summary>
    BitZeroIsLsb = 1,

    /// <summary>
    /// LSB-first (hardware/register convention). Bit 0 is the least significant bit of byte 0.
    /// This matches the convention used by <see cref="BitFieldsAttribute"/> for hardware registers.
    /// <para>Example: <c>[BitField(0, 3)]</c> = bottom nibble of byte 0.</para>
    /// </summary>
    HwRegisterOrder = BitZeroIsLsb,
}
