using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 256-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt256LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct UInt256Le : IComparable, IComparable<UInt256Le>, IEquatable<UInt256Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt256Le>, ISpanParsable<UInt256Le>
    {
        [FieldOffset(0)] internal UInt128Le lo;
        [FieldOffset(16)] internal UInt128Le hi;

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a <see cref="UInt256"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(UInt256 num)
        {
#if BIG_ENDIAN
            // On BE: each limb must be byte-swapped to land in LE order.
            Unsafe.SkipInit(out this);
            Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            BinaryPrimitives.WriteUInt64LittleEndian(raw,       num._p0);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[8..],  num._p1);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[16..], num._p2);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[24..], num._p3);
#else
            // On LE: UInt256Le and UInt256 share identical memory layout — direct copy.
            Unsafe.SkipInit(out this);
            Unsafe.As<UInt256Le, UInt256>(ref this) = num;
#endif
        }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a <see cref="UInt128"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(UInt128 num) { lo = (UInt128Le)num; hi = (UInt128Le)UInt128.Zero; }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a <see cref="ulong"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(ulong num) { lo = (UInt128Le)(UInt128)num; hi = (UInt128Le)UInt128.Zero; }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a read-only byte span.</summary>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 32 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            bytes[..32].CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>
        /// Initializes a new <see cref="UInt256Le"/> from a read-only byte span whose byte order is specified.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 32; i++) dst[i] = bytes[31 - i];
            }
            else
            {
                bytes[..32].CopyTo(dst);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="UInt256Le"/> from a read-only byte span at the given offset.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">If span is too short from the given offset.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Le(ReadOnlySpan<byte> bytes, int offset, bool isBigEndian = false)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 32; i++) dst[i] = bytes[offset + 31 - i];
            }
            else
            {
                bytes.Slice(offset, 32).CopyTo(dst);
            }
        }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a byte array whose byte order is specified.</summary>
        /// <param name="bytes">Source byte array (must have at least 32 bytes).</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">The array is too short.</exception>
        public UInt256Le(byte[] bytes, bool isBigEndian = false) : this(new ReadOnlySpan<byte>(bytes), isBigEndian) { }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a byte array at the given offset.</summary>
        /// <param name="bytes">Source byte array (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">The zero-based offset into <paramref name="bytes"/>.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian and reversed during storage.</param>
        /// <exception cref="ArgumentException">The array is too short for the given offset.</exception>
        public UInt256Le(byte[] bytes, int offset, bool isBigEndian = false) : this(new ReadOnlySpan<byte>(bytes), offset, isBigEndian) { }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="bytes">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        [Obsolete("Use WriteTo(byte[], int, bool) instead.")]
        public readonly void ToBytes(byte[] bytes, int offset = 0, bool isBigEndian = false)
            => WriteTo(new Span<byte>(bytes), offset, isBigEndian);

        /// <summary>Writes the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the destination span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination, int offset = 0, bool isBigEndian = false)
        {
            if (destination.Length - offset < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 32; i++) destination[offset + i] = src[31 - i];
            }
            else
            {
                src.CopyTo(destination.Slice(offset));
            }
        }

        /// <summary>Writes the value to a byte array in the specified byte order.</summary>
        /// <param name="destination">Destination byte array (must have at least 32 bytes starting from <paramref name="offset"/>).</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <exception cref="ArgumentException">The array is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(byte[] destination, int offset = 0, bool isBigEndian = false)
            => WriteTo(new Span<byte>(destination), offset, isBigEndian);

        /// <summary>Attempts to write the value to a destination span in the specified byte order.</summary>
        /// <param name="destination">Destination span.</param>
        /// <param name="offset">Starting offset in the destination span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination, int offset = 0, bool isBigEndian = false)
        {
            if (destination.Length - offset < 32) return false;
            ReadOnlySpan<byte> src = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
            if (isBigEndian)
            {
                for (int i = 0; i < 32; i++) destination[offset + i] = src[31 - i];
            }
            else
            {
                src.CopyTo(destination.Slice(offset));
            }
            return true;
        }

        /// <summary>Attempts to write the value to a byte array in the specified byte order.</summary>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) bytes are written little-endian; if <see langword="true"/> they are written big-endian.</param>
        /// <returns>True if successful, false if array is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(byte[] destination, int offset = 0, bool isBigEndian = false)
            => TryWriteTo(new Span<byte>(destination), offset, isBigEndian);

        /// <summary>Reads a <see cref="UInt256Le"/> from a read-only byte span.</summary>
        /// <param name="source">Source span.</param>
        /// <param name="offset">Starting offset in the source span.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le ReadFrom(ReadOnlySpan<byte> source, int offset = 0, bool isBigEndian = false) => new(source, offset, isBigEndian);

        /// <summary>Reads a <see cref="UInt256Le"/> from a byte array.</summary>
        /// <param name="source">Source byte array.</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <param name="isBigEndian">If <see langword="false"/> (default) the source is interpreted as little-endian; if <see langword="true"/> it is interpreted as big-endian.</param>
        /// <returns>The parsed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le ReadFrom(byte[] source, int offset = 0, bool isBigEndian = false) => new(new ReadOnlySpan<byte>(source), offset, isBigEndian);

        /// <summary>Parses a string into a <see cref="UInt256Le"/>.</summary>
        public static UInt256Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            return new(UInt256.Parse(s, style));
        }

        /// <inheritdoc/>
        public static UInt256Le Parse(string s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));
        /// <inheritdoc/>
        public static UInt256Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));

        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt256Le result)
        {
            if (UInt256.TryParse(s, provider, out UInt256 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt256Le result)
        {
            if (UInt256.TryParse(s, provider, out UInt256 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override string ToString() => $"0x{(UInt256)this:x64}";
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((UInt256)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((UInt256)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt256Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt256Le other) => ((UInt256)this).CompareTo((UInt256)other);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is UInt256Le other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256Le other) => this == other;
        /// <inheritdoc/>
        public override int GetHashCode() => ((UInt256)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator +(UInt256Le a) => a;
        /// <summary>Computes the two's complement negation (modulo 2^256).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator -(UInt256Le a) => new(-(UInt256)a);
        /// <summary>Adds two values (wraps on overflow).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator +(UInt256Le a, UInt256Le b) => new((UInt256)a + (UInt256)b);
        /// <summary>Subtracts two values (wraps on underflow).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator -(UInt256Le a, UInt256Le b) => new((UInt256)a - (UInt256)b);
        /// <summary>Multiplies two values (low 256 bits of the full product).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator *(UInt256Le a, UInt256Le b) => new((UInt256)a * (UInt256)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator /(UInt256Le a, UInt256Le b) => new((UInt256)a / (UInt256)b);
        /// <summary>Computes the remainder of division.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator %(UInt256Le a, UInt256Le b) => new((UInt256)a % (UInt256)b);
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator &(UInt256Le a, UInt256Le b)
        {
            Unsafe.As<UInt256Le, ulong>(ref a) &= Unsafe.As<UInt256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 2) &= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 3) &= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator |(UInt256Le a, UInt256Le b)
        {
            Unsafe.As<UInt256Le, ulong>(ref a) |= Unsafe.As<UInt256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 2) |= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 3) |= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ^(UInt256Le a, UInt256Le b)
        {
            Unsafe.As<UInt256Le, ulong>(ref a) ^= Unsafe.As<UInt256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 2) ^= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 3) ^= Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ~(UInt256Le a)
        {
            Unsafe.As<UInt256Le, ulong>(ref a) = ~Unsafe.As<UInt256Le, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 2) = ~Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 3) = ~Unsafe.Add(ref Unsafe.As<UInt256Le, ulong>(ref a), 3);
            return a;
        }
        /// <summary>Shifts the value left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator <<(UInt256Le a, int b) => new((UInt256)a << b);
        /// <summary>Shifts the value right by the specified amount (logical for unsigned).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator >>(UInt256Le a, int b) => new((UInt256)a >> b);
        /// <summary>Unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator >>>(UInt256Le a, int b) => new((UInt256)a >>> b);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ++(UInt256Le a) => new((UInt256)a + UInt256.One);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator --(UInt256Le a) => new((UInt256)a - UInt256.One);

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256Le a, UInt256Le b)
        {
            ref ulong ra = ref Unsafe.As<UInt256Le, ulong>(ref a);
            ref ulong rb = ref Unsafe.As<UInt256Le, ulong>(ref b);
            return ra == rb
                && Unsafe.Add(ref ra, 1) == Unsafe.Add(ref rb, 1)
                && Unsafe.Add(ref ra, 2) == Unsafe.Add(ref rb, 2)
                && Unsafe.Add(ref ra, 3) == Unsafe.Add(ref rb, 3);
        }
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256Le a, UInt256Le b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256Le a, UInt256Le b) => (UInt256)a < (UInt256)b;
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256Le a, UInt256Le b) => (UInt256)a > (UInt256)b;
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UInt256Le a, UInt256Le b) => (UInt256)a <= (UInt256)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UInt256Le a, UInt256Le b) => (UInt256)a >= (UInt256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt256Le"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(UInt256Le a)
        {
#if BIG_ENDIAN
            // On BE: each limb must be byte-swapped back to native order.
            ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            return new UInt256(
                BinaryPrimitives.ReadUInt64LittleEndian(raw[24..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw[16..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw[8..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw));
#else
            // On LE: UInt256Le and UInt256 share identical memory layout — zero-cost reinterpret.
            return Unsafe.As<UInt256Le, UInt256>(ref a);
#endif
        }
        /// <summary>Implicitly converts a <see cref="UInt256"/> to a <see cref="UInt256Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Le(UInt256 a) => new(a);

        /// <summary>Widening conversion from a 128-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Le(UInt128Le a) => new((UInt128)a);
        /// <summary>Widening conversion from a 64-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Le(UInt64Le a) => new((ulong)a);
        /// <summary>Widening conversion from a 32-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Le(UInt32Le a) => new((ulong)(uint)a);

        /// <summary>Narrowing conversion to a 128-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128Le(UInt256Le a) => a.lo;
        /// <summary>Narrowing conversion to a 64-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt64Le(UInt256Le a) => (UInt64Le)(ulong)(UInt128)a.lo;
        /// <summary>Narrowing conversion to a 32-bit little-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt32Le(UInt256Le a) => (UInt32Le)(uint)(ulong)(UInt128)a.lo;

        /// <summary>Explicitly converts to a <see cref="UInt128"/> (keeps low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(UInt256Le a) => (UInt128)a.lo;
        /// <summary>Explicitly converts to an <see cref="Int256"/> (reinterpret).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt256Le a) => (Int256)(UInt256)a;

        #endregion
    }
}
