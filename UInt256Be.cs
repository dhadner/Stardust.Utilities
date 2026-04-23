using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 256-bit unsigned integer type.
    /// Stores bytes in big-endian order (most significant byte first, network order).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt256BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct UInt256Be : IComparable, IComparable<UInt256Be>, IEquatable<UInt256Be>,
                              IFormattable, ISpanFormattable, IParsable<UInt256Be>, ISpanParsable<UInt256Be>
    {
        // Big-endian: hi half stored at lower addresses so first bytes on the wire are most-significant.
        [FieldOffset(0)] internal UInt128Be hi;
        [FieldOffset(16)] internal UInt128Be lo;

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="UInt256"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(UInt256 num)
        {
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; write limbs directly in network (MSB-first) order.
            ref ulong dst = ref Unsafe.As<UInt256Be, ulong>(ref this);
            dst                        = num._p3;
            Unsafe.Add(ref dst, 1) = num._p2;
            Unsafe.Add(ref dst, 2) = num._p1;
            Unsafe.Add(ref dst, 3) = num._p0;
#else
            // On LE: each limb must be byte-swapped to produce network byte order.
            Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            BinaryPrimitives.WriteUInt64BigEndian(raw,       num._p3);
            BinaryPrimitives.WriteUInt64BigEndian(raw[8..],  num._p2);
            BinaryPrimitives.WriteUInt64BigEndian(raw[16..], num._p1);
            BinaryPrimitives.WriteUInt64BigEndian(raw[24..], num._p0);
#endif
        }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="UInt128"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(UInt128 num) { hi = (UInt128Be)UInt128.Zero; lo = (UInt128Be)num; }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="ulong"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ulong num) { hi = (UInt128Be)UInt128.Zero; lo = (UInt128Be)(UInt128)num; }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a read-only byte span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            bytes[..32].CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>
        /// Initializes a new <see cref="UInt256Be"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ReadOnlySpan<byte> bytes, bool isBigEndian = true)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                bytes[..32].CopyTo(dst);
            }
            else
            {
                for (int i = 0; i < 32; i++) dst[i] = bytes[31 - i];
            }
        }

        /// <summary>
        /// Initializes a new <see cref="UInt256Be"/> from a read-only byte span at the given offset.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short from the given offset.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ReadOnlySpan<byte> bytes, int offset, bool isBigEndian = true)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                bytes.Slice(offset, 32).CopyTo(dst);
            }
            else
            {
                for (int i = 0; i < 32; i++) dst[i] = bytes[offset + 31 - i];
            }
        }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a byte array whose byte order is specified.</summary>
        /// <param name="bytes">Source byte array (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If array is too short.</exception>
        public UInt256Be(byte[] bytes, bool isBigEndian = true) : this(new ReadOnlySpan<byte>(bytes), isBigEndian) { }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">Source byte array (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If array is too short.</exception>
        public UInt256Be(byte[] bytes, int offset, bool isBigEndian = true) : this(new ReadOnlySpan<byte>(bytes), offset, isBigEndian) { }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        [Obsolete("Use WriteTo(byte[], int, bool) instead.")]
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = true)
            => WriteTo(new Span<byte>(bytes), offset, isBigEndian);

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the destination span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, int offset = 0, bool isBigEndian = true)
        {
            if (destination.Length - offset < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                src.CopyTo(destination.Slice(offset));
            }
            else
            {
                for (int i = 0; i < 32; i++) destination[offset + i] = src[31 - i];
            }
        }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="destination">Destination byte array (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <exception cref="ArgumentException">If array is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(byte[] destination, int offset = 0, bool isBigEndian = true)
            => WriteTo(new Span<byte>(destination), offset, isBigEndian);

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span.</param>
        /// <param name="offset">Starting offset in the destination span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, int offset = 0, bool isBigEndian = true)
        {
            if (destination.Length - offset < 32) return false;
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                src.CopyTo(destination.Slice(offset));
            }
            else
            {
                for (int i = 0; i < 32; i++) destination[offset + i] = src[31 - i];
            }
            return true;
        }

        /// <summary>Attempts to write the value to a byte array in the specified byte order.</summary>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <returns>True if successful, false if array is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(byte[] destination, int offset = 0, bool isBigEndian = true)
            => TryWriteTo(new Span<byte>(destination), offset, isBigEndian);

        /// <summary>Reads a <see cref="UInt256Be"/> from a read-only byte span in the specified byte order.</summary>
        /// <param name="source">Source span.</param>
        /// <param name="offset">Starting offset in the source span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be ReadFrom(ReadOnlySpan<byte> source, int offset = 0, bool isBigEndian = true) => new(source, offset, isBigEndian);

        /// <summary>Reads a <see cref="UInt256Be"/> from a byte array in the specified byte order.</summary>
        /// <param name="source">Source byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be ReadFrom(byte[] source, int offset = 0, bool isBigEndian = true) => new(new ReadOnlySpan<byte>(source), offset, isBigEndian);

        /// <summary>Parses a string into a <see cref="UInt256Be"/>.</summary>
        public static UInt256Be Parse(string s, NumberStyles style = NumberStyles.Integer) => new(UInt256.Parse(s, style));
        /// <inheritdoc/>
        public static UInt256Be Parse(string s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));
        /// <inheritdoc/>
        public static UInt256Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));
        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt256Be result)
        {
            if (UInt256.TryParse(s, provider, out UInt256 v)) { result = new(v); return true; }
            result = default; return false;
        }
        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt256Be result)
        {
            if (UInt256.TryParse(s, provider, out UInt256 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(UInt256)this:x64}";
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((UInt256)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((UInt256)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt256Be)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt256Be other) => ((UInt256)this).CompareTo((UInt256)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is UInt256Be other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256Be other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((UInt256)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator +(UInt256Be a) => a;
        /// <summary>Computes the two's complement negation (modulo 2^256).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator -(UInt256Be a) => new(-(UInt256)a);
        /// <summary>Adds two values (wraps on overflow).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator +(UInt256Be a, UInt256Be b) => new((UInt256)a + (UInt256)b);
        /// <summary>Subtracts two values (wraps on underflow).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator -(UInt256Be a, UInt256Be b) => new((UInt256)a - (UInt256)b);
        /// <summary>Multiplies two values (low 256 bits of the full product).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator *(UInt256Be a, UInt256Be b) => new((UInt256)a * (UInt256)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator /(UInt256Be a, UInt256Be b) => new((UInt256)a / (UInt256)b);
        /// <summary>Computes the remainder of division.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator %(UInt256Be a, UInt256Be b) => new((UInt256)a % (UInt256)b);
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator &(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) &= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator |(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) |= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ^(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) ^= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ~(UInt256Be a)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) = ~Unsafe.As<UInt256Be, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3);
            return a;
        }
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator <<(UInt256Be a, int b) => new((UInt256)a << b);
        /// <summary>Shifts the value right by the specified amount (logical for unsigned).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator >>(UInt256Be a, int b) => new((UInt256)a >> b);
        /// <summary>Unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator >>>(UInt256Be a, int b) => new((UInt256)a >>> b);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ++(UInt256Be a) => new((UInt256)a + UInt256.One);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator --(UInt256Be a) => new((UInt256)a - UInt256.One);

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256Be a, UInt256Be b)
        {
            ref ulong ra = ref Unsafe.As<UInt256Be, ulong>(ref a);
            ref ulong rb = ref Unsafe.As<UInt256Be, ulong>(ref b);
            return ra == rb
                && Unsafe.Add(ref ra, 1) == Unsafe.Add(ref rb, 1)
                && Unsafe.Add(ref ra, 2) == Unsafe.Add(ref rb, 2)
                && Unsafe.Add(ref ra, 3) == Unsafe.Add(ref rb, 3);
        }
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256Be a, UInt256Be b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256Be a, UInt256Be b) => (UInt256)a < (UInt256)b;
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256Be a, UInt256Be b) => (UInt256)a > (UInt256)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UInt256Be a, UInt256Be b) => (UInt256)a <= (UInt256)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UInt256Be a, UInt256Be b) => (UInt256)a >= (UInt256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt256Be"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(UInt256Be a)
        {
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; limbs are already in native order at predictable offsets.
            ref ulong src = ref Unsafe.As<UInt256Be, ulong>(ref a);
            return new UInt256(src, Unsafe.Add(ref src, 1), Unsafe.Add(ref src, 2), Unsafe.Add(ref src, 3));
#else
            // On LE: byte-swap each limb back to native little-endian order.
            ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            return new UInt256(
                BinaryPrimitives.ReadUInt64BigEndian(raw),
                BinaryPrimitives.ReadUInt64BigEndian(raw[8..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[16..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[24..]));
#endif
        }
        /// <summary>Implicitly converts a <see cref="UInt256"/> to a <see cref="UInt256Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt256 a) => new(a);

        /// <summary>Widening conversion from a 128-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt128Be a) => new((UInt128)a);
        /// <summary>Widening conversion from a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt64Be a) => new((ulong)a);
        /// <summary>Widening conversion from a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt32Be a) => new((ulong)(uint)a);

        /// <summary>Narrowing conversion to a 128-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128Be(UInt256Be a) => a.lo;
        /// <summary>Narrowing conversion to a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt64Be(UInt256Be a) => (UInt64Be)(ulong)(UInt128)a.lo;
        /// <summary>Narrowing conversion to a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt32Be(UInt256Be a) => (UInt32Be)(uint)(ulong)(UInt128)a.lo;

        /// <summary>Explicitly converts to a <see cref="UInt128"/> (keeps low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(UInt256Be a) => (UInt128)a.lo;
        /// <summary>Explicitly converts to an <see cref="Int256"/> (reinterpret).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt256Be a) => (Int256)(UInt256)a;

        #endregion
    }
}
