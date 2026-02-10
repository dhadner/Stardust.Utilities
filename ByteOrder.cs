namespace Stardust.Utilities;

/// <summary>
/// Specifies the byte order (endianness) for multi-byte field access in a <see cref="BitFieldsViewAttribute"/>.
/// </summary>
public enum ByteOrder
{
    /// <summary>
    /// Big-endian (network byte order). Most significant byte first.
    /// This is the standard for network protocols (TCP/IP, DNS, HTTP/2, etc.).
    /// </summary>
    BigEndian = 0,

    /// <summary>
    /// Network byte order. Synonym for <see cref="BigEndian"/>.
    /// Provided for readability when defining protocol headers.
    /// </summary>
    NetworkEndian = BigEndian,

    /// <summary>
    /// Little-endian (native byte order on x86/ARM). Least significant byte first.
    /// This matches the native memory layout on most modern processors.
    /// </summary>
    LittleEndian = 1
}
