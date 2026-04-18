using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 128-bit unsigned integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt128BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct UInt128Be : IComparable, IComparable<UInt128Be>, IEquatable<UInt128Be>,
                               IFormattable, ISpanFormattable, IParsable<UInt128Be>, ISpanParsable<UInt128Be>
    {
        [FieldOffset(0)] internal UInt64Be hi;
        [FieldOffset(8)] internal UInt64Be lo;

        /// <summary>
        /// Creates a big-endian 128-bit unsigned integer from two UInt64Be values.
        /// </summary>
        /// <param name="hi">The high 64 bits.</param>
        /// <param name="lo">The low 64 bits.</param>
        public UInt128Be(UInt64Be hi, UInt64Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 128-bit unsigned integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 16 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public UInt128Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 16)
            {
                throw new ArgumentException("List is too short");
            }
            hi = new UInt64Be(bytes, offset);
            lo = new UInt64Be(bytes, offset + 8);
        }

        /// <summary>
        /// Creates a big-endian 128-bit unsigned integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 16 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 16)
            {
                throw new ArgumentException("Span must have at least 16 bytes", nameof(bytes));
            }
            hi = new UInt64Be(bytes);
            lo = new UInt64Be(bytes.Slice(8));
        }

        /// <summary>
        /// Creates a big-endian 128-bit unsigned integer from a native UInt128.
        /// </summary>
        /// <param name="num">The native value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128Be(UInt128 num)
        {
            hi = new UInt64Be((ulong)(num >> 64));
            lo = new UInt64Be((ulong)num);
        }

        /// <summary>
        /// Creates a big-endian 128-bit unsigned integer from a native ulong.
        /// </summary>
        /// <param name="num">The native value.</param>
        public UInt128Be(ulong num)
        {
            hi = new UInt64Be(0UL);
            lo = new UInt64Be(num);
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
        /// Reads a big-endian UInt128 from a ReadOnlySpan.
        /// </summary>
        /// <param name="source">Source span.</param>
        /// <returns>The parsed big-endian value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new UInt128Be(source);
        }

        #region Operators

        /// <summary>Returns the value unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator +(UInt128Be a) => a;

        /// <summary>Negates the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator -(UInt128Be a) => new((UInt128)(-(Int128)(UInt128)a));

        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator +(UInt128Be a, UInt128Be b)
            => new((UInt128)a + (UInt128)b);

        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator -(UInt128Be a, UInt128Be b)
            => new((UInt128)a - (UInt128)b);

        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator *(UInt128Be a, UInt128Be b)
            => new((UInt128)a * (UInt128)b);

        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator /(UInt128Be a, UInt128Be b)
        {
            if ((UInt128)b == 0)
            {
                throw new DivideByZeroException();
            }
            return new((UInt128)a / (UInt128)b);
        }

        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator %(UInt128Be a, UInt128Be b)
        {
            if ((UInt128)b == 0)
            {
                throw new DivideByZeroException();
            }
            return new((UInt128)a % (UInt128)b);
        }

        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(UInt128Be a, UInt128Be b) => (UInt128)a > (UInt128)b;

        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(UInt128Be a, UInt128Be b) => (UInt128)a < (UInt128)b;

        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(UInt128Be a, UInt128Be b) => (UInt128)a >= (UInt128)b;

        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(UInt128Be a, UInt128Be b) => (UInt128)a <= (UInt128)b;

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(UInt128Be a, UInt128Be b) => (UInt128)a == (UInt128)b;

        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(UInt128Be a, UInt128Be b) => (UInt128)a != (UInt128)b;

        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator &(UInt128Be a, UInt128Be b) => new((UInt128)a & (UInt128)b);

        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator |(UInt128Be a, UInt128Be b) => new((UInt128)a | (UInt128)b);

        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator ^(UInt128Be a, UInt128Be b) => new((UInt128)a ^ (UInt128)b);

        /// <summary>Computes the bitwise complement of a value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator ~(UInt128Be a) => new(~(UInt128)a);

        /// <summary>Shifts a value right by the specified number of bits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator >>(UInt128Be a, int b) => new((UInt128)a >> b);

        /// <summary>Performs an unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128Be operator >>>(UInt128Be a, int b) => new((UInt128)a >> b);

        /// <summary>Shifts a value left by the specified number of bits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator <<(UInt128Be a, int b) => new((UInt128)a << b);

        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator ++(UInt128Be a) => new((UInt128)a + 1);

        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Be operator --(UInt128Be a) => new((UInt128)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt128Be"/> to a <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128(UInt128Be a)
        {
            return ((UInt128)(ulong)a.hi << 64) | (ulong)a.lo;
        }

        /// <summary>Implicitly converts a <see cref="UInt128"/> to a <see cref="UInt128Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128Be(UInt128 a) => new(a);

        /// <summary>Explicitly converts a <see cref="UInt128Be"/> to a <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator ulong(UInt128Be a) => (ulong)(UInt128)a;

        /// <summary>Explicitly converts a <see cref="UInt128Be"/> to an <see cref="Int128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int128(UInt128Be a) => (Int128)(UInt128)a;

        /// <summary>Widening conversion from a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator UInt128Be(UInt64Be a) => new(new UInt64Be(0UL), a);

        /// <summary>Widening conversion from a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator UInt128Be(UInt32Be a) => new(new UInt64Be(0UL), new UInt64Be((uint)a));

        /// <summary>Narrowing conversion to a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt64Be(UInt128Be a) => a.lo;

        /// <summary>Narrowing conversion to a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt32Be(UInt128Be a) => (UInt32Be)a.lo;

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to a UInt128Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt128Be(UInt128.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to a UInt128Be using the specified format provider.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Be Parse(string s, IFormatProvider? provider)
        {
            return new UInt128Be(UInt128.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to a UInt128Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new UInt128Be(UInt128.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to a UInt128Be.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt128Be result)
        {
            if (UInt128.TryParse(s, provider, out UInt128 value))
            {
                result = new UInt128Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to a UInt128Be.
        /// </summary>
        /// <param name="s">The characters to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns><see langword="true"/> if parsing succeeds; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt128Be result)
        {
            if (UInt128.TryParse(s, provider, out UInt128 value))
            {
                result = new UInt128Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>The formatted string.</returns>
        public override readonly string ToString() => $"0x{(UInt128)this:x32}";

        /// <summary>
        /// Returns a string representation of the value using the specified format.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>The formatted string.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((UInt128)this).ToString(format, formatProvider);
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
            return ((UInt128)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((UInt128Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(UInt128Be other)
        {
            return ((UInt128)this).CompareTo((UInt128)other);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt128Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(UInt128Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((UInt128)this).GetHashCode();
        }

        #endregion
    }
}
