using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 32-bit unsigned integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt32BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct UInt32Be : IComparable, IComparable<UInt32Be>, IEquatable<UInt32Be>,
                              IFormattable, ISpanFormattable, IParsable<UInt32Be>, ISpanParsable<UInt32Be>
    {
        [FieldOffset(0)] internal UInt16Be hi;
        [FieldOffset(2)] internal UInt16Be lo;

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from two UInt16Be values.
        /// </summary>
        /// <param name="hi">The high 16 bits.</param>
        /// <param name="lo">The low 16 bits.</param>
        public UInt32Be(UInt16Be hi, UInt16Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 4 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public UInt32Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 4)
            {
                throw new ArgumentException("List is too short");
            }
            hi.hi = bytes[offset + 0];
            hi.lo = bytes[offset + 1];
            lo.hi = bytes[offset + 2];
            lo.lo = bytes[offset + 3];
        }

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 4 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
            {
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            }
            hi.hi = bytes[0];
            hi.lo = bytes[1];
            lo.hi = bytes[2];
            lo.lo = bytes[3];
        }

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from a ReadOnlySpan whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 4 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Be(ReadOnlySpan<byte> bytes, bool isBigEndian = true)
        {
            if (bytes.Length < 4)
            {
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            }
            if (isBigEndian)
            {
                hi.hi = bytes[0];
                hi.lo = bytes[1];
                lo.hi = bytes[2];
                lo.lo = bytes[3];
            }
            else
            {
                hi.hi = bytes[3];
                hi.lo = bytes[2];
                lo.hi = bytes[1];
                lo.lo = bytes[0];
            }
        }

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from a native uint.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Be(uint num)
        {
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            Unsafe.As<UInt32Be, uint>(ref this) = num;
#else
            Unsafe.As<UInt32Be, uint>(ref this) = BinaryPrimitives.ReverseEndianness(num);
#endif
        }

        /// <summary>
        /// Creates a big-endian 32-bit unsigned integer from a native int.
        /// </summary>
        /// <param name="num">The native value.</param>
        public UInt32Be(int num)
        {
            hi = (UInt16Be)(ushort)((uint)num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        /// <summary>
        /// Writes the bytes to an IList in the specified byte order.
        /// </summary>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0, bool isBigEndian = true)
        {
            if (isBigEndian)
            {
                bytes[offset + 0] = hi.hi;
                bytes[offset + 1] = hi.lo;
                bytes[offset + 2] = lo.hi;
                bytes[offset + 3] = lo.lo;
            }
            else
            {
                bytes[offset + 0] = lo.lo;
                bytes[offset + 1] = lo.hi;
                bytes[offset + 2] = hi.lo;
                bytes[offset + 3] = hi.hi;
            }
        }

        /// <summary>
        /// Writes the bytes to a Span in the specified byte order.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 4 bytes).</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, bool isBigEndian = true)
        {
            if (destination.Length < 4)
            {
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            }
            if (isBigEndian)
            {
                destination[0] = hi.hi;
                destination[1] = hi.lo;
                destination[2] = lo.hi;
                destination[3] = lo.lo;
            }
            else
            {
                destination[0] = lo.lo;
                destination[1] = lo.hi;
                destination[2] = hi.lo;
                destination[3] = hi.hi;
            }
        }

        /// <summary>
        /// Tries to write the bytes to a Span in the specified byte order.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) bytes are written big-endian; if <see langword="false"/> they are written little-endian.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, bool isBigEndian = true)
        {
            if (destination.Length < 4)
                return false;
            if (isBigEndian)
            {
                destination[0] = hi.hi;
                destination[1] = hi.lo;
                destination[2] = lo.hi;
                destination[3] = lo.lo;
            }
            else
            {
                destination[0] = lo.lo;
                destination[1] = lo.hi;
                destination[2] = hi.lo;
                destination[3] = hi.hi;
            }
            return true;
        }

        /// <summary>
        /// Writes a native uint as big-endian bytes to an IList.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <returns>No return value.</returns>
        public static void ToBytes(uint num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 24);
            bytes[offset + 1] = (byte)((num >> 16) & 0xff);
            bytes[offset + 2] = (byte)((num >> 8) & 0xff);
            bytes[offset + 3] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native uint as big-endian bytes to a Span.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="destination">Destination span.</param>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(uint num, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a <see cref="UInt32Be"/> from a ReadOnlySpan in the specified byte order.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <param name="isBigEndian">If <see langword="true"/> (default) the source is interpreted as big-endian; if <see langword="false"/> it is interpreted as little-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be ReadFrom(ReadOnlySpan<byte> source, bool isBigEndian = true) => new(source, isBigEndian);

        #region Operators

        /// <summary>
        /// Returns the value unchanged.
        /// </summary>
        /// <param name="a">The value.</param>
        /// <returns>The same value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator +(UInt32Be a) => a;

        /// <summary>
        /// Negates the value.
        /// </summary>
        /// <param name="a">The value to negate.</param>
        /// <returns>The negated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator -(UInt32Be a) => new((uint)-(uint)a);

        /// <summary>
        /// Adds two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The sum of the values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator +(UInt32Be a, UInt32Be b)
            => new((uint)a + (uint)b);

        /// <summary>
        /// Determines whether one value is greater than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(UInt32Be a, UInt32Be b)
           => (uint)a > (uint)b;

        /// <summary>
        /// Determines whether one value is less than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(UInt32Be a, UInt32Be b)
           => (uint)a < (uint)b;

        /// <summary>
        /// Determines whether one value is greater than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(UInt32Be a, UInt32Be b)
           => (uint)a >= (uint)b;

        /// <summary>
        /// Determines whether one value is less than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(UInt32Be a, UInt32Be b)
           => (uint)a <= (uint)b;

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(UInt32Be a, UInt32Be b)
           => (uint)a == (uint)b;

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(UInt32Be a, UInt32Be b)
           => (uint)a != (uint)b;

        /// <summary>
        /// Subtracts one value from another.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The difference of the values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator -(UInt32Be a, UInt32Be b)
            => a + (-b);

        /// <summary>
        /// Multiplies two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The product of the values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator *(UInt32Be a, UInt32Be b)
            => new((uint)a * (uint)b);

        /// <summary>
        /// Computes the bitwise AND of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise AND of the values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator &(UInt32Be a, UInt32Be b)
        {
            Unsafe.As<UInt32Be, uint>(ref a) &= Unsafe.As<UInt32Be, uint>(ref b);
            return a;
        }

        /// <summary>
        /// Computes the bitwise OR of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise OR of the values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator |(UInt32Be a, UInt32Be b)
        {
            Unsafe.As<UInt32Be, uint>(ref a) |= Unsafe.As<UInt32Be, uint>(ref b);
            return a;
        }

        /// <summary>
        /// Shifts a value right by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The shift count source.</param>
        /// <returns>The shifted value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator >>(UInt32Be a, UInt32Be b)
            => new((uint)a >> b.lo.lo);

        /// <summary>Performs an unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator >>>(UInt32Be a, int b)
            => new((uint)a >> b);

        /// <summary>
        /// Shifts a value left by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The shift count source.</param>
        /// <returns>The shifted value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator <<(UInt32Be a, UInt32Be b)
            => new((uint)a << b.lo.lo);

        /// <summary>
        /// Divides one value by another.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The quotient of the division.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Be operator /(UInt32Be a, UInt32Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt32Be((uint)a / (uint)b);
        }
        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator %(UInt32Be a, UInt32Be b)
        {
            if ((uint)b == 0) throw new DivideByZeroException();
            return new((uint)a % (uint)b);
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator ^(UInt32Be a, UInt32Be b)
        {
            Unsafe.As<UInt32Be, uint>(ref a) ^= Unsafe.As<UInt32Be, uint>(ref b);
            return a;
        }
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator ~(UInt32Be a)
        {
            Unsafe.As<UInt32Be, uint>(ref a) = ~Unsafe.As<UInt32Be, uint>(ref a);
            return a;
        }
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator ++(UInt32Be a) => new((uint)a + 1);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be operator --(UInt32Be a) => new((uint)a - 1);

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a big-endian value to a native <see cref="uint"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if BIG_ENDIAN
        public static implicit operator uint(UInt32Be a) => Unsafe.As<UInt32Be, uint>(ref a);
#else
        public static implicit operator uint(UInt32Be a) => BinaryPrimitives.ReverseEndianness(Unsafe.As<UInt32Be, uint>(ref a));
#endif

        /// <summary>
        /// Converts a native <see cref="uint"/> to a big-endian value.
        /// </summary>
        /// <param name="a">The native value.</param>
        /// <returns>The big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Be(uint a) => new(a);

        /// <summary>
        /// Converts a big-endian value to a native <see cref="ushort"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator ushort(UInt32Be a) => (ushort)a.hi;

        /// <summary>
        /// Converts a big-endian value to its high byte.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The high byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator byte(UInt32Be a) => a.hi.hi;

        /// <summary>
        /// Converts a big-endian value to a big-endian 16-bit value.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The low 16-bit portion as a big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt16Be(UInt32Be a) => a.lo;

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to a UInt32Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt32Be(uint.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to a UInt32Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Be Parse(string s, IFormatProvider? provider)
        {
            return new UInt32Be(uint.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to a UInt32Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new UInt32Be(uint.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to a UInt32Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt32Be result)
        {
            if (uint.TryParse(s, provider, out uint value))
            {
                result = new UInt32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to a UInt32Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt32Be result)
        {
            if (uint.TryParse(s, provider, out uint value))
            {
                result = new UInt32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override string ToString() => $"0x{hi.hi:x2}{hi.lo:x2}{lo.hi:x2}{lo.lo:x2}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((uint)this).ToString(format, formatProvider);
        }

        /// <summary>
        /// Tries to format the value into the destination span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="charsWritten">The number of characters written.</param>
        /// <param name="format">The format string.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns><see langword="true"/> if formatting succeeds; otherwise, <see langword="false"/>.</returns>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((uint)this).TryFormat(destination, out charsWritten, format, provider);
        }

        #endregion

        #region Comparison and Equality

        /// <summary>
        /// Compares this instance with another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is null.</exception>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((UInt32Be)obj);
        }

        /// <summary>
        /// Compares this instance with another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        public readonly int CompareTo(UInt32Be other)
        {
            uint a = (uint)other;
            uint b = (uint)this;
            return b.CompareTo(a);
        }

        /// <summary>
        /// Determines whether this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt32Be)obj);
        }

        /// <summary>
        /// Determines whether this instance equals another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(UInt32Be other)
        {
            return this == other;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return ((uint)this).GetHashCode();
        }

        #endregion
    }
}
