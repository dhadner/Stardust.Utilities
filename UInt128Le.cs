using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 128-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt128LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct UInt128Le : IComparable, IComparable<UInt128Le>, IEquatable<UInt128Le>,
                               IFormattable, ISpanFormattable, IParsable<UInt128Le>, ISpanParsable<UInt128Le>
    {
        [FieldOffset(0)] internal UInt64Le lo;
        [FieldOffset(8)] internal UInt64Le hi;

        /// <summary>Initializes a new <see cref="UInt128Le"/> from a <see cref="UInt128"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128Le(UInt128 num)
        {
            lo = (UInt64Le)(ulong)num;
            hi = (UInt64Le)(ulong)(num >> 64);
        }

        /// <summary>Initializes a new <see cref="UInt128Le"/> from a <see cref="ulong"/> value.</summary>
        /// <param name="num">The value to store.</param>
        public UInt128Le(ulong num)
        {
            lo = (UInt64Le)num;
            hi = (UInt64Le)0UL;
        }

        /// <summary>Initializes a new <see cref="UInt128Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 16 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 16 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 16) throw new ArgumentException("Span must have at least 16 bytes", nameof(bytes));
            lo = new UInt64Le(bytes);
            hi = new UInt64Le(bytes.Slice(8));
        }

        /// <summary>Initializes a new <see cref="UInt128Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public UInt128Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 16) throw new ArgumentException("Array is too short");
            lo = new UInt64Le(bytes, offset);
            hi = new UInt64Le(bytes, offset + 8);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 8);
        }

        /// <summary>Writes the value to a destination span.</summary>
        /// <param name="destination">A span with at least 16 bytes of space.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 16) throw new ArgumentException("Destination span must have at least 16 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(8));
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

        /// <summary>Reads a <see cref="UInt128Le"/> from a read-only byte span.</summary>
        /// <param name="source">A span containing at least 16 bytes.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into a <see cref="UInt128Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(UInt128.Parse(s, style));
        }

        /// <summary>Parses a string into a <see cref="UInt128Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Le Parse(string s, IFormatProvider? provider) => new(UInt128.Parse(s, provider));

        /// <summary>Parses a character span into a <see cref="UInt128Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt128Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(UInt128.Parse(s, provider));

        /// <summary>Tries to parse a string into a <see cref="UInt128Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt128Le result)
        {
            if (UInt128.TryParse(s, provider, out UInt128 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into a <see cref="UInt128Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt128Le result)
        {
            if (UInt128.TryParse(s, provider, out UInt128 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(UInt128)this:x32}";
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((UInt128)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((UInt128)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt128Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt128Le other) => ((UInt128)this).CompareTo((UInt128)other);
        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt128Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(UInt128Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((UInt128)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        public static UInt128Le operator +(UInt128Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        public static UInt128Le operator -(UInt128Le a) => new((UInt128)(-(Int128)(UInt128)a));
        /// <summary>Adds two values.</summary>
        public static UInt128Le operator +(UInt128Le a, UInt128Le b) => new((UInt128)a + (UInt128)b);
        /// <summary>Subtracts two values.</summary>
        public static UInt128Le operator -(UInt128Le a, UInt128Le b) => new((UInt128)a - (UInt128)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        public static bool operator >(UInt128Le a, UInt128Le b) => (UInt128)a > (UInt128)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        public static bool operator <(UInt128Le a, UInt128Le b) => (UInt128)a < (UInt128)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        public static bool operator >=(UInt128Le a, UInt128Le b) => (UInt128)a >= (UInt128)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        public static bool operator <=(UInt128Le a, UInt128Le b) => (UInt128)a <= (UInt128)b;
        /// <summary>Determines whether two values are equal.</summary>
        public static bool operator ==(UInt128Le a, UInt128Le b) => (UInt128)a == (UInt128)b;
        /// <summary>Determines whether two values are not equal.</summary>
        public static bool operator !=(UInt128Le a, UInt128Le b) => (UInt128)a != (UInt128)b;
        /// <summary>Multiplies two values.</summary>
        public static UInt128Le operator *(UInt128Le a, UInt128Le b) => new((UInt128)a * (UInt128)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt128Le operator /(UInt128Le a, UInt128Le b)
        {
            if ((UInt128)b == 0) throw new DivideByZeroException();
            return new((UInt128)a / (UInt128)b);
        }
        /// <summary>Computes the remainder of two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt128Le operator %(UInt128Le a, UInt128Le b)
        {
            if ((UInt128)b == 0) throw new DivideByZeroException();
            return new((UInt128)a % (UInt128)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        public static UInt128Le operator &(UInt128Le a, UInt128Le b) => new((UInt128)a & (UInt128)b);
        /// <summary>Computes the bitwise OR of two values.</summary>
        public static UInt128Le operator |(UInt128Le a, UInt128Le b) => new((UInt128)a | (UInt128)b);
        /// <summary>Computes the bitwise XOR of two values.</summary>
        public static UInt128Le operator ^(UInt128Le a, UInt128Le b) => new((UInt128)a ^ (UInt128)b);
        /// <summary>Shifts the value right by the specified amount.</summary>
        public static UInt128Le operator >>(UInt128Le a, int b) => new((UInt128)a >> b);
        /// <summary>Shifts the value left by the specified amount.</summary>
        public static UInt128Le operator <<(UInt128Le a, int b) => new((UInt128)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        public static UInt128Le operator ~(UInt128Le a) => new(~(UInt128)a);
        /// <summary>Increments the value by one.</summary>
        public static UInt128Le operator ++(UInt128Le a) => new((UInt128)a + 1);
        /// <summary>Decrements the value by one.</summary>
        public static UInt128Le operator --(UInt128Le a) => new((UInt128)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt128Le"/> to a <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128(UInt128Le a) => ((UInt128)(ulong)a.hi << 64) | (ulong)a.lo;

        /// <summary>Implicitly converts a <see cref="UInt128"/> to a <see cref="UInt128Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128Le(UInt128 a) => new(a);

        /// <summary>Explicitly converts a <see cref="UInt128Le"/> to a <see cref="ulong"/>.</summary>
        public static explicit operator ulong(UInt128Le a) => (ulong)(UInt128)a;
        /// <summary>Explicitly converts a <see cref="UInt128Le"/> to an <see cref="Int128"/>.</summary>
        public static explicit operator Int128(UInt128Le a) => (Int128)(UInt128)a;

        /// <summary>Widening conversion from a 64-bit little-endian unsigned value.</summary>
        public static implicit operator UInt128Le(UInt64Le a) => new((ulong)a);

        /// <summary>Widening conversion from a 32-bit little-endian unsigned value.</summary>
        public static implicit operator UInt128Le(UInt32Le a) => new((ulong)(uint)a);

        /// <summary>Narrowing conversion to a 64-bit little-endian unsigned value.</summary>
        public static explicit operator UInt64Le(UInt128Le a) => new((ulong)(UInt128)a);

        /// <summary>Narrowing conversion to a 32-bit little-endian unsigned value.</summary>
        public static explicit operator UInt32Le(UInt128Le a) => new((uint)(UInt128)a);

        #endregion
    }
}
