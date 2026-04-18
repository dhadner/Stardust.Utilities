using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 128-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int128LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Int128Le : IComparable, IComparable<Int128Le>, IEquatable<Int128Le>,
                              IFormattable, ISpanFormattable, IParsable<Int128Le>, ISpanParsable<Int128Le>
    {
        [FieldOffset(0)] internal UInt64Le lo;
        [FieldOffset(8)] internal UInt64Le hi;

        /// <summary>Initializes a new <see cref="Int128Le"/> from an <see cref="Int128"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128Le(Int128 num)
        {
            UInt128 u = (UInt128)num;
            lo = (UInt64Le)(ulong)u;
            hi = (UInt64Le)(ulong)(u >> 64);
        }

        /// <summary>Initializes a new <see cref="Int128Le"/> from a <see cref="long"/> value.</summary>
        /// <param name="num">The value to store.</param>
        public Int128Le(long num)
        {
            Int128 wide = num;
            UInt128 u = (UInt128)wide;
            lo = (UInt64Le)(ulong)u;
            hi = (UInt64Le)(ulong)(u >> 64);
        }

        /// <summary>Initializes a new <see cref="Int128Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 16 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 16 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 16) throw new ArgumentException("Span must have at least 16 bytes", nameof(bytes));
            lo = new UInt64Le(bytes);
            hi = new UInt64Le(bytes.Slice(8));
        }

        /// <summary>Initializes a new <see cref="Int128Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public Int128Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 16) throw new ArgumentException("Array is too short");
            lo = new UInt64Le(bytes, offset);
            hi = new UInt64Le(bytes, offset + 8);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0) { lo.ToBytes(bytes, offset); hi.ToBytes(bytes, offset + 8); }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 16 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 16) throw new ArgumentException("Destination span must have at least 16 bytes", nameof(destination));
            lo.WriteTo(destination); hi.WriteTo(destination.Slice(8));
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 16)
                return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(8));
            return true;
        }

        /// <summary>Reads an <see cref="Int128Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 16 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into an <see cref="Int128Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(Int128.Parse(s, style));
        }

        /// <summary>Parses a string into an <see cref="Int128Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Le Parse(string s, IFormatProvider? provider) => new(Int128.Parse(s, provider));

        /// <summary>Parses a character span into an <see cref="Int128Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int128Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(Int128.Parse(s, provider));

        /// <summary>Tries to parse a string into an <see cref="Int128Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int128Le result)
        {
            if (Int128.TryParse(s, provider, out Int128 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into an <see cref="Int128Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int128Le result)
        {
            if (Int128.TryParse(s, provider, out Int128 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ((Int128)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((Int128)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((Int128)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int128Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int128Le other) => ((Int128)this).CompareTo((Int128)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int128Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(Int128Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((Int128)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator +(Int128Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator -(Int128Le a) => new(-(Int128)a);
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator +(Int128Le a, Int128Le b) => new((Int128)a + (Int128)b);
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator -(Int128Le a, Int128Le b) => new((Int128)a - (Int128)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(Int128Le a, Int128Le b) => (Int128)a > (Int128)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(Int128Le a, Int128Le b) => (Int128)a < (Int128)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(Int128Le a, Int128Le b) => (Int128)a >= (Int128)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(Int128Le a, Int128Le b) => (Int128)a <= (Int128)b;
        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(Int128Le a, Int128Le b) => (Int128)a == (Int128)b;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(Int128Le a, Int128Le b) => (Int128)a != (Int128)b;
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator *(Int128Le a, Int128Le b) => new((Int128)a * (Int128)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator /(Int128Le a, Int128Le b)
        {
            if ((Int128)b == 0) throw new DivideByZeroException();
            return new((Int128)a / (Int128)b);
        }
        /// <summary>Computes the remainder of two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator %(Int128Le a, Int128Le b)
        {
            if ((Int128)b == 0) throw new DivideByZeroException();
            return new((Int128)a % (Int128)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator &(Int128Le a, Int128Le b) => new((Int128)((UInt128)(Int128)a & (UInt128)(Int128)b));
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator |(Int128Le a, Int128Le b) => new((Int128)((UInt128)(Int128)a | (UInt128)(Int128)b));
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator ^(Int128Le a, Int128Le b) => new((Int128)((UInt128)(Int128)a ^ (UInt128)(Int128)b));
        /// <summary>Shifts the value right by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator >>(Int128Le a, int b) => new((Int128)a >> b);
        /// <summary>Performs an unsigned (logical) right shift by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128Le operator >>>(Int128Le a, int b) => new((Int128)((UInt128)(Int128)a >> b));
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator <<(Int128Le a, int b) => new((Int128)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator ~(Int128Le a) => new((Int128)(~(UInt128)(Int128)a));
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator ++(Int128Le a) => new((Int128)a + 1);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Int128Le operator --(Int128Le a) => new((Int128)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int128Le"/> to an <see cref="Int128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int128(Int128Le a) => (Int128)(((UInt128)(ulong)a.hi << 64) | (ulong)a.lo);

        /// <summary>Implicitly converts an <see cref="Int128"/> to an <see cref="Int128Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int128Le(Int128 a) => new(a);

        /// <summary>Explicitly converts an <see cref="Int128Le"/> to a <see cref="long"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator long(Int128Le a) => (long)(Int128)a;
        /// <summary>Explicitly converts an <see cref="Int128Le"/> to a <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt128(Int128Le a) => (UInt128)(Int128)a;

        /// <summary>Widening conversion from a 64-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Int128Le(Int64Le a) => new((long)a);

        /// <summary>Widening conversion from a 32-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator Int128Le(Int32Le a) => new((long)(int)a);

        /// <summary>Narrowing conversion to a 64-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int64Le(Int128Le a) => new((long)(Int128)a);

        /// <summary>Narrowing conversion to a 32-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int32Le(Int128Le a) => new((int)(Int128)a);

        #endregion
    }
}
