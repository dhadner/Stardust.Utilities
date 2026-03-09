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
    /// Big-endian 64-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int64BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Int64Be : IComparable, IComparable<Int64Be>, IEquatable<Int64Be>,
                             IFormattable, ISpanFormattable, IParsable<Int64Be>, ISpanParsable<Int64Be>
    {
        [FieldOffset(0)] internal UInt32Be hi;
        [FieldOffset(4)] internal UInt32Be lo;

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from two UInt32Be values.
        /// </summary>
        /// <param name="hi">The high 32 bits.</param>
        /// <param name="lo">The low 32 bits.</param>
        public Int64Be(UInt32Be hi, UInt32Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 8 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int64Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 8)
            {
                throw new ArgumentException("List is too short");
            }
            hi = new UInt32Be(bytes, offset);
            lo = new UInt32Be(bytes, offset + 4);
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 8 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
            {
                throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            }
            hi = new UInt32Be(bytes);
            lo = new UInt32Be(bytes.Slice(4));
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a native long.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Be(long num)
        {
            hi = new UInt32Be((uint)((ulong)num >> 32));
            lo = new UInt32Be((uint)(num & 0xFFFFFFFF));
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a native ulong.
        /// </summary>
        /// <param name="num">The native value.</param>
        public Int64Be(ulong num)
        {
            hi = new UInt32Be((uint)(num >> 32));
            lo = new UInt32Be((uint)(num & 0xFFFFFFFF));
        }

        /// <summary>
        /// Writes the big-endian bytes to an IList.
        /// </summary>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <returns>No return value.</returns>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            hi.ToBytes(bytes, offset);
            lo.ToBytes(bytes, offset + 4);
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 8 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
            {
                throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            }
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(4));
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                return false;
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(4));
            return true;
        }

        /// <summary>
        /// Writes a native long as big-endian bytes to an IList.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <returns>No return value.</returns>
        public static void ToBytes(long num, IList<byte> bytes, int offset = 0)
        {
            ulong unum = (ulong)num;
            bytes[offset + 0] = (byte)(unum >> 56);
            bytes[offset + 1] = (byte)((unum >> 48) & 0xff);
            bytes[offset + 2] = (byte)((unum >> 40) & 0xff);
            bytes[offset + 3] = (byte)((unum >> 32) & 0xff);
            bytes[offset + 4] = (byte)((unum >> 24) & 0xff);
            bytes[offset + 5] = (byte)((unum >> 16) & 0xff);
            bytes[offset + 6] = (byte)((unum >> 8) & 0xff);
            bytes[offset + 7] = (byte)(unum & 0xff);
        }

        /// <summary>
        /// Writes a native long as big-endian bytes to a Span.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="destination">Destination span.</param>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(long num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt64BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian long from a ReadOnlySpan.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <returns>The parsed big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int64Be(source);
        }

        #region Operators

        /// <summary>
        /// Returns the value unchanged.
        /// </summary>
        /// <param name="a">The value.</param>
        /// <returns>The same value.</returns>
        public static Int64Be operator +(Int64Be a) => a;

        /// <summary>
        /// Negates the value.
        /// </summary>
        /// <param name="a">The value to negate.</param>
        /// <returns>The negated value.</returns>
        public static Int64Be operator -(Int64Be a) => new(-(long)a);

        /// <summary>
        /// Adds two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The sum of the values.</returns>
        public static Int64Be operator +(Int64Be a, Int64Be b)
            => new((long)a + (long)b);

        /// <summary>
        /// Determines whether one value is greater than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(Int64Be a, Int64Be b)
           => (long)a > (long)b;

        /// <summary>
        /// Determines whether one value is less than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(Int64Be a, Int64Be b)
           => (long)a < (long)b;

        /// <summary>
        /// Determines whether one value is greater than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(Int64Be a, Int64Be b)
           => (long)a >= (long)b;

        /// <summary>
        /// Determines whether one value is less than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(Int64Be a, Int64Be b)
           => (long)a <= (long)b;

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Int64Be a, Int64Be b)
           => (long)a == (long)b;

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Int64Be a, Int64Be b)
           => (long)a != (long)b;

        /// <summary>
        /// Subtracts one value from another.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The difference of the values.</returns>
        public static Int64Be operator -(Int64Be a, Int64Be b)
            => new((long)a - (long)b);

        /// <summary>
        /// Multiplies two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The product of the values.</returns>
        public static Int64Be operator *(Int64Be a, Int64Be b)
            => new((long)a * (long)b);

        /// <summary>
        /// Computes the bitwise AND of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise AND of the values.</returns>
        public static Int64Be operator &(Int64Be a, Int64Be b)
            => new((long)a & (long)b);

        /// <summary>
        /// Computes the bitwise OR of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise OR of the values.</returns>
        public static Int64Be operator |(Int64Be a, Int64Be b)
            => new((long)a | (long)b);

        /// <summary>
        /// Computes the bitwise XOR of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise XOR of the values.</returns>
        public static Int64Be operator ^(Int64Be a, Int64Be b)
            => new((long)a ^ (long)b);

        /// <summary>
        /// Computes the bitwise complement of a value.
        /// </summary>
        /// <param name="a">The value to complement.</param>
        /// <returns>The bitwise complement.</returns>
        public static Int64Be operator ~(Int64Be a)
            => new(~(long)a);

        /// <summary>
        /// Shifts a value right by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The number of bits to shift.</param>
        /// <returns>The shifted value.</returns>
        public static Int64Be operator >>(Int64Be a, int b)
            => new((long)a >> b);

        /// <summary>
        /// Shifts a value left by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The number of bits to shift.</param>
        /// <returns>The shifted value.</returns>
        public static Int64Be operator <<(Int64Be a, int b)
            => new((long)a << b);

        /// <summary>
        /// Divides one value by another.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The quotient of the division.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is zero.</exception>
        public static Int64Be operator /(Int64Be a, Int64Be b)
        {
            long divisor = (long)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int64Be((long)a / divisor);
        }

        /// <summary>
        /// Computes the remainder of dividing a value by another.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The remainder of the division.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is zero.</exception>
        public static Int64Be operator %(Int64Be a, Int64Be b)
        {
            long divisor = (long)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int64Be((long)a % divisor);
        }

        /// <summary>
        /// Increments a value by one.
        /// </summary>
        /// <param name="a">The value to increment.</param>
        /// <returns>The incremented value.</returns>
        public static Int64Be operator ++(Int64Be a)
            => new((long)a + 1);

        /// <summary>
        /// Decrements a value by one.
        /// </summary>
        /// <param name="a">The value to decrement.</param>
        /// <returns>The decremented value.</returns>
        public static Int64Be operator --(Int64Be a)
            => new((long)a - 1);

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a big-endian value to a native <see cref="long"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int64Be a)
        {
            return (long)(((ulong)(uint)a.hi << 32) | (uint)a.lo);
        }

        /// <summary>
        /// Converts a native <see cref="long"/> to a big-endian value.
        /// </summary>
        /// <param name="a">The native value.</param>
        /// <returns>The big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int64Be(long a) => new(a);

        /// <summary>
        /// Converts a big-endian value to a native <see cref="int"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        public static explicit operator int(Int64Be a) => (int)(long)a;

        /// <summary>
        /// Converts a big-endian value to a native <see cref="short"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        public static explicit operator short(Int64Be a) => (short)(long)a;

        /// <summary>
        /// Converts a big-endian value to a native <see cref="sbyte"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        public static explicit operator sbyte(Int64Be a) => (sbyte)(long)a;

        /// <summary>
        /// Converts a big-endian value to a big-endian 32-bit value.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The 32-bit big-endian value.</returns>
        public static explicit operator Int32Be(Int64Be a) => new((int)(long)a);

        /// <summary>
        /// Converts a big-endian value to a big-endian 16-bit value.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The 16-bit big-endian value.</returns>
        public static explicit operator Int16Be(Int64Be a) => new((short)(long)a);

        // Sign-extending conversions from smaller types
        /// <summary>
        /// Widening conversion from a 32-bit big-endian value.
        /// </summary>
        /// <param name="a">The 32-bit value.</param>
        /// <returns>The 64-bit big-endian value.</returns>
        public static implicit operator Int64Be(Int32Be a) => new((int)a);

        /// <summary>
        /// Widening conversion from a 16-bit big-endian value.
        /// </summary>
        /// <param name="a">The 16-bit value.</param>
        /// <returns>The 64-bit big-endian value.</returns>
        public static implicit operator Int64Be(Int16Be a) => new((short)a);

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int64Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int64Be(long.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int64Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Be Parse(string s, IFormatProvider? provider)
        {
            return new Int64Be(long.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int64Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int64Be(long.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int64Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int64Be result)
        {
            if (long.TryParse(s, provider, out long value))
            {
                result = new Int64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int64Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int64Be result)
        {
            if (long.TryParse(s, provider, out long value))
            {
                result = new Int64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override readonly string ToString() => $"0x{(ulong)(long)this:x16}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((long)this).ToString(format, formatProvider);
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
            return ((long)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((Int64Be)obj);
        }

        /// <summary>
        /// Compares this instance with another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        public readonly int CompareTo(Int64Be other)
        {
            long a = (long)other;
            long b = (long)this;
            return b.CompareTo(a);
        }

        /// <summary>
        /// Determines whether this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int64Be)obj);
        }

        /// <summary>
        /// Determines whether this instance equals another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(Int64Be other)
        {
            return this == other;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return ((long)this).GetHashCode();
        }

        #endregion
    }
}
