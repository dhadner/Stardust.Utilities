using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 64-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int64LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Int64Le : IComparable, IComparable<Int64Le>, IEquatable<Int64Le>,
                             IFormattable, ISpanFormattable, IParsable<Int64Le>, ISpanParsable<Int64Le>
    {
        [FieldOffset(0)] internal UInt32Le lo;
        [FieldOffset(4)] internal UInt32Le hi;

        /// <summary>Initializes a new <see cref="Int64Le"/> from a <see cref="long"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Le(long num)
        {
            lo = (UInt32Le)(uint)((ulong)num & 0xffffffff);
            hi = (UInt32Le)(uint)((ulong)num >> 32);
        }

        /// <summary>Initializes a new <see cref="Int64Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 8 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 8 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8) throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            lo = new UInt32Le(bytes);
            hi = new UInt32Le(bytes.Slice(4));
        }

        /// <summary>Initializes a new <see cref="Int64Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public Int64Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 8) throw new ArgumentException("Array is too short");
            lo = new UInt32Le(bytes, offset);
            hi = new UInt32Le(bytes, offset + 4);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0) { lo.ToBytes(bytes, offset); hi.ToBytes(bytes, offset + 4); }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 8 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8) throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            lo.WriteTo(destination); hi.WriteTo(destination.Slice(4));
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(4));
            return true;
        }

        /// <summary>Reads an <see cref="Int64Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 8 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into an <see cref="Int64Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(long.Parse(s, style));
        }

        /// <summary>Parses a string into an <see cref="Int64Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Le Parse(string s, IFormatProvider? provider) => new(long.Parse(s, provider));
        /// <summary>Parses a character span into an <see cref="Int64Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int64Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(long.Parse(s, provider));

        /// <summary>Tries to parse a string into an <see cref="Int64Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int64Le result)
        {
            if (long.TryParse(s, provider, out long v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into an <see cref="Int64Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int64Le result)
        {
            if (long.TryParse(s, provider, out long v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ((long)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((long)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((long)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int64Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int64Le other) => ((long)this).CompareTo((long)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int64Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(Int64Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((long)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static Int64Le operator +(Int64Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static Int64Le operator -(Int64Le a) => new(-(long)a);
        /// <summary>Adds two values.</summary>
        public static Int64Le operator +(Int64Le a, Int64Le b) => new((long)a + (long)b);
        /// <summary>Subtracts two values.</summary>
        public static Int64Le operator -(Int64Le a, Int64Le b) => new((long)a - (long)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(Int64Le a, Int64Le b) => (long)a > (long)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(Int64Le a, Int64Le b) => (long)a < (long)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(Int64Le a, Int64Le b) => (long)a >= (long)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(Int64Le a, Int64Le b) => (long)a <= (long)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(Int64Le a, Int64Le b) => (long)a == (long)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(Int64Le a, Int64Le b) => (long)a != (long)b;
        /// <summary>Increments the value by one.</summary>
        public static Int64Le operator ++(Int64Le a) => new((long)a + 1);
        /// <summary>Decrements the value by one.</summary>
        public static Int64Le operator --(Int64Le a) => new((long)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int64Le"/> to a <see cref="long"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int64Le a) => (long)(((ulong)(uint)a.hi << 32) | (uint)a.lo);

        /// <summary>Implicitly converts a <see cref="long"/> to an <see cref="Int64Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int64Le(long a) => new(a);

        /// <summary>Explicitly converts an <see cref="Int64Le"/> to a <see cref="ulong"/>.</summary>
        public static explicit operator ulong(Int64Le a) => (ulong)(long)a;

        #endregion
    }
}
