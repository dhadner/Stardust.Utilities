using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 256-bit unsigned integer type.
    /// Stores bytes in big-endian order (most significant byte first, network order).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt256BeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct UInt256Be : IComparable, IComparable<UInt256Be>, IEquatable<UInt256Be>,
                              IFormattable, ISpanFormattable, IParsable<UInt256Be>, ISpanParsable<UInt256Be>
    {
        // Big-endian: hi half stored at lower addresses so first bytes on the wire are most-significant.
        [FieldOffset(0)] internal UInt128Be hi;
        [FieldOffset(16)] internal UInt128Be lo;

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="UInt256"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(UInt256 num)
        {
            Unsafe.SkipInit(out this);
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; write limbs directly in network (MSB-first) order.
            ref ulong dst = ref Unsafe.As<UInt256Be, ulong>(ref this);
            dst                        = num._p3;
            Unsafe.Add(ref dst, 1) = num._p2;
            Unsafe.Add(ref dst, 2) = num._p1;
            Unsafe.Add(ref dst, 3) = num._p0;
#else
            // On LE: each limb must be byte-swapped to produce network byte order.
            Span<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            BinaryPrimitives.WriteUInt64BigEndian(raw,       num._p3);
            BinaryPrimitives.WriteUInt64BigEndian(raw[8..],  num._p2);
            BinaryPrimitives.WriteUInt64BigEndian(raw[16..], num._p1);
            BinaryPrimitives.WriteUInt64BigEndian(raw[24..], num._p0);
#endif
        }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="UInt128"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(UInt128 num) { hi = (UInt128Be)UInt128.Zero; lo = (UInt128Be)num; }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a <see cref="ulong"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ulong num) { hi = (UInt128Be)UInt128.Zero; lo = (UInt128Be)(UInt128)num; }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a read-only byte span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must have at least 32 bytes", nameof(bytes));
            Unsafe.SkipInit(out this);
            bytes[..32].CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a byte array at the given offset.</summary>
        public UInt256Be(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Array is too short");
            Unsafe.SkipInit(out this);
            new ReadOnlySpan<byte>(bytes, offset, 32).CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            hi.ToBytes(bytes, offset);
            lo.ToBytes(bytes, offset + 16);
        }

        /// <summary>Writes the value to a destination span (big-endian: most-significant byte first).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1)).CopyTo(destination);
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) return false;
            MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1)).CopyTo(destination);
            return true;
        }

        /// <summary>Reads a <see cref="UInt256Be"/> from a read-only byte span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be ReadFrom(ReadOnlySpan<byte> source) => new(source);

        /// <summary>Parses a string into a <see cref="UInt256Be"/>.</summary>
        public static UInt256Be Parse(string s, NumberStyles style = NumberStyles.Integer) => new(UInt256.Parse(s, style));
        /// <inheritdoc/>
        public static UInt256Be Parse(string s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));
        /// <inheritdoc/>
        public static UInt256Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(UInt256.Parse(s, provider));
        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt256Be result)
        {
            if (UInt256.TryParse(s, provider, out UInt256 v)) { result = new(v); return true; }
            result = default; return false;
        }
        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt256Be result)
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
        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt256Be)obj); }
        /// <inheritdoc/>
        public readonly int CompareTo(UInt256Be other) => ((UInt256)this).CompareTo((UInt256)other);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is UInt256Be other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256Be other) => this == other;
        /// <inheritdoc/>
        public override int GetHashCode() => ((UInt256)this).GetHashCode();

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator +(UInt256Be a) => a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator -(UInt256Be a) => new(-(UInt256)a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator +(UInt256Be a, UInt256Be b) => new((UInt256)a + (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator -(UInt256Be a, UInt256Be b) => new((UInt256)a - (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator *(UInt256Be a, UInt256Be b) => new((UInt256)a * (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator /(UInt256Be a, UInt256Be b) => new((UInt256)a / (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator %(UInt256Be a, UInt256Be b) => new((UInt256)a % (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator &(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) &= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) &= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator |(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) |= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) |= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ^(UInt256Be a, UInt256Be b)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) ^= Unsafe.As<UInt256Be, ulong>(ref b);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) ^= Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref b), 3);
            return a;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ~(UInt256Be a)
        {
            Unsafe.As<UInt256Be, ulong>(ref a) = ~Unsafe.As<UInt256Be, ulong>(ref a);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 1);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 2);
            Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3) = ~Unsafe.Add(ref Unsafe.As<UInt256Be, ulong>(ref a), 3);
            return a;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator <<(UInt256Be a, int b) => new((UInt256)a << b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator >>(UInt256Be a, int b) => new((UInt256)a >> b);
        /// <summary>Unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator >>>(UInt256Be a, int b) => new((UInt256)a >>> b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ++(UInt256Be a) => new((UInt256)a + UInt256.One);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator --(UInt256Be a) => new((UInt256)a - UInt256.One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256Be a, UInt256Be b)
        {
            ref ulong ra = ref Unsafe.As<UInt256Be, ulong>(ref a);
            ref ulong rb = ref Unsafe.As<UInt256Be, ulong>(ref b);
            return ra == rb
                && Unsafe.Add(ref ra, 1) == Unsafe.Add(ref rb, 1)
                && Unsafe.Add(ref ra, 2) == Unsafe.Add(ref rb, 2)
                && Unsafe.Add(ref ra, 3) == Unsafe.Add(ref rb, 3);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256Be a, UInt256Be b) => !(a == b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256Be a, UInt256Be b) => (UInt256)a < (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256Be a, UInt256Be b) => (UInt256)a > (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UInt256Be a, UInt256Be b) => (UInt256)a <= (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UInt256Be a, UInt256Be b) => (UInt256)a >= (UInt256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt256Be"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(UInt256Be a)
        {
#if BIG_ENDIAN
            // On BE: no byte-swapping needed; limbs are already in native order at predictable offsets.
            ref ulong src = ref Unsafe.As<UInt256Be, ulong>(ref a);
            return new UInt256(src, Unsafe.Add(ref src, 1), Unsafe.Add(ref src, 2), Unsafe.Add(ref src, 3));
#else
            // On LE: byte-swap each limb back to native little-endian order.
            ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            return new UInt256(
                BinaryPrimitives.ReadUInt64BigEndian(raw),
                BinaryPrimitives.ReadUInt64BigEndian(raw[8..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[16..]),
                BinaryPrimitives.ReadUInt64BigEndian(raw[24..]));
#endif
        }
        /// <summary>Implicitly converts a <see cref="UInt256"/> to a <see cref="UInt256Be"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt256 a) => new(a);

        /// <summary>Widening conversion from a 128-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt128Be a) => new((UInt128)a);
        /// <summary>Widening conversion from a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt64Be a) => new((ulong)a);
        /// <summary>Widening conversion from a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256Be(UInt32Be a) => new((ulong)(uint)a);

        /// <summary>Narrowing conversion to a 128-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128Be(UInt256Be a) => a.lo;
        /// <summary>Narrowing conversion to a 64-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt64Be(UInt256Be a) => (UInt64Be)(ulong)(UInt128)a.lo;
        /// <summary>Narrowing conversion to a 32-bit big-endian unsigned value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt32Be(UInt256Be a) => (UInt32Be)(uint)(ulong)(UInt128)a.lo;

        /// <summary>Explicitly converts to a <see cref="UInt128"/> (keeps low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(UInt256Be a) => (UInt128)a.lo;
        /// <summary>Explicitly converts to an <see cref="Int256"/> (reinterpret).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt256Be a) => (Int256)(UInt256)a;

        #endregion
    }
}
