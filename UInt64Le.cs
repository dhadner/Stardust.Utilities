using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 64-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt64LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct UInt64Le : IComparable, IComparable<UInt64Le>, IEquatable<UInt64Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt64Le>, ISpanParsable<UInt64Le>
    {
        [FieldOffset(0)] internal UInt32Le lo;
        [FieldOffset(4)] internal UInt32Le hi;

        /// <summary>Initializes a new <see cref="UInt64Le"/> from a <see cref="ulong"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Le(ulong num)
        {
            lo = (UInt32Le)(uint)(num & 0xffffffff);
            hi = (UInt32Le)(uint)(num >> 32);
        }

        /// <summary>Initializes a new <see cref="UInt64Le"/> from a <see cref="long"/> value.</summary>
        /// <param name="num">The value to store.</param>
        public UInt64Le(long num)
        {
            lo = (UInt32Le)(uint)((ulong)num & 0xffffffff);
            hi = (UInt32Le)(uint)((ulong)num >> 32);
        }

        /// <summary>Initializes a new <see cref="UInt64Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 8 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 8 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
                throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            lo = new UInt32Le(bytes);
            hi = new UInt32Le(bytes.Slice(4));
        }

        /// <summary>Initializes a new <see cref="UInt64Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public UInt64Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 8)
                throw new ArgumentException("Array is too short");
            lo = new UInt32Le(bytes, offset);
            hi = new UInt32Le(bytes, offset + 4);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 4);
        }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 8 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(4));
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

        /// <summary>Reads a <see cref="UInt64Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 8 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into a <see cref="UInt64Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static UInt64Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(ulong.Parse(s, style));
        }

        /// <summary>Parses a string into a <see cref="UInt64Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt64Le Parse(string s, IFormatProvider? provider) => new(ulong.Parse(s, provider));
        /// <summary>Parses a character span into a <see cref="UInt64Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt64Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(ulong.Parse(s, provider));

        /// <summary>Tries to parse a string into a <see cref="UInt64Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt64Le result)
        {
            if (ulong.TryParse(s, provider, out ulong v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into a <see cref="UInt64Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt64Le result)
        {
            if (ulong.TryParse(s, provider, out ulong v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(ulong)this:x16}";
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((ulong)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((ulong)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt64Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt64Le other) => ((ulong)this).CompareTo((ulong)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt64Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(UInt64Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((ulong)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static UInt64Le operator +(UInt64Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static UInt64Le operator -(UInt64Le a) => new((ulong)-(long)(ulong)a);
        /// <summary>Adds two values.</summary>
        public static UInt64Le operator +(UInt64Le a, UInt64Le b) => new((ulong)a + (ulong)b);
        /// <summary>Subtracts two values.</summary>
        public static UInt64Le operator -(UInt64Le a, UInt64Le b) => new((ulong)a - (ulong)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(UInt64Le a, UInt64Le b) => (ulong)a > (ulong)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(UInt64Le a, UInt64Le b) => (ulong)a < (ulong)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(UInt64Le a, UInt64Le b) => (ulong)a >= (ulong)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(UInt64Le a, UInt64Le b) => (ulong)a <= (ulong)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(UInt64Le a, UInt64Le b) => (ulong)a == (ulong)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(UInt64Le a, UInt64Le b) => (ulong)a != (ulong)b;
        /// <summary>Multiplies two values.</summary>
        public static UInt64Le operator *(UInt64Le a, UInt64Le b) => new((ulong)a * (ulong)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt64Le operator /(UInt64Le a, UInt64Le b)
        {
            if ((ulong)b == 0) throw new DivideByZeroException();
            return new((ulong)a / (ulong)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        public static UInt64Le operator &(UInt64Le a, UInt64Le b) => new((ulong)a & (ulong)b);
        /// <summary>Computes the bitwise OR of two values.</summary>
        public static UInt64Le operator |(UInt64Le a, UInt64Le b) => new((ulong)a | (ulong)b);
        /// <summary>Shifts the value right by the specified amount.</summary>
        public static UInt64Le operator >>(UInt64Le a, int b) => new((ulong)a >> b);
        /// <summary>Shifts the value left by the specified amount.</summary>
        public static UInt64Le operator <<(UInt64Le a, int b) => new((ulong)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        public static UInt64Le operator ~(UInt64Le a) => new(~(ulong)a);
        /// <summary>Increments the value by one.</summary>
        public static UInt64Le operator ++(UInt64Le a) => new((ulong)a + 1);
        /// <summary>Decrements the value by one.</summary>
        public static UInt64Le operator --(UInt64Le a) => new((ulong)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt64Le"/> to a <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt64Le a) => ((ulong)(uint)a.hi << 32) | (uint)a.lo;

        /// <summary>Implicitly converts a <see cref="ulong"/> to a <see cref="UInt64Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt64Le(ulong a) => new(a);

        /// <summary>Explicitly converts a <see cref="UInt64Le"/> to a <see cref="uint"/>.</summary>
        public static explicit operator uint(UInt64Le a) => (uint)(ulong)a;
        /// <summary>Explicitly converts a <see cref="UInt64Le"/> to a <see cref="long"/>.</summary>
        public static explicit operator long(UInt64Le a) => (long)(ulong)a;

        #endregion
    }
}
