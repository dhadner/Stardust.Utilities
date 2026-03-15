using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 32-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt32LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct UInt32Le : IComparable, IComparable<UInt32Le>, IEquatable<UInt32Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt32Le>, ISpanParsable<UInt32Le>
    {
        [FieldOffset(0)] internal UInt16Le lo;
        [FieldOffset(2)] internal UInt16Le hi;

        /// <summary>Initializes a new <see cref="UInt32Le"/> from high and low <see cref="UInt16Le"/> halves.</summary>
        /// <param name="hi">The high 16-bit half.</param>
        /// <param name="lo">The low 16-bit half.</param>
        public UInt32Le(UInt16Le hi, UInt16Le lo) { this.hi = hi; this.lo = lo; }

        /// <summary>Initializes a new <see cref="UInt32Le"/> from a <see cref="uint"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Le(uint num)
        {
            lo = (UInt16Le)(ushort)(num & 0xffff);
            hi = (UInt16Le)(ushort)(num >> 16);
        }

        /// <summary>Initializes a new <see cref="UInt32Le"/> from an <see cref="int"/> value.</summary>
        /// <param name="num">The value to store.</param>
        public UInt32Le(int num)
        {
            lo = (UInt16Le)(ushort)((uint)num & 0xffff);
            hi = (UInt16Le)(ushort)((uint)num >> 16);
        }

        /// <summary>Initializes a new <see cref="UInt32Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 4 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 4 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            lo = new UInt16Le(bytes);
            hi = new UInt16Le(bytes.Slice(2));
        }

        /// <summary>Initializes a new <see cref="UInt32Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public UInt32Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 4)
                throw new ArgumentException("Array is too short");
            lo.lo = bytes[offset + 0];
            lo.hi = bytes[offset + 1];
            hi.lo = bytes[offset + 2];
            hi.hi = bytes[offset + 3];
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = lo.lo;
            bytes[offset + 1] = lo.hi;
            bytes[offset + 2] = hi.lo;
            bytes[offset + 3] = hi.hi;
        }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 4 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            destination[0] = lo.lo;
            destination[1] = lo.hi;
            destination[2] = hi.lo;
            destination[3] = hi.hi;
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4) return false;
            destination[0] = lo.lo;
            destination[1] = lo.hi;
            destination[2] = hi.lo;
            destination[3] = hi.hi;
            return true;
        }

        /// <summary>Reads a <see cref="UInt32Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 4 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into a <see cref="UInt32Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(uint.Parse(s, style));
        }

        /// <summary>Parses a string into a <see cref="UInt32Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Le Parse(string s, IFormatProvider? provider) => new(uint.Parse(s, provider));
        /// <summary>Parses a character span into a <see cref="UInt32Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt32Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(uint.Parse(s, provider));

        /// <summary>Tries to parse a string into a <see cref="UInt32Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt32Le result)
        {
            if (uint.TryParse(s, provider, out uint v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into a <see cref="UInt32Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt32Le result)
        {
            if (uint.TryParse(s, provider, out uint v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(uint)this:x8}";
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((uint)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((uint)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt32Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt32Le other) => ((uint)this).CompareTo((uint)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt32Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(UInt32Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((uint)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static UInt32Le operator +(UInt32Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static UInt32Le operator -(UInt32Le a) => new((uint)-(int)(uint)a);
        /// <summary>Adds two values.</summary>
        public static UInt32Le operator +(UInt32Le a, UInt32Le b) => new((uint)a + (uint)b);
        /// <summary>Subtracts two values.</summary>
        public static UInt32Le operator -(UInt32Le a, UInt32Le b) => new((uint)a - (uint)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(UInt32Le a, UInt32Le b) => (uint)a > (uint)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(UInt32Le a, UInt32Le b) => (uint)a < (uint)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(UInt32Le a, UInt32Le b) => (uint)a >= (uint)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(UInt32Le a, UInt32Le b) => (uint)a <= (uint)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(UInt32Le a, UInt32Le b) => (uint)a == (uint)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(UInt32Le a, UInt32Le b) => (uint)a != (uint)b;
        /// <summary>Multiplies two values.</summary>
        public static UInt32Le operator *(UInt32Le a, UInt32Le b) => new((uint)a * (uint)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt32Le operator /(UInt32Le a, UInt32Le b)
        {
            if ((uint)b == 0) throw new DivideByZeroException();
            return new((uint)a / (uint)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        public static UInt32Le operator &(UInt32Le a, UInt32Le b) => new((uint)a & (uint)b);
        /// <summary>Computes the bitwise OR of two values.</summary>
        public static UInt32Le operator |(UInt32Le a, UInt32Le b) => new((uint)a | (uint)b);
        /// <summary>Shifts the value right by the specified amount.</summary>
        public static UInt32Le operator >>(UInt32Le a, int b) => new((uint)a >> b);
        /// <summary>Shifts the value left by the specified amount.</summary>
        public static UInt32Le operator <<(UInt32Le a, int b) => new((uint)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        public static UInt32Le operator ~(UInt32Le a) => new(~(uint)a);
        /// <summary>Increments the value by one.</summary>
        public static UInt32Le operator ++(UInt32Le a) => new((uint)a + 1);
        /// <summary>Decrements the value by one.</summary>
        public static UInt32Le operator --(UInt32Le a) => new((uint)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt32Le"/> to a <see cref="uint"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UInt32Le a) => (uint)((uint)(ushort)a.hi << 16) | (ushort)a.lo;

        /// <summary>Implicitly converts a <see cref="uint"/> to a <see cref="UInt32Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Le(uint a) => new(a);

        /// <summary>Implicitly converts a <see cref="UInt32Le"/> to a <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt32Le a) => (uint)a;

        /// <summary>Explicitly converts a <see cref="UInt32Le"/> to a <see cref="ushort"/>.</summary>
        public static explicit operator ushort(UInt32Le a) => (ushort)(uint)a;
        /// <summary>Explicitly converts a <see cref="UInt32Le"/> to an <see cref="int"/>.</summary>
        public static explicit operator int(UInt32Le a) => (int)(uint)a;

        #endregion
    }
}
