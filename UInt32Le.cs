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
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            Unsafe.As<UInt32Le, uint>(ref this) = BinaryPrimitives.ReverseEndianness(num);
#else
            Unsafe.As<UInt32Le, uint>(ref this) = num;
#endif
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

        /// <summary>
        /// Initializes a new <see cref="UInt32Le"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 4 bytes).</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Le(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 4; i++) dst[i] = bytes[3 - i];
            }
            else
            {
                bytes[..4].CopyTo(dst);
            }
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

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = false)
        {
            if (isBigEndian)
            {
                bytes[offset + 0] = hi.hi;
                bytes[offset + 1] = hi.lo;
                bytes[offset + 2] = lo.hi;
                bytes[offset + 3] = lo.lo;
            }
            else
            {
                bytes[offset + 0] = lo.lo;
                bytes[offset + 1] = lo.hi;
                bytes[offset + 2] = hi.lo;
                bytes[offset + 3] = hi.hi;
            }
        }

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">A span with at least 4 bytes of space.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 4)
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            if (isBigEndian)
            {
                destination[0] = hi.hi;
                destination[1] = hi.lo;
                destination[2] = lo.hi;
                destination[3] = lo.lo;
            }
            else
            {
                destination[0] = lo.lo;
                destination[1] = lo.hi;
                destination[2] = hi.lo;
                destination[3] = hi.hi;
            }
        }

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 4) return false;
            if (isBigEndian)
            {
                destination[0] = hi.hi;
                destination[1] = hi.lo;
                destination[2] = lo.hi;
                destination[3] = lo.lo;
            }
            else
            {
                destination[0] = lo.lo;
                destination[1] = lo.hi;
                destination[2] = hi.lo;
                destination[3] = hi.hi;
            }
            return true;
        }

        /// <summary>Reads a <see cref="UInt32Le"/> from a read-only byte span in the specified byte order.</summary>
        /// <param name="source">A span containing at least 4 bytes.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le ReadFrom(ReadOnlySpan<byte> source, bool isBigEndian = false) => new(source, isBigEndian);

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
        public override string ToString() => $"0x{(uint)this:x8}";
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
        public override bool Equals(object? obj) => obj != null && Equals((UInt32Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(UInt32Le other) => this == other;
        /// <inheritdoc/>
        public override int GetHashCode() => ((uint)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator +(UInt32Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator -(UInt32Le a) => new((uint)-(int)(uint)a);
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator +(UInt32Le a, UInt32Le b) => new((uint)a + (uint)b);
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator -(UInt32Le a, UInt32Le b) => new((uint)a - (uint)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(UInt32Le a, UInt32Le b) => (uint)a > (uint)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(UInt32Le a, UInt32Le b) => (uint)a < (uint)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(UInt32Le a, UInt32Le b) => (uint)a >= (uint)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(UInt32Le a, UInt32Le b) => (uint)a <= (uint)b;
        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(UInt32Le a, UInt32Le b) => (uint)a == (uint)b;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(UInt32Le a, UInt32Le b) => (uint)a != (uint)b;
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator *(UInt32Le a, UInt32Le b) => new((uint)a * (uint)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator /(UInt32Le a, UInt32Le b)
        {
            if ((uint)b == 0) throw new DivideByZeroException();
            return new((uint)a / (uint)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator &(UInt32Le a, UInt32Le b) => new((uint)a & (uint)b);
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator |(UInt32Le a, UInt32Le b) => new((uint)a | (uint)b);
        /// <summary>Shifts the value right by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator >>(UInt32Le a, int b) => new((uint)a >> b);
        /// <summary>Performs an unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le operator >>>(UInt32Le a, int b) => new((uint)a >> b);
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator <<(UInt32Le a, int b) => new((uint)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator ~(UInt32Le a) => new(~(uint)a);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator ++(UInt32Le a) => new((uint)a + 1);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt32Le operator --(UInt32Le a) => new((uint)a - 1);
        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le operator %(UInt32Le a, UInt32Le b)
        {
            if ((uint)b == 0) throw new DivideByZeroException();
            return new((uint)a % (uint)b);
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le operator ^(UInt32Le a, UInt32Le b) => new((uint)a ^ (uint)b);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt32Le"/> to a <see cref="uint"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if BIG_ENDIAN
        public static implicit operator uint(UInt32Le a) => BinaryPrimitives.ReverseEndianness(Unsafe.As<UInt32Le, uint>(ref a));
#else
        public static implicit operator uint(UInt32Le a) => Unsafe.As<UInt32Le, uint>(ref a);
#endif

        /// <summary>Implicitly converts a <see cref="uint"/> to a <see cref="UInt32Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Le(uint a) => new(a);

        /// <summary>Implicitly converts a <see cref="UInt32Le"/> to a <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt32Le a) => (uint)a;

        /// <summary>Explicitly converts a <see cref="UInt32Le"/> to a <see cref="ushort"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator ushort(UInt32Le a) => (ushort)(uint)a;
        /// <summary>Explicitly converts a <see cref="UInt32Le"/> to an <see cref="int"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator int(UInt32Le a) => (int)(uint)a;

        /// <summary>Narrowing conversion to a 16-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt16Le(UInt32Le a) => new((ushort)(uint)a);

        #endregion
    }
}
