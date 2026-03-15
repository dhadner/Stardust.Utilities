using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 16-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int16LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Int16Le : IComparable, IComparable<Int16Le>, IEquatable<Int16Le>,
                             IFormattable, ISpanFormattable, IParsable<Int16Le>, ISpanParsable<Int16Le>
    {
        [FieldOffset(0)] internal byte lo;
        [FieldOffset(1)] internal byte hi;

        /// <summary>Initializes a new <see cref="Int16Le"/> from a <see cref="short"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Le(short num)
        {
            lo = (byte)(num & 0xff);
            hi = (byte)(num >> 8);
        }

        /// <summary>Initializes a new <see cref="Int16Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public Int16Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
                throw new ArgumentException("offset too large");
            lo = bytes[offset + 0];
            hi = bytes[offset + 1];
        }

        /// <summary>Initializes a new <see cref="Int16Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 2 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 2 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            lo = bytes[0];
            hi = bytes[1];
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0) { bytes[offset] = lo; bytes[offset + 1] = hi; }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 2 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 2) throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            destination[0] = lo; destination[1] = hi;
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
                return false;
            destination[0] = lo;
            destination[1] = hi;
            return true;
        }

        /// <summary>Reads an <see cref="Int16Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 2 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into an <see cref="Int16Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(short.Parse(s, style));
        }

        /// <summary>Parses a string into an <see cref="Int16Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Le Parse(string s, IFormatProvider? provider) => new(short.Parse(s, provider));
        /// <summary>Parses a character span into an <see cref="Int16Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int16Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(short.Parse(s, provider));

        /// <summary>Tries to parse a string into an <see cref="Int16Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int16Le result)
        {
            if (short.TryParse(s, provider, out short v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into an <see cref="Int16Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int16Le result)
        {
            if (short.TryParse(s, provider, out short v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ((short)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((short)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((short)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int16Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int16Le other) => ((short)this).CompareTo((short)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int16Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(Int16Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((short)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static Int16Le operator +(Int16Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static Int16Le operator -(Int16Le a) => new((short)-(short)a);
        /// <summary>Adds two values.</summary>
        public static Int16Le operator +(Int16Le a, Int16Le b) => new((short)((short)a + (short)b));
        /// <summary>Subtracts two values.</summary>
        public static Int16Le operator -(Int16Le a, Int16Le b) => new((short)((short)a - (short)b));
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(Int16Le a, Int16Le b) => (short)a > (short)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(Int16Le a, Int16Le b) => (short)a < (short)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(Int16Le a, Int16Le b) => (short)a >= (short)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(Int16Le a, Int16Le b) => (short)a <= (short)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(Int16Le a, Int16Le b) => (short)a == (short)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(Int16Le a, Int16Le b) => (short)a != (short)b;
        /// <summary>Increments the value by one.</summary>
        public static Int16Le operator ++(Int16Le a) => new((short)((short)a + 1));
        /// <summary>Decrements the value by one.</summary>
        public static Int16Le operator --(Int16Le a) => new((short)((short)a - 1));

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int16Le"/> to a <see cref="short"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(Int16Le a) => (short)((ushort)(a.hi << 8) | a.lo);

        /// <summary>Implicitly converts a <see cref="short"/> to an <see cref="Int16Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int16Le(short a) => new(a);

        /// <summary>Implicitly converts an <see cref="Int16Le"/> to an <see cref="int"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int16Le a) => (short)a;

        #endregion
    }
}
