using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 16-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Int16Be : IComparable, IComparable<Int16Be>, IEquatable<Int16Be>,
                             IFormattable, ISpanFormattable, IParsable<Int16Be>, ISpanParsable<Int16Be>
    {
        [FieldOffset(0)] internal byte hi;
        [FieldOffset(1)] internal byte lo;

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native short.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Be(short num)
        {
            hi = (byte)(num >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 2 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int16Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 2)
            {
                throw new ArgumentException("List is too short");
            }
            hi = bytes[offset + 0];
            lo = bytes[offset + 1];
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 2 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
            {
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            }
            hi = bytes[0];
            lo = bytes[1];
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native uint.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value exceeds short.MaxValue.</exception>
        public Int16Be(uint num)
        {
            if (num > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native int.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value exceeds short.MaxValue.</exception>
        public Int16Be(int num)
        {
            if (num > short.MaxValue || num < short.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes the big-endian bytes to a byte array.
        /// </summary>
        /// <param name="bytes">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <returns>No return value.</returns>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = hi;
            bytes[offset + 1] = lo;
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 2 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
            {
                throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            }
            destination[0] = hi;
            destination[1] = lo;
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
                return false;
            destination[0] = hi;
            destination[1] = lo;
            return true;
        }

        /// <summary>
        /// Writes a native short as big-endian bytes to a byte array.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="bytes">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <returns>No return value.</returns>
        public static void ToBytes(short num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 8);
            bytes[offset + 1] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native short as big-endian bytes to a Span.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="destination">Destination span.</param>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(short num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt16BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian short from a ReadOnlySpan.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <returns>The parsed big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int16Be(source);
        }

        #region Operators

        /// <summary>
        /// Returns the value unchanged.
        /// </summary>
        /// <param name="a">The value.</param>
        /// <returns>The same value.</returns>
        public static Int16Be operator +(Int16Be a) => a;

        /// <summary>
        /// Negates the value.
        /// </summary>
        /// <param name="a">The value to negate.</param>
        /// <returns>The negated value.</returns>
        public static Int16Be operator -(Int16Be a) => new((short)-(short)a);

        /// <summary>
        /// Adds two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The sum of the values.</returns>
        public static Int16Be operator +(Int16Be a, Int16Be b)
            => new((short)((short)a + (short)b));

        /// <summary>
        /// Subtracts one value from another.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The difference of the values.</returns>
        public static Int16Be operator -(Int16Be a, Int16Be b)
            => a + (-b);

        /// <summary>
        /// Determines whether one value is greater than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(Int16Be a, Int16Be b)
           => (short)a > (short)b;

        /// <summary>
        /// Determines whether one value is less than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(Int16Be a, Int16Be b)
           => (short)a < (short)b;

        /// <summary>
        /// Determines whether one value is greater than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(Int16Be a, Int16Be b)
           => (short)a >= (short)b;

        /// <summary>
        /// Determines whether one value is less than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(Int16Be a, Int16Be b)
           => (short)a <= (short)b;

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Int16Be a, Int16Be b)
           => (short)a == (short)b;

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Int16Be a, Int16Be b)
           => (short)a != (short)b;

        /// <summary>
        /// Multiplies two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The product of the values.</returns>
        public static Int16Be operator *(Int16Be a, Int16Be b)
            => new((short)((short)a * (short)b));

        /// <summary>
        /// Divides one value by another.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The quotient of the division.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is zero.</exception>
        public static Int16Be operator /(Int16Be a, Int16Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new((short)((short)a / (short)b));
        }

        /// <summary>
        /// Computes the bitwise AND of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise AND of the values.</returns>
        public static Int16Be operator &(Int16Be a, Int16Be b)
            => new((short)((short)a & (short)b));

        /// <summary>
        /// Computes the bitwise OR of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise OR of the values.</returns>
        public static Int16Be operator |(Int16Be a, Int16Be b)
            => new((short)((short)a | (short)b));

        /// <summary>
        /// Shifts a value right by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The number of bits to shift.</param>
        /// <returns>The shifted value.</returns>
        public static Int16Be operator >>(Int16Be a, int b)
            => new((short)((short)a >> (byte)b));

        /// <summary>
        /// Shift left loses bits on the left side.
        /// Must cast the argument to Int32Be prior to shifting
        /// (i.e., use the Int32Be version of this operator)
        /// if the intent is to widen the result.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The number of bits to shift.</param>
        /// <returns>The shifted value.</returns>
        public static Int16Be operator <<(Int16Be a, int b)
            => new((short)((short)a << (byte)b));

        /// <summary>
        /// Computes the remainder of dividing a value by the specified divisor.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The remainder of the division.</returns>
        public static Int16Be operator %(Int16Be a, int b)
            => new((short)((short)a % b));

        /// <summary>
        /// Computes the bitwise XOR of a value and the specified mask.
        /// </summary>
        /// <param name="a">The value to mask.</param>
        /// <param name="b">The mask value.</param>
        /// <returns>The bitwise XOR of the inputs.</returns>
        public static Int16Be operator ^(Int16Be a, int b)
            => new((short)((short)a ^ b));

        /// <summary>
        /// Computes the bitwise complement of a value.
        /// </summary>
        /// <param name="a">The value to complement.</param>
        /// <returns>The bitwise complement.</returns>
        public static Int16Be operator ~(Int16Be a)
            => new((short)~(short)a);

        /// <summary>
        /// Increments a value by one.
        /// </summary>
        /// <param name="a">The value to increment.</param>
        /// <returns>The incremented value.</returns>
        public static Int16Be operator ++(Int16Be a)
            => new((short)((short)a + 1));

        /// <summary>
        /// Decrements a value by one.
        /// </summary>
        /// <param name="a">The value to decrement.</param>
        /// <returns>The decremented value.</returns>
        public static Int16Be operator --(Int16Be a)
            => new((short)((short)a - 1));

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a big-endian value to a native <see cref="short"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(Int16Be a) => (short)((a.hi << 8) | a.lo);

        /// <summary>
        /// Converts a big-endian value to a native <see cref="int"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int16Be a) => (short)a;

        /// <summary>
        /// Converts a native <see cref="short"/> to a big-endian value.
        /// </summary>
        /// <param name="a">The native value.</param>
        /// <returns>The big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int16Be(short a) => new(a);

        /// <summary>
        /// Converts a big-endian 16-bit value to a big-endian 32-bit value.
        /// </summary>
        /// <param name="a">The 16-bit value.</param>
        /// <returns>The 32-bit big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Be(Int16Be a) => new((short)a);

        /// <summary>
        /// Converts a big-endian value to its high byte.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The high byte.</returns>
        public static explicit operator byte(Int16Be a) => a.hi;

        /// <summary>
        /// Converts a native <see cref="uint"/> to a big-endian 16-bit value.
        /// </summary>
        /// <param name="a">The native value.</param>
        /// <returns>The big-endian value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="a"/> is outside the range of <see cref="short"/>.</exception>
        public static explicit operator Int16Be(uint a)
        {
            if (a > short.MaxValue || (int)a < short.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(a));
            }
            return new Int16Be((short)a);
        }

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int16Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int16Be(short.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int16Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Be Parse(string s, IFormatProvider? provider)
        {
            return new Int16Be(short.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int16Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int16Be(short.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int16Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int16Be result)
        {
            if (short.TryParse(s, provider, out short value))
            {
                result = new Int16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int16Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int16Be result)
        {
            if (short.TryParse(s, provider, out short value))
            {
                result = new Int16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override readonly string ToString() => $"0x{(short)this:x4}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((short)this).ToString(format, formatProvider);
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
            return ((short)this).TryFormat(destination, out charsWritten, format, provider);
        }

        #endregion

        #region Comparison and Equality

        /// <summary>
        /// Compares this instance with another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((Int16Be)obj);
        }

        /// <summary>
        /// Compares this instance with another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        public readonly int CompareTo(Int16Be other)
        {
            short a = (short)other;
            short b = (short)this;
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
            return Equals((Int16Be)obj);
        }

        /// <summary>
        /// Determines whether this instance equals another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(Int16Be other)
        {
            return this == other;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return ((short)this).GetHashCode();
        }

        #endregion
    }
}
