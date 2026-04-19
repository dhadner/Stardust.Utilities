using System;
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
            hi = (UInt128Be)num.Upper;
            lo = (UInt128Be)num.Lower;
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
            hi = new UInt128Be(bytes);
            lo = new UInt128Be(bytes.Slice(16));
        }

        /// <summary>Initializes a new <see cref="UInt256Be"/> from a byte array at the given offset.</summary>
        public UInt256Be(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Array is too short");
            hi = new UInt128Be(bytes, offset);
            lo = new UInt128Be(bytes, offset + 16);
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
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(16));
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) return false;
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(16));
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
        public override readonly string ToString() => $"0x{(UInt256)this:x64}";
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
        public override readonly bool Equals(object? obj) => obj is UInt256Be other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256Be other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((UInt256)this).GetHashCode();

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
        public static UInt256Be operator &(UInt256Be a, UInt256Be b) => new((UInt256)a & (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator |(UInt256Be a, UInt256Be b) => new((UInt256)a | (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ^(UInt256Be a, UInt256Be b) => new((UInt256)a ^ (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Be operator ~(UInt256Be a) => new(~(UInt256)a);
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
        public static bool operator ==(UInt256Be a, UInt256Be b) => (UInt256)a == (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256Be a, UInt256Be b) => (UInt256)a != (UInt256)b;
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
        public static implicit operator UInt256(UInt256Be a) => new((UInt128)a.hi, (UInt128)a.lo);
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
