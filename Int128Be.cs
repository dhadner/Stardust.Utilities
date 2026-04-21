using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 128-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int128BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Int128Be : IComparable, IComparable<Int128Be>, IEquatable<Int128Be>,
                              IFormattable, ISpanFormattable, IParsable<Int128Be>, ISpanParsable<Int128Be>
    {
        [FieldOffset(0)] internal UInt64Be hi;
        [FieldOffset(8)] internal UInt64Be lo;

        /// <summary>
        /// Creates a big-endian 128-bit signed integer from two UInt64Be values.
        /// </summary>
        /// <param name="hi">The high 64 bits.</param>
        /// <param name="lo">The low 64 bits.</param>
        public Int128Be(UInt64Be hi, UInt64Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 128-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 16 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int128Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 16)
            {
                throw new ArgumentException("List is too short");
            }
            hi = new UInt64Be(bytes, offset);
            lo = new UInt64Be(bytes, offset + 8);
        }

        /// <summary>
        /// Creates a big-endian 128-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 16 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 16)
            {
                throw new ArgumentException("Span must have at least 16 bytes", nameof(bytes));
            }
            hi = new UInt64Be(bytes);
            lo = new UInt64Be(bytes.Slice(8));
        }

        /// <summary>
        /// Creates a big-endian 128-bit signed integer from a native Int128.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128Be(Int128 num)
        {
            UInt128 u = (UInt128)num;
            hi = new UInt64Be((ulong)(u >> 64));
            lo = new UInt64Be((ulong)u);
        }

        /// <summary>
        /// Creates a big-endian 128-bit signed integer from a native long.
        /// </summary>
        /// <param name="num">The native value.</param>
        public Int128Be(long num)
        {
            Int128 wide = num;
            UInt128 u = (UInt128)wide;
            hi = new UInt64Be((ulong)(u >> 64));
            lo = new UInt64Be((ulong)u);
        }

        /// <summary>
        /// Writes the big-endian bytes to an IList.
        /// </summary>
        /// <param name="bytes">Destination list.</param>
        /// <param name="offset">Starting offset.</param>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            hi.ToBytes(bytes, offset);
            lo.ToBytes(bytes, offset + 8);
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 16 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 16)
            {
                throw new ArgumentException("Destination span must have at least 16 bytes", nameof(destination));
            }
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(8));
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 16)
                return false;
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(8));
            return true;
        }

        /// <summary>
        /// Reads a big-endian Int128 from a ReadOnlySpan.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <returns>The parsed big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int128Be(source);
        }

        #region Operators

        /// <summary>Returns the value unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator +(Int128Be a) => a;

        /// <summary>Negates the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator -(Int128Be a) => new(-(Int128)a);

        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator +(Int128Be a, Int128Be b)
            => new((Int128)a + (Int128)b);

        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator -(Int128Be a, Int128Be b)
            => new((Int128)a - (Int128)b);

        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator *(Int128Be a, Int128Be b)
            => new((Int128)a * (Int128)b);

        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator /(Int128Be a, Int128Be b)
        {
            Int128 divisor = (Int128)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int128Be((Int128)a / divisor);
        }

        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator %(Int128Be a, Int128Be b)
        {
            Int128 divisor = (Int128)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int128Be((Int128)a % divisor);
        }

        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(Int128Be a, Int128Be b) => (Int128)a > (Int128)b;

        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(Int128Be a, Int128Be b) => (Int128)a < (Int128)b;

        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(Int128Be a, Int128Be b) => (Int128)a >= (Int128)b;

        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(Int128Be a, Int128Be b) => (Int128)a <= (Int128)b;

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(Int128Be a, Int128Be b) => (Int128)a == (Int128)b;

        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(Int128Be a, Int128Be b) => (Int128)a != (Int128)b;

        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator &(Int128Be a, Int128Be b) => new((Int128)((UInt128)(Int128)a & (UInt128)(Int128)b));

        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator |(Int128Be a, Int128Be b) => new((Int128)((UInt128)(Int128)a | (UInt128)(Int128)b));

        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator ^(Int128Be a, Int128Be b) => new((Int128)((UInt128)(Int128)a ^ (UInt128)(Int128)b));

        /// <summary>Computes the bitwise complement of a value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator ~(Int128Be a) => new((Int128)(~(UInt128)(Int128)a));

        /// <summary>Shifts a value right by the specified number of bits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator >>(Int128Be a, int b) => new((Int128)a >> b);

        /// <summary>Performs an unsigned (logical) right shift by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128Be operator >>>(Int128Be a, int b) => new((Int128)((UInt128)(Int128)a >> b));

        /// <summary>Shifts a value left by the specified number of bits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator <<(Int128Be a, int b) => new((Int128)a << b);

        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator ++(Int128Be a) => new((Int128)a + 1);

        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Be operator --(Int128Be a) => new((Int128)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int128Be"/> to an <see cref="Int128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int128(Int128Be a)
        {
            return (Int128)(((UInt128)(ulong)a.hi << 64) | (ulong)a.lo);
        }

        /// <summary>Implicitly converts an <see cref="Int128"/> to an <see cref="Int128Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int128Be(Int128 a) => new(a);

        /// <summary>Explicitly converts an <see cref="Int128Be"/> to a <see cref="long"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator long(Int128Be a) => (long)(Int128)a;

        /// <summary>Explicitly converts an <see cref="Int128Be"/> to a <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt128(Int128Be a) => (UInt128)(Int128)a;

        /// <summary>Widening conversion from a 64-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Int128Be(Int64Be a) => new((long)a);

        /// <summary>Widening conversion from a 32-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Int128Be(Int32Be a) => new((long)(int)a);

        /// <summary>Narrowing conversion to a 64-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int64Be(Int128Be a) => new((long)(Int128)a);

        /// <summary>Narrowing conversion to a 32-bit big-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int32Be(Int128Be a) => new((int)(Int128)a);

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int128Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int128Be(Int128.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int128Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Be Parse(string s, IFormatProvider? provider)
        {
            return new Int128Be(Int128.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int128Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int128Be(Int128.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int128Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int128Be result)
        {
            if (Int128.TryParse(s, provider, out Int128 value))
            {
                result = new Int128Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int128Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int128Be result)
        {
            if (Int128.TryParse(s, provider, out Int128 value))
            {
                result = new Int128Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override string ToString() => $"0x{(UInt128)(Int128)this:x32}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((Int128)this).ToString(format, formatProvider);
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
            return ((Int128)this).TryFormat(destination, out charsWritten, format, provider);
        }

        #endregion

        #region Comparison and Equality

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((Int128Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(Int128Be other)
        {
            return ((Int128)this).CompareTo((Int128)other);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int128Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Int128Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ((Int128)this).GetHashCode();
        }

        #endregion
    }
}
