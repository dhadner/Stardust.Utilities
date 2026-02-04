using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 32-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Int32Be : IComparable, IComparable<Int32Be>, IEquatable<Int32Be>,
                             IFormattable, ISpanFormattable, IParsable<Int32Be>, ISpanParsable<Int32Be>
    {
        [FieldOffset(0)] internal UInt16Be hi;
        [FieldOffset(2)] internal UInt16Be lo;

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from two UInt16Be values.
        /// </summary>
        /// <param name="hi">The high 16 bits.</param>
        /// <param name="lo">The low 16 bits.</param>
        public Int32Be(UInt16Be hi, UInt16Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 4 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int32Be(IList<byte> bytes, int offset = 0)
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
        /// Creates a big-endian 32-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 4 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Be(ReadOnlySpan<byte> bytes)
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
        /// Creates a big-endian 32-bit signed integer from a native uint.
        /// </summary>
        /// <param name="num">The native value.</param>
        public Int32Be(uint num)
        {
            hi = (UInt16Be)(ushort)(num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from a native int.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Be(int num)
        {
            hi = (UInt16Be)(ushort)((uint)num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        /// <summary>
        /// Writes the big-endian bytes to an IList.
        /// </summary>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <returns>No return value.</returns>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = hi.hi;
            bytes[offset + 1] = hi.lo;
            bytes[offset + 2] = lo.hi;
            bytes[offset + 3] = lo.lo;
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 4 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
            {
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            }
            destination[0] = hi.hi;
            destination[1] = hi.lo;
            destination[2] = lo.hi;
            destination[3] = lo.lo;
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                return false;
            destination[0] = hi.hi;
            destination[1] = hi.lo;
            destination[2] = lo.hi;
            destination[3] = lo.lo;
            return true;
        }

        /// <summary>
        /// Writes a native int as big-endian bytes to an IList.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        /// <returns>No return value.</returns>
        public static void ToBytes(int num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)((uint)num >> 24);
            bytes[offset + 1] = (byte)((num >> 16) & 0xff);
            bytes[offset + 2] = (byte)((num >> 8) & 0xff);
            bytes[offset + 3] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native int as big-endian bytes to a Span.
        /// </summary>
        /// <param name="num">The native value.</param>
        /// <param name="destination">Destination span.</param>
        /// <returns>No return value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(int num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt32BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian int from a ReadOnlySpan.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <returns>The parsed big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int32Be(source);
        }

        #region Operators

        /// <summary>
        /// Returns the value unchanged.
        /// </summary>
        /// <param name="a">The value.</param>
        /// <returns>The same value.</returns>
        public static Int32Be operator +(Int32Be a) => a;

        /// <summary>
        /// Negates the value.
        /// </summary>
        /// <param name="a">The value to negate.</param>
        /// <returns>The negated value.</returns>
        public static Int32Be operator -(Int32Be a) => new(-(int)a);

        /// <summary>
        /// Adds two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The sum of the values.</returns>
        public static Int32Be operator +(Int32Be a, Int32Be b)
            => new((int)a + (int)b);

        /// <summary>
        /// Determines whether one value is greater than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(Int32Be a, Int32Be b)
           => (int)a > (int)b;

        /// <summary>
        /// Determines whether one value is less than another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(Int32Be a, Int32Be b)
           => (int)a < (int)b;

        /// <summary>
        /// Determines whether one value is greater than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(Int32Be a, Int32Be b)
           => (int)a >= (int)b;

        /// <summary>
        /// Determines whether one value is less than or equal to another.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(Int32Be a, Int32Be b)
           => (int)a <= (int)b;

        /// <summary>
        /// Determines whether two values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(Int32Be a, Int32Be b)
           => (int)a == (int)b;

        /// <summary>
        /// Determines whether two values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(Int32Be a, Int32Be b)
           => (int)a != (int)b;

        /// <summary>
        /// Subtracts one value from another.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The difference of the values.</returns>
        public static Int32Be operator -(Int32Be a, Int32Be b)
            => a + (-b);

        /// <summary>
        /// Multiplies two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The product of the values.</returns>
        public static Int32Be operator *(Int32Be a, Int32Be b)
            => new((int)a * (int)b);

        /// <summary>
        /// Computes the bitwise AND of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise AND of the values.</returns>
        public static Int32Be operator &(Int32Be a, Int32Be b)
            => new((int)a & (int)b);

        /// <summary>
        /// Computes the bitwise OR of two values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The bitwise OR of the values.</returns>
        public static Int32Be operator |(Int32Be a, Int32Be b)
            => new((int)a | (int)b);

        /// <summary>
        /// Shifts a value right by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The shift count source.</param>
        /// <returns>The shifted value.</returns>
        public static Int32Be operator >>(Int32Be a, Int32Be b)
            => new((int)a >> b.lo.lo);

        /// <summary>
        /// Shifts a value left by the specified number of bits.
        /// </summary>
        /// <param name="a">The value to shift.</param>
        /// <param name="b">The shift count source.</param>
        /// <returns>The shifted value.</returns>
        public static Int32Be operator <<(Int32Be a, Int32Be b)
            => new((int)a << b.lo.lo);

        /// <summary>
        /// Divides one value by another.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The quotient of the division.</returns>
        /// <exception cref="DivideByZeroException">Thrown when <paramref name="b"/> is zero.</exception>
        public static Int32Be operator /(Int32Be a, Int32Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int32Be((int)a / (int)b);
        }

        #endregion

        #region Conversions

        /// <summary>
        /// Converts a big-endian value to a native <see cref="int"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int32Be a) => a.hi.hi << 24 | a.hi.lo << 16 | a.lo.hi << 8 | a.lo.lo;

        /// <summary>
        /// Converts a native <see cref="int"/> to a big-endian value.
        /// </summary>
        /// <param name="a">The native value.</param>
        /// <returns>The big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int32Be(int a) => new(a);

        /// <summary>
        /// Converts a big-endian value to a native <see cref="ushort"/>.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The native value.</returns>
        public static explicit operator ushort(Int32Be a) => (ushort)a.hi;

        /// <summary>
        /// Converts a big-endian value to its high byte.
        /// </summary>
        /// <param name="a">The big-endian value.</param>
        /// <returns>The high byte.</returns>
        public static explicit operator byte(Int32Be a) => a.hi.hi;

        /// <summary>
        /// Converts a big-endian 16-bit value to a big-endian 32-bit value with sign extension.
        /// </summary>
        /// <param name="v">The 16-bit value.</param>
        /// <returns>The 32-bit big-endian value.</returns>
        public static explicit operator Int32Be(Int16Be v)
        {
            // Sign-extend Int16Be to Int32Be
            short value = (short)v;
            return new Int32Be(value);
        }

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int32Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int32Be(int.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int32Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Be Parse(string s, IFormatProvider? provider)
        {
            return new Int32Be(int.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int32Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int32Be(int.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int32Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int32Be result)
        {
            if (int.TryParse(s, provider, out int value))
            {
                result = new Int32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int32Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int32Be result)
        {
            if (int.TryParse(s, provider, out int value))
            {
                result = new Int32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override readonly string ToString() => $"0x{hi.hi:x2}{hi.lo:x2}{lo.hi:x2}{lo.lo:x2}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((int)this).ToString(format, formatProvider);
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
            return ((int)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((Int32Be)obj);
        }

        /// <summary>
        /// Compares this instance with another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>A signed integer that indicates the relative order.</returns>
        public readonly int CompareTo(Int32Be other)
        {
            int a = (int)other;
            int b = (int)this;
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
            return Equals((Int32Be)obj);
        }

        /// <summary>
        /// Determines whether this instance equals another value.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(Int32Be other)
        {
            return this == other;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return ((int)this).GetHashCode();
        }

        #endregion
    }
}
