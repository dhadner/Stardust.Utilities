namespace Stardust.Utilities;

/// <summary>
/// Specifies the backing storage type for a <see cref="BitFieldsAttribute"/> struct.
/// This enum provides a discoverable, compile-time-safe alternative to the
/// <c>typeof(T)</c> constructor parameter, making it clear which types are supported.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [BitFields(StorageType.Byte)]
/// public partial struct MyRegister { ... }
///
/// // Equivalent to:
/// [BitFields(typeof(byte))]
/// public partial struct MyRegister { ... }
/// </code>
/// For arbitrary-width bitfields, use the <c>int</c> constructor instead:
/// <code>
/// [BitFields(200)]  // 200-bit bitfield backed by 4 x ulong
/// public partial struct WideRegister { ... }
/// </code>
/// </remarks>
public enum StorageType
{
    /// <summary>Signed 8-bit integer (<c>sbyte</c>).</summary>
    SByte = 0,

    /// <summary>Unsigned 8-bit integer (<c>byte</c>).</summary>
    Byte = 1,

    /// <summary>Signed 16-bit integer (<c>short</c>).</summary>
    Int16 = 2,

    /// <summary>Unsigned 16-bit integer (<c>ushort</c>).</summary>
    UInt16 = 3,

    /// <summary>Signed 32-bit integer (<c>int</c>).</summary>
    Int32 = 4,

    /// <summary>Unsigned 32-bit integer (<c>uint</c>).</summary>
    UInt32 = 5,

    /// <summary>Signed 64-bit integer (<c>long</c>).</summary>
    Int64 = 6,

    /// <summary>Unsigned 64-bit integer (<c>ulong</c>).</summary>
    UInt64 = 7,

    /// <summary>Platform-dependent signed native integer (<c>nint</c>). 32 bits on x86, 64 bits on x64/ARM64.</summary>
    NInt = 8,

    /// <summary>Platform-dependent unsigned native integer (<c>nuint</c>). 32 bits on x86, 64 bits on x64/ARM64.</summary>
    NUInt = 9,

    /// <summary>IEEE 754 half-precision floating point (<c>Half</c>, 16 bits).</summary>
    Half = 10,

    /// <summary>IEEE 754 single-precision floating point (<c>float</c>, 32 bits).</summary>
    Single = 11,

    /// <summary>IEEE 754 double-precision floating point (<c>double</c>, 64 bits).</summary>
    Double = 12,

    /// <summary>.NET decimal (128 bits).</summary>
    Decimal = 13,

    /// <summary>Signed 128-bit integer (<c>Int128</c>).</summary>
    Int128 = 14,

    /// <summary>Unsigned 128-bit integer (<c>UInt128</c>).</summary>
    UInt128 = 15,
}
