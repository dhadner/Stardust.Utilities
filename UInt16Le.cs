using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 16-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt16LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct UInt16Le : IComparable, IComparable<UInt16Le>, IEquatable<UInt16Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt16Le>, ISpanParsable<UInt16Le>
    {
        [FieldOffset(0)] internal byte lo;
        [FieldOffset(1)] internal byte hi;
        // Overlapping native field gives the JIT a single primitive to keep in a
        // register. On LE hosts (x86/x64/ARM), bytes [lo, hi] = native ushort directly.
        [FieldOffset(0)] private ushort _value;

        /// <summary>Initializes a new <see cref="UInt16Le"/> from a <see cref="ushort"/> value.</summary>
        /// <param name="num">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Le(ushort num)
        {
            // SkipInit suppresses the definite-assignment check so we can write the
            // overlapping _value field once without first zeroing lo/hi separately.
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            _value = BinaryPrimitives.ReverseEndianness(num);
#else
            _value = num;
#endif
        }

        /// <summary>Initializes a new <see cref="UInt16Le"/> from an <see cref="int"/> value.</summary>
        /// <param name="num">The value to store. Must be within <see cref="ushort"/> range.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="num"/> is outside the valid range.</exception>
        public UInt16Le(int num)
        {
            if (num > ushort.MaxValue || num < ushort.MinValue)
                throw new ArgumentOutOfRangeException(nameof(num));
            lo = (byte)(num & 0xff);
            hi = (byte)((num & 0xffff) >> 8);
        }

        /// <summary>Initializes a new <see cref="UInt16Le"/> from a <see cref="uint"/> value.</summary>
        /// <param name="num">The value to store. Must be within <see cref="ushort"/> range.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="num"/> exceeds <see cref="ushort.MaxValue"/>.</exception>
        public UInt16Le(uint num)
        {
            if (num > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(num));
            lo = (byte)(num & 0xff);
            hi = (byte)((num & 0xffff) >> 8);
        }

        /// <summary>Initializes a new <see cref="UInt16Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">The source byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public UInt16Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
                throw new ArgumentException("offset too large");
            lo = bytes[offset + 0];
            hi = bytes[offset + 1];
        }

        /// <summary>Initializes a new <see cref="UInt16Le"/> from a read-only byte span.</summary>
        /// <param name="bytes">A span containing at least 2 bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 2 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            lo = bytes[0];
            hi = bytes[1];
        }

        /// <summary>
        /// Initializes a new <see cref="UInt16Le"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 2 bytes).</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Le(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            if (bytes.Length < 2)
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            if (isBigEndian)
            {
                lo = bytes[1];
                hi = bytes[0];
            }
            else
            {
                lo = bytes[0];
                hi = bytes[1];
            }
        }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = false)
        {
            if (isBigEndian)
            {
                bytes[offset + 0] = hi;
                bytes[offset + 1] = lo;
            }
            else
            {
                bytes[offset + 0] = lo;
                bytes[offset + 1] = hi;
            }
        }

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">A span with at least 2 bytes of space.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 2)
                throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            if (isBigEndian)
            {
                destination[0] = hi;
                destination[1] = lo;
            }
            else
            {
                destination[0] = lo;
                destination[1] = hi;
            }
        }

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 2)
                return false;
            if (isBigEndian)
            {
                destination[0] = hi;
                destination[1] = lo;
            }
            else
            {
                destination[0] = lo;
                destination[1] = hi;
            }
            return true;
        }

        /// <summary>Writes a <see cref="ushort"/> value to a byte array in little-endian order.</summary>
        /// <param name="num">The value to write.</param>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        public static void ToBytes(ushort num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)(num >> 8);
        }

        /// <summary>Writes a <see cref="ushort"/> value to a span in little-endian order.</summary>
        /// <param name="num">The value to write.</param>
        /// <param name="destination">The destination span.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(ushort num, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, num);
        }

        /// <summary>Reads a <see cref="UInt16Le"/> from a read-only byte span in the specified byte order.</summary>
        /// <param name="source">A span containing at least 2 bytes.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Le ReadFrom(ReadOnlySpan<byte> source, bool isBigEndian = false) => new(source, isBigEndian);

        /// <summary>Parses a string into a <see cref="UInt16Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="style">The number style to use.</param>
        /// <returns>The parsed value.</returns>
        public static UInt16Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
                s = s.ToLower().Replace("0x", "");
            return new UInt16Le(ushort.Parse(s, style));
        }

        /// <summary>Parses a string into a <see cref="UInt16Le"/> using the specified format provider.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt16Le Parse(string s, IFormatProvider? provider) => new(ushort.Parse(s, provider));

        /// <summary>Parses a character span into a <see cref="UInt16Le"/> using the specified format provider.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt16Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(ushort.Parse(s, provider));

        /// <summary>Tries to parse a string into a <see cref="UInt16Le"/>.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt16Le result)
        {
            if (ushort.TryParse(s, provider, out ushort value)) { result = new(value); return true; }
            result = default; return false;
        }

        /// <summary>Tries to parse a character span into a <see cref="UInt16Le"/>.</summary>
        /// <param name="s">The character span to parse.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">When successful, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt16Le result)
        {
            if (ushort.TryParse(s, provider, out ushort value)) { result = new(value); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override string ToString() => $"0x{(ushort)this:x4}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) =>
            ((ushort)this).ToString(format, formatProvider);

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((ushort)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null) throw new ArgumentException("obj is null");
            return CompareTo((UInt16Le)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(UInt16Le other) => ((ushort)this).CompareTo((ushort)other);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj != null && Equals((UInt16Le)obj);

        /// <inheritdoc/>
        public readonly bool Equals(UInt16Le other) => this == other;

        /// <inheritdoc/>
        public override int GetHashCode() => ((ushort)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator +(UInt16Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator -(UInt16Le a) => new((ushort)-(ushort)a);
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator +(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a + (ushort)b));
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator -(UInt16Le a, UInt16Le b) => a + (-b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(UInt16Le a, UInt16Le b) => (ushort)a > (ushort)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(UInt16Le a, UInt16Le b) => (ushort)a < (ushort)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(UInt16Le a, UInt16Le b) => (ushort)a >= (ushort)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(UInt16Le a, UInt16Le b) => (ushort)a <= (ushort)b;
        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(UInt16Le a, UInt16Le b) => (ushort)a == (ushort)b;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(UInt16Le a, UInt16Le b) => (ushort)a != (ushort)b;
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator *(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a * (ushort)b));
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator /(UInt16Le a, UInt16Le b)
        {
            if (b.hi == 0 && b.lo == 0) throw new DivideByZeroException();
            return new((ushort)((ushort)a / (ushort)b));
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator &(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a & (ushort)b));
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator |(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a | (ushort)b));
        /// <summary>Shifts the value right by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator >>(UInt16Le a, uint b) => new((ushort)((ushort)a >> (byte)b));
        /// <summary>Performs an unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Le operator >>>(UInt16Le a, int b) => new((ushort)((ushort)a >> b));
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator <<(UInt16Le a, uint b) => new((ushort)((ushort)a << (byte)b));
        /// <summary>Computes the modulo of a value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator %(UInt16Le a, uint b) => new((ushort)((ushort)a % b));
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Le operator ^(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a ^ (ushort)b));
        /// <summary>Computes the bitwise XOR of a value and an unsigned mask.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator ^(UInt16Le a, uint b) => new((ushort)((ushort)a ^ b));
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator ~(UInt16Le a) => new((ushort)~(ushort)a);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator ++(UInt16Le a) => new((ushort)((ushort)a + 1));
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt16Le operator --(UInt16Le a) => new((ushort)((ushort)a - 1));

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt16Le"/> to a <see cref="ushort"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if BIG_ENDIAN
        public static implicit operator ushort(UInt16Le a) => BinaryPrimitives.ReverseEndianness(a._value);
#else
        public static implicit operator ushort(UInt16Le a) => a._value;
#endif

        /// <summary>Implicitly converts a <see cref="UInt16Le"/> to a <see cref="uint"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UInt16Le a) => (ushort)a;

        /// <summary>Implicitly converts a <see cref="ushort"/> to a <see cref="UInt16Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt16Le(ushort a) => new(a);

        /// <summary>Explicitly converts a <see cref="UInt16Le"/> to a <see cref="byte"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator byte(UInt16Le a) => a.lo;

        /// <summary>Explicitly converts a <see cref="uint"/> to a <see cref="UInt16Le"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="a"/> exceeds <see cref="ushort.MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt16Le(uint a)
        {
            if (a > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(a));
            return new((ushort)a);
        }

        /// <summary>Explicitly converts an <see cref="int"/> to a <see cref="UInt16Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt16Le(int a) => (UInt16Le)(uint)a;

        /// <summary>Widening conversion from a 16-bit little-endian value to a 32-bit little-endian value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Le(UInt16Le a) => new((ushort)a);

        #endregion
    }
}
