using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 256-bit signed integer type (two's complement).
    /// Stores bytes in big-endian order (most significant byte first, network order).
    /// Use this type at I/O boundaries; convert to <see cref="Int256"/> for arithmetic.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int256BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct Int256Be : IComparable, IComparable<Int256Be>, IEquatable<Int256Be>,
                             IFormattable, ISpanFormattable, IParsable<Int256Be>, ISpanParsable<Int256Be>
    {
        // Big-endian: most-significant bits at the lowest address.
        [FieldOffset(0)]  internal UInt128Be hi; // bits 128-255 (sign bit is MSB of hi)
        [FieldOffset(16)] internal UInt128Be lo; // bits 0-127

        /// <summary>Initializes a new <see cref="Int256Be"/> from an <see cref="Int256"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256Be(Int256 num)
        {
            UInt256 u = (UInt256)num;
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; write limbs directly in network (MSB-first) order.
            ref ulong dst = ref Unsafe.As<Int256Be, ulong>(ref this);
            dst                    = u._p3;
            Unsafe.Add(ref dst, 1) = u._p2;
            Unsafe.Add(ref dst, 2) = u._p1;
            Unsafe.Add(ref dst, 3) = u._p0;
#else
            // On LE: each limb must be byte-swapped to produce network byte order.
            Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            BinaryPrimitives.WriteUInt64BigEndian(raw,       u._p3);
            BinaryPrimitives.WriteUInt64BigEndian(raw[8..],  u._p2);
            BinaryPrimitives.WriteUInt64BigEndian(raw[16..], u._p1);
            BinaryPrimitives.WriteUInt64BigEndian(raw[24..], u._p0);
#endif
        }

        /// <summary>Initializes a new <see cref="Int256Be"/> from a read-only byte span (big-endian, most-significant byte first).</summary>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 32 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            bytes[..32].CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>
        /// Initializes a new <see cref="Int256Be"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256Be(ReadOnlySpan<byte> bytes, bool isBigEndian = true)
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

        /// <summary>Initializes a new <see cref="Int256Be"/> from a byte array at the given offset.</summary>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> is too short.</exception>
        public Int256Be(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Array is too short");
            Unsafe.SkipInit(out this);
            new ReadOnlySpan<byte>(bytes, offset, 32).CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = true)
        {
            if (isBigEndian)
            {
                hi.ToBytes(bytes, offset);
                lo.ToBytes(bytes, offset + 16);
            }
            else
            {
                ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
                for (int i = 0; i < 32; i++) bytes[offset + i] = src[31 - i];
            }
        }

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> has fewer than 32 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, bool isBigEndian = true)
        {
            if (destination.Length < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                src.CopyTo(destination);
            }
            else
            {
                for (int i = 0; i < 32; i++) destination[i] = src[31 - i];
            }
        }

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> if the span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, bool isBigEndian = true)
        {
            if (destination.Length < 32) return false;
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                src.CopyTo(destination);
            }
            else
            {
                for (int i = 0; i < 32; i++) destination[i] = src[31 - i];
            }
            return true;
        }

        /// <summary>Reads an <see cref="Int256Be"/> from a read-only byte span in the specified byte order.</summary>
        /// <param name="source">Source span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be ReadFrom(ReadOnlySpan<byte> source, bool isBigEndian = true) => new(source, isBigEndian);

        /// <summary>Parses a string into an <see cref="Int256Be"/>.</summary>
        public static Int256Be Parse(string s, NumberStyles style = NumberStyles.Integer) => new(Int256.Parse(s, style));
        /// <inheritdoc/>
        public static Int256Be Parse(string s, IFormatProvider? provider) => new(Int256.Parse(s, provider));
        /// <inheritdoc/>
        public static Int256Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(Int256.Parse(s, provider));
        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int256Be result)
        {
            if (Int256.TryParse(s, provider, out Int256 v)) { result = new(v); return true; }
            result = default; return false;
        }
        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int256Be result)
        {
            if (Int256.TryParse(s, provider, out Int256 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ((Int256)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((Int256)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((Int256)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int256Be)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int256Be other) => ((Int256)this).CompareTo((Int256)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is Int256Be other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(Int256Be other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((Int256)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator +(Int256Be a) => a;
        /// <summary>Negates the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator -(Int256Be a) => new(-(Int256)a);
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator +(Int256Be a, Int256Be b) => new((Int256)a + (Int256)b);
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator -(Int256Be a, Int256Be b) => new((Int256)a - (Int256)b);
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator *(Int256Be a, Int256Be b) => new((Int256)a * (Int256)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator /(Int256Be a, Int256Be b) => new((Int256)a / (Int256)b);
        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator %(Int256Be a, Int256Be b) => new((Int256)a % (Int256)b);
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator &(Int256Be a, Int256Be b)
        {
            Unsafe.As<Int256Be, ulong>(ref a) &= Unsafe.As<Int256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 2) &= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 3) &= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator |(Int256Be a, Int256Be b)
        {
            Unsafe.As<Int256Be, ulong>(ref a) |= Unsafe.As<Int256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 2) |= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 3) |= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator ^(Int256Be a, Int256Be b)
        {
            Unsafe.As<Int256Be, ulong>(ref a) ^= Unsafe.As<Int256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 2) ^= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 3) ^= Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator ~(Int256Be a)
        {
            Unsafe.As<Int256Be, ulong>(ref a) = ~Unsafe.As<Int256Be, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 1);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 2) = ~Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 2);
            Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 3) = ~Unsafe.Add(ref Unsafe.As<Int256Be, ulong>(ref a), 3);
            return a;
        }
        /// <summary>Arithmetic (sign-extending) right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator >>(Int256Be a, int b) => new((Int256)a >> b);
        /// <summary>Unsigned (logical, zero-filling) right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator >>>(Int256Be a, int b) => new((Int256)a >>> b);
        /// <summary>Shifts the value left.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator <<(Int256Be a, int b) => new((Int256)a << b);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator ++(Int256Be a) => new((Int256)a + Int256.One);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Be operator --(Int256Be a) => new((Int256)a - Int256.One);

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Int256Be a, Int256Be b)
        {
            ref ulong ra = ref Unsafe.As<Int256Be, ulong>(ref a);
            ref ulong rb = ref Unsafe.As<Int256Be, ulong>(ref b);
            return ra == rb
                && Unsafe.Add(ref ra, 1) == Unsafe.Add(ref rb, 1)
                && Unsafe.Add(ref ra, 2) == Unsafe.Add(ref rb, 2)
                && Unsafe.Add(ref ra, 3) == Unsafe.Add(ref rb, 3);
        }
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Int256Be a, Int256Be b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Int256Be a, Int256Be b) => (Int256)a < (Int256)b;
        /// <summary>Determines whether the left operand is greater than the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Int256Be a, Int256Be b) => (Int256)a > (Int256)b;
        /// <summary>Determines whether the left operand is less than or equal to the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Int256Be a, Int256Be b) => (Int256)a <= (Int256)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Int256Be a, Int256Be b) => (Int256)a >= (Int256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int256Be"/> to an <see cref="Int256"/> (host-native, signed).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(Int256Be a)
        {
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; limbs are already in native order at predictable offsets.
            ref ulong src = ref Unsafe.As<Int256Be, ulong>(ref a);
            return (Int256)new UInt256(src, Unsafe.Add(ref src, 1), Unsafe.Add(ref src, 2), Unsafe.Add(ref src, 3));
#else
            // On LE: byte-swap each limb back to native little-endian order.
            ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            return (Int256)new UInt256(
                BinaryPrimitives.ReadUInt64BigEndian(raw),
                BinaryPrimitives.ReadUInt64BigEndian(raw[8..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[16..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[24..]));
#endif
        }
        /// <summary>Implicitly converts an <see cref="Int256"/> to an <see cref="Int256Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Be(Int256 a) => new(a);

        /// <summary>Widening sign-extending conversion from a 128-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Be(Int128Be a) => new((Int256)(Int128)a);
        /// <summary>Widening sign-extending conversion from a 64-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Be(Int64Be a) => new((Int256)(long)a);
        /// <summary>Widening sign-extending conversion from a 32-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Be(Int32Be a) => new((Int256)(int)a);

        /// <summary>Narrowing conversion to a 128-bit big-endian signed value (truncates to low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128Be(Int256Be a) => (Int128Be)(Int128)(Int256)a;
        /// <summary>Narrowing conversion to a 64-bit big-endian signed value (truncates to low 64 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int64Be(Int256Be a) => (Int64Be)(long)(Int256)a;
        /// <summary>Narrowing conversion to a 32-bit big-endian signed value (truncates to low 32 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int32Be(Int256Be a) => (Int32Be)(int)(Int256)a;

        /// <summary>Explicitly reinterprets the bit pattern as an unsigned <see cref="UInt256Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256Be(Int256Be a) => new((UInt256)(Int256)a);
        /// <summary>Explicitly reinterprets the bit pattern of a <see cref="UInt256Be"/> as a signed <see cref="Int256Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256Be(UInt256Be a) => new((Int256)(UInt256)a);

        /// <summary>Explicitly converts to a host-native <see cref="UInt256"/> (bit-reinterpret, no sign extension).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(Int256Be a) => new UInt256((UInt128)a.hi, (UInt128)a.lo);

        #endregion
    }
}
