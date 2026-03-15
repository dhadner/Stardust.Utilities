using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 32-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int32LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Int32Le : IComparable, IComparable<Int32Le>, IEquatable<Int32Le>,
                             IFormattable, ISpanFormattable, IParsable<Int32Le>, ISpanParsable<Int32Le>
    {
        [FieldOffset(0)] internal UInt16Le lo;
        [FieldOffset(2)] internal UInt16Le hi;

        /// <summary>Initializes a new <see cref="Int32Le"/> from an <see cref="int"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Le(int num)
        {
            lo = (UInt16Le)(ushort)((uint)num & 0xffff);
            hi = (UInt16Le)(ushort)((uint)num >> 16);
        }

        /// <summary>Initializes a new <see cref="Int32Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 4 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 4 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4) throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            lo = new UInt16Le(bytes);
            hi = new UInt16Le(bytes.Slice(2));
        }

        /// <summary>Initializes a new <see cref="Int32Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public Int32Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 4) throw new ArgumentException("Array is too short");
            lo = new UInt16Le(bytes, offset);
            hi = new UInt16Le(bytes, offset + 2);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 2);
        }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 4 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4) throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(2));
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(2));
            return true;
        }

        /// <summary>Reads an <see cref="Int32Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 4 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into an <see cref="Int32Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(int.Parse(s, style));
        }

        /// <summary>Parses a string into an <see cref="Int32Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Le Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider));
        /// <summary>Parses a character span into an <see cref="Int32Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int32Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(int.Parse(s, provider));

        /// <summary>Tries to parse a string into an <see cref="Int32Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int32Le result)
        {
            if (int.TryParse(s, provider, out int v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into an <see cref="Int32Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int32Le result)
        {
            if (int.TryParse(s, provider, out int v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ((int)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((int)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((int)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int32Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int32Le other) => ((int)this).CompareTo((int)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int32Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(Int32Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((int)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static Int32Le operator +(Int32Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static Int32Le operator -(Int32Le a) => new(-(int)a);
        /// <summary>Adds two values.</summary>
        public static Int32Le operator +(Int32Le a, Int32Le b) => new((int)a + (int)b);
        /// <summary>Subtracts two values.</summary>
        public static Int32Le operator -(Int32Le a, Int32Le b) => new((int)a - (int)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(Int32Le a, Int32Le b) => (int)a > (int)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(Int32Le a, Int32Le b) => (int)a < (int)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(Int32Le a, Int32Le b) => (int)a >= (int)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(Int32Le a, Int32Le b) => (int)a <= (int)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(Int32Le a, Int32Le b) => (int)a == (int)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(Int32Le a, Int32Le b) => (int)a != (int)b;
        /// <summary>Increments the value by one.</summary>
        public static Int32Le operator ++(Int32Le a) => new((int)a + 1);
        /// <summary>Decrements the value by one.</summary>
        public static Int32Le operator --(Int32Le a) => new((int)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int32Le"/> to an <see cref="int"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int32Le a) => (int)(((uint)(ushort)a.hi << 16) | (ushort)a.lo);

        /// <summary>Implicitly converts an <see cref="int"/> to an <see cref="Int32Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int32Le(int a) => new(a);

        /// <summary>Implicitly converts an <see cref="Int32Le"/> to a <see cref="long"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int32Le a) => (int)a;

        /// <summary>Explicitly converts an <see cref="Int32Le"/> to a <see cref="uint"/>.</summary>
        public static explicit operator uint(Int32Le a) => (uint)(int)a;

        #endregion
    }
}
