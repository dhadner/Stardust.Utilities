using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 256-bit signed integer type (two's complement).
    /// Stores bytes in little-endian order (least significant byte first, x86 native order).
    /// Use this type at I/O boundaries; convert to <see cref="Int256"/> for arithmetic.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int256LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct Int256Le : IComparable, IComparable<Int256Le>, IEquatable<Int256Le>,
                             IFormattable, ISpanFormattable, IParsable<Int256Le>, ISpanParsable<Int256Le>
    {
        // Little-endian: least-significant bits at the lowest address.
        [FieldOffset(0)]  internal UInt128Le lo; // bits 0-127
        [FieldOffset(16)] internal UInt128Le hi; // bits 128-255 (sign bit is MSB of hi)

        /// <summary>Initializes a new <see cref="Int256Le"/> from an <see cref="Int256"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256Le(Int256 num)
        {
#if BIG_ENDIAN
            // On BE: each limb must be byte-swapped to land in LE order.
            UInt256 u = (UInt256)num;
            Unsafe.SkipInit(out this);
            Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            BinaryPrimitives.WriteUInt64LittleEndian(raw,       u._p0);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[8..],  u._p1);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[16..], u._p2);
            BinaryPrimitives.WriteUInt64LittleEndian(raw[24..], u._p3);
#else
            // On LE: Int256Le and Int256 share identical memory layout — direct copy.
            Unsafe.SkipInit(out this);
            Unsafe.As<Int256Le, Int256>(ref this) = num;
#endif
        }

        /// <summary>Initializes a new <see cref="Int256Le"/> from a read-only byte span (little-endian, least-significant byte first).</summary>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> has fewer than 32 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            bytes[..32].CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>Initializes a new <see cref="Int256Le"/> from a byte array at the given offset.</summary>
        /// <exception cref="ArgumentException"><paramref name="bytes"/> is too short.</exception>
        public Int256Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Array is too short");
            Unsafe.SkipInit(out this);
            new ReadOnlySpan<byte>(bytes, offset, 32).CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>Writes the value to a byte array at the given offset (little-endian).</summary>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 16);
        }

        /// <summary>Writes the value to a destination span (little-endian: least-significant byte first).</summary>
        /// <exception cref="ArgumentException"><paramref name="destination"/> has fewer than 32 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1)).CopyTo(destination);
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> if the span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) return false;
            MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1)).CopyTo(destination);
            return true;
        }

        /// <summary>Reads an <see cref="Int256Le"/> from a read-only byte span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into an <see cref="Int256Le"/>.</summary>
        public static Int256Le Parse(string s, NumberStyles style = NumberStyles.Integer) => new(Int256.Parse(s, style));
        /// <inheritdoc/>
        public static Int256Le Parse(string s, IFormatProvider? provider) => new(Int256.Parse(s, provider));
        /// <inheritdoc/>
        public static Int256Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(Int256.Parse(s, provider));
        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int256Le result)
        {
            if (Int256.TryParse(s, provider, out Int256 v)) { result = new(v); return true; }
            result = default; return false;
        }
        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int256Le result)
        {
            if (Int256.TryParse(s, provider, out Int256 v)) { result = new(v); return true; }
            result = default; return false;
        }

        /// <inheritdoc/>
        public override string ToString() => ((Int256)this).ToString();
        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((Int256)this).ToString(format, formatProvider);
        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((Int256)this).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int256Le)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(Int256Le other) => ((Int256)this).CompareTo((Int256)other);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Int256Le other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(Int256Le other) => this == other;
        /// <inheritdoc/>
        public override int GetHashCode() => ((Int256)this).GetHashCode();

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator +(Int256Le a) => a;
        /// <summary>Negates the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator -(Int256Le a) => new(-(Int256)a);
        /// <summary>Adds two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator +(Int256Le a, Int256Le b) => new((Int256)a + (Int256)b);
        /// <summary>Subtracts two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator -(Int256Le a, Int256Le b) => new((Int256)a - (Int256)b);
        /// <summary>Multiplies two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator *(Int256Le a, Int256Le b) => new((Int256)a * (Int256)b);
        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator /(Int256Le a, Int256Le b) => new((Int256)a / (Int256)b);
        /// <summary>Computes the remainder of dividing two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator %(Int256Le a, Int256Le b) => new((Int256)a % (Int256)b);
        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator &(Int256Le a, Int256Le b)
        {
            Unsafe.As<Int256Le, ulong>(ref a) &= Unsafe.As<Int256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 2) &= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 3) &= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator |(Int256Le a, Int256Le b)
        {
            Unsafe.As<Int256Le, ulong>(ref a) |= Unsafe.As<Int256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 2) |= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 3) |= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator ^(Int256Le a, Int256Le b)
        {
            Unsafe.As<Int256Le, ulong>(ref a) ^= Unsafe.As<Int256Le, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 2) ^= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 3) ^= Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref b), 3);
            return a;
        }
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator ~(Int256Le a)
        {
            Unsafe.As<Int256Le, ulong>(ref a) = ~Unsafe.As<Int256Le, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 1);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 2) = ~Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 2);
            Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 3) = ~Unsafe.Add(ref Unsafe.As<Int256Le, ulong>(ref a), 3);
            return a;
        }
        /// <summary>Arithmetic (sign-extending) right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator >>(Int256Le a, int b) => new((Int256)a >> b);
        /// <summary>Unsigned (logical, zero-filling) right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator >>>(Int256Le a, int b) => new((Int256)a >>> b);
        /// <summary>Shifts the value left.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator <<(Int256Le a, int b) => new((Int256)a << b);
        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator ++(Int256Le a) => new((Int256)a + Int256.One);
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256Le operator --(Int256Le a) => new((Int256)a - Int256.One);

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Int256Le a, Int256Le b)
        {
            ref ulong ra = ref Unsafe.As<Int256Le, ulong>(ref a);
            ref ulong rb = ref Unsafe.As<Int256Le, ulong>(ref b);
            return ra == rb
                && Unsafe.Add(ref ra, 1) == Unsafe.Add(ref rb, 1)
                && Unsafe.Add(ref ra, 2) == Unsafe.Add(ref rb, 2)
                && Unsafe.Add(ref ra, 3) == Unsafe.Add(ref rb, 3);
        }
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Int256Le a, Int256Le b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Int256Le a, Int256Le b) => (Int256)a < (Int256)b;
        /// <summary>Determines whether the left operand is greater than the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Int256Le a, Int256Le b) => (Int256)a > (Int256)b;
        /// <summary>Determines whether the left operand is less than or equal to the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Int256Le a, Int256Le b) => (Int256)a <= (Int256)b;
        /// <summary>Determines whether the left operand is greater than or equal to the right (signed comparison).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Int256Le a, Int256Le b) => (Int256)a >= (Int256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts an <see cref="Int256Le"/> to an <see cref="Int256"/> (host-native, signed).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(Int256Le a)
        {
#if BIG_ENDIAN
            // On BE: each limb must be byte-swapped back to native order.
            ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            return (Int256)new UInt256(
                BinaryPrimitives.ReadUInt64LittleEndian(raw[24..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw[16..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw[8..]),
                BinaryPrimitives.ReadUInt64LittleEndian(raw));
#else
            // On LE: Int256Le and Int256 share identical memory layout — zero-cost reinterpret.
            return Unsafe.As<Int256Le, Int256>(ref a);
#endif
        }
        /// <summary>Implicitly converts an <see cref="Int256"/> to an <see cref="Int256Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Le(Int256 a) => new(a);

        /// <summary>Widening sign-extending conversion from a 128-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Le(Int128Le a) => new((Int256)(Int128)a);
        /// <summary>Widening sign-extending conversion from a 64-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Le(Int64Le a) => new((Int256)(long)a);
        /// <summary>Widening sign-extending conversion from a 32-bit little-endian signed value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256Le(Int32Le a) => new((Int256)(int)a);

        /// <summary>Narrowing conversion to a 128-bit little-endian signed value (truncates to low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128Le(Int256Le a) => (Int128Le)(Int128)(Int256)a;
        /// <summary>Narrowing conversion to a 64-bit little-endian signed value (truncates to low 64 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int64Le(Int256Le a) => (Int64Le)(long)(Int256)a;
        /// <summary>Narrowing conversion to a 32-bit little-endian signed value (truncates to low 32 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int32Le(Int256Le a) => (Int32Le)(int)(Int256)a;

        /// <summary>Explicitly reinterprets the bit pattern as an unsigned <see cref="UInt256Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256Le(Int256Le a) => new((UInt256)(Int256)a);
        /// <summary>Explicitly reinterprets the bit pattern of a <see cref="UInt256Le"/> as a signed <see cref="Int256Le"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256Le(UInt256Le a) => new((Int256)(UInt256)a);

        /// <summary>Explicitly converts to a host-native <see cref="UInt256"/> (bit-reinterpret, no sign extension).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(Int256Le a) => new UInt256((UInt128)a.hi, (UInt128)a.lo);

        #endregion
    }
}
