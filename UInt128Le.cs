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
#if BIG_ENDIAN
            // On BE: UInt64Le ctor swaps each half so bytes land in LE order.
            lo = (UInt64Le)(ulong)num;
            hi = (UInt64Le)(ulong)(num >> 64);
#else
            // On LE: UInt128Le and UInt128 share identical memory layout — direct copy.
            Unsafe.SkipInit(out this);
            Unsafe.As<UInt128Le, UInt128>(ref this) = num;
#endif
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

        /// <summary>
        /// Initializes a new <see cref="UInt128Le"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 16 bytes).</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt128Le(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            if (bytes.Length < 16) throw new ArgumentException("Span must have at least 16 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 16; i++) dst[i] = bytes[15 - i];
            }
            else
            {
                bytes[..16].CopyTo(dst);
            }
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

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">The destination byte array.</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = false)
        {
            if (isBigEndian)
            {
                ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
                for (int i = 0; i < 16; i++) bytes[offset + i] = src[15 - i];
            }
            else
            {
                lo.ToBytes(bytes, offset);
                hi.ToBytes(bytes, offset + 8);
            }
        }

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">A span with at least 16 bytes of space.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 16) throw new ArgumentException("Destination span must have at least 16 bytes", nameof(destination));
            if (isBigEndian)
            {
                ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
                for (int i = 0; i < 16; i++) destination[i] = src[15 - i];
            }
            else
            {
                lo.WriteTo(destination);
                hi.WriteTo(destination.Slice(8));
            }
        }

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">The destination span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <returns><see langword="true"/> if the span was large enough; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, bool isBigEndian = false)
        {
            if (destination.Length < 16)
                return false;
            if (isBigEndian)
            {
                ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
                for (int i = 0; i < 16; i++) destination[i] = src[15 - i];
            }
            else
            {
                lo.WriteTo(destination);
                hi.WriteTo(destination.Slice(8));
            }
            return true;
        }

        /// <summary>Reads a <see cref="UInt128Le"/> from a read-only byte span in the specified byte order.</summary>
        /// <param name="source">A span containing at least 16 bytes.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian.</param>
        /// <returns>The value read from the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128Le ReadFrom(ReadOnlySpan<byte> source, bool isBigEndian = false) => new(source, isBigEndian);

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
        public override string ToString() => $"0x{(UInt128)this:x32}";
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
        public override bool Equals(object? obj) => obj != null && Equals((UInt128Le)obj);
        /// <inheritdoc/>
        public readonly bool Equals(UInt128Le other) => this == other;
        /// <inheritdoc/>
        public override int GetHashCode() => ((UInt128)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value of the operand (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator +(UInt128Le a) => a;
        /// <summary>Negates the value (unary minus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator -(UInt128Le a) => new((UInt128)(-(Int128)(UInt128)a));
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator +(UInt128Le a, UInt128Le b) => new((UInt128)a + (UInt128)b);
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator -(UInt128Le a, UInt128Le b) => new((UInt128)a - (UInt128)b);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >(UInt128Le a, UInt128Le b) => (UInt128)a > (UInt128)b;
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <(UInt128Le a, UInt128Le b) => (UInt128)a < (UInt128)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator >=(UInt128Le a, UInt128Le b) => (UInt128)a >= (UInt128)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator <=(UInt128Le a, UInt128Le b) => (UInt128)a <= (UInt128)b;
        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator ==(UInt128Le a, UInt128Le b) => (UInt128)a == (UInt128)b;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static bool operator !=(UInt128Le a, UInt128Le b) => (UInt128)a != (UInt128)b;
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator *(UInt128Le a, UInt128Le b) => new((UInt128)a * (UInt128)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator /(UInt128Le a, UInt128Le b)
        {
            if ((UInt128)b == 0) throw new DivideByZeroException();
            return new((UInt128)a / (UInt128)b);
        }
        /// <summary>Computes the remainder of two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator %(UInt128Le a, UInt128Le b)
        {
            if ((UInt128)b == 0) throw new DivideByZeroException();
            return new((UInt128)a % (UInt128)b);
        }
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator &(UInt128Le a, UInt128Le b)
        {
            Unsafe.As<UInt128Le, ulong>(ref a) &= Unsafe.As<UInt128Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref b), 1);
            return a;
        }
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator |(UInt128Le a, UInt128Le b)
        {
            Unsafe.As<UInt128Le, ulong>(ref a) |= Unsafe.As<UInt128Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref b), 1);
            return a;
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator ^(UInt128Le a, UInt128Le b)
        {
            Unsafe.As<UInt128Le, ulong>(ref a) ^= Unsafe.As<UInt128Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref b), 1);
            return a;
        }
        /// <summary>Shifts the value right by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator >>(UInt128Le a, int b) => new((UInt128)a >> b);
        /// <summary>Performs an unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128Le operator >>>(UInt128Le a, int b) => new((UInt128)a >> b);
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator <<(UInt128Le a, int b) => new((UInt128)a << b);
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator ~(UInt128Le a)
        {
            Unsafe.As<UInt128Le, ulong>(ref a) = ~Unsafe.As<UInt128Le, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<UInt128Le, ulong>(ref a), 1);
            return a;
        }
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator ++(UInt128Le a) => new((UInt128)a + 1);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static UInt128Le operator --(UInt128Le a) => new((UInt128)a - 1);

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt128Le"/> to a <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if BIG_ENDIAN
        public static implicit operator UInt128(UInt128Le a) => ((UInt128)(ulong)a.hi << 64) | (ulong)a.lo;
#else
        // On LE: UInt128Le and UInt128 share identical memory layout — zero-cost reinterpret.
        public static implicit operator UInt128(UInt128Le a) => Unsafe.As<UInt128Le, UInt128>(ref a);
#endif

        /// <summary>Implicitly converts a <see cref="UInt128"/> to a <see cref="UInt128Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128Le(UInt128 a) => new(a);

        /// <summary>Explicitly converts a <see cref="UInt128Le"/> to a <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator ulong(UInt128Le a) => (ulong)(UInt128)a;
        /// <summary>Explicitly converts a <see cref="UInt128Le"/> to an <see cref="Int128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator Int128(UInt128Le a) => (Int128)(UInt128)a;

        /// <summary>Widening conversion from a 64-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator UInt128Le(UInt64Le a) => new((ulong)a);

        /// <summary>Widening conversion from a 32-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator UInt128Le(UInt32Le a) => new((ulong)(uint)a);

        /// <summary>Narrowing conversion to a 64-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt64Le(UInt128Le a) => new((ulong)(UInt128)a);

        /// <summary>Narrowing conversion to a 32-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static explicit operator UInt32Le(UInt128Le a) => new((uint)(UInt128)a);

        #endregion
    }
}
