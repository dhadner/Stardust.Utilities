using System;
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
            lo = (UInt128Le)num.Lower;
            hi = (UInt128Le)num.Upper;
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
            lo = new UInt128Le(bytes);
            hi = new UInt128Le(bytes.Slice(16));
        }

        /// <summary>Initializes a new <see cref="UInt256Le"/> from a byte array at the given offset.</summary>
        public UInt256Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Array is too short");
            lo = new UInt128Le(bytes, offset);
            hi = new UInt128Le(bytes, offset + 16);
        }

        /// <summary>Writes the value to a byte array at the given offset.</summary>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 16);
        }

        /// <summary>Writes the value to a destination span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) throw new ArgumentException("Destination span must have at least 32 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(16));
        }

        /// <summary>Attempts to write the value to a destination span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 32) return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(16));
            return true;
        }

        /// <summary>Reads a <see cref="UInt256Le"/> from a read-only byte span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

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
        public override readonly string ToString() => $"0x{(UInt256)this:x64}";
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
        public override readonly bool Equals(object? obj) => obj is UInt256Le other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256Le other) => this == other;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => ((UInt256)this).GetHashCode();

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator +(UInt256Le a) => a;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator -(UInt256Le a) => new(-(UInt256)a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator +(UInt256Le a, UInt256Le b) => new((UInt256)a + (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator -(UInt256Le a, UInt256Le b) => new((UInt256)a - (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator *(UInt256Le a, UInt256Le b) => new((UInt256)a * (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator /(UInt256Le a, UInt256Le b) => new((UInt256)a / (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator %(UInt256Le a, UInt256Le b) => new((UInt256)a % (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator &(UInt256Le a, UInt256Le b) => new((UInt256)a & (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator |(UInt256Le a, UInt256Le b) => new((UInt256)a | (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ^(UInt256Le a, UInt256Le b) => new((UInt256)a ^ (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ~(UInt256Le a) => new(~(UInt256)a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator <<(UInt256Le a, int b) => new((UInt256)a << b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator >>(UInt256Le a, int b) => new((UInt256)a >> b);
        /// <summary>Unsigned (logical) right shift. For unsigned types this is identical to <c>&gt;&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator >>>(UInt256Le a, int b) => new((UInt256)a >>> b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator ++(UInt256Le a) => new((UInt256)a + UInt256.One);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256Le operator --(UInt256Le a) => new((UInt256)a - UInt256.One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256Le a, UInt256Le b) => (UInt256)a == (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256Le a, UInt256Le b) => (UInt256)a != (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256Le a, UInt256Le b) => (UInt256)a < (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256Le a, UInt256Le b) => (UInt256)a > (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UInt256Le a, UInt256Le b) => (UInt256)a <= (UInt256)b;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UInt256Le a, UInt256Le b) => (UInt256)a >= (UInt256)b;

        #endregion

        #region Conversions

        /// <summary>Implicitly converts a <see cref="UInt256Le"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(UInt256Le a) => new((UInt128)a.hi, (UInt128)a.lo);
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
