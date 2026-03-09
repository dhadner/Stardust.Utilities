using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 16-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int16LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Int16Le : IComparable, IComparable<Int16Le>, IEquatable<Int16Le>,
                             IFormattable, ISpanFormattable, IParsable<Int16Le>, ISpanParsable<Int16Le>
    {
        [FieldOffset(0)] internal byte lo;
        [FieldOffset(1)] internal byte hi;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Le(short num)
        {
            lo = (byte)(num & 0xff);
            hi = (byte)(num >> 8);
        }

        public Int16Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
                throw new ArgumentException("offset too large");
            lo = bytes[offset + 0];
            hi = bytes[offset + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            lo = bytes[0];
            hi = bytes[1];
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0) { bytes[offset] = lo; bytes[offset + 1] = hi; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 2) throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            destination[0] = lo; destination[1] = hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
                return false;
            destination[0] = lo;
            destination[1] = hi;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static Int16Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(short.Parse(s, style));
        }

        public static Int16Le Parse(string s, IFormatProvider? provider) => new(short.Parse(s, provider));
        public static Int16Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(short.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out Int16Le result)
        {
            if (short.TryParse(s, provider, out short v)) { result = new(v); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int16Le result)
        {
            if (short.TryParse(s, provider, out short v)) { result = new(v); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => ((short)this).ToString();
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((short)this).ToString(format, formatProvider);
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((short)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int16Le)obj); }
        public readonly int CompareTo(Int16Le other) => ((short)this).CompareTo((short)other);
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int16Le)obj);
        public readonly bool Equals(Int16Le other) => this == other;
        public override readonly int GetHashCode() => ((short)this).GetHashCode();

        #region Operators

        public static Int16Le operator +(Int16Le a) => a;
        public static Int16Le operator -(Int16Le a) => new((short)-(short)a);
        public static Int16Le operator +(Int16Le a, Int16Le b) => new((short)((short)a + (short)b));
        public static Int16Le operator -(Int16Le a, Int16Le b) => new((short)((short)a - (short)b));
        public static bool operator >(Int16Le a, Int16Le b) => (short)a > (short)b;
        public static bool operator <(Int16Le a, Int16Le b) => (short)a < (short)b;
        public static bool operator >=(Int16Le a, Int16Le b) => (short)a >= (short)b;
        public static bool operator <=(Int16Le a, Int16Le b) => (short)a <= (short)b;
        public static bool operator ==(Int16Le a, Int16Le b) => (short)a == (short)b;
        public static bool operator !=(Int16Le a, Int16Le b) => (short)a != (short)b;
        public static Int16Le operator ++(Int16Le a) => new((short)((short)a + 1));
        public static Int16Le operator --(Int16Le a) => new((short)((short)a - 1));

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(Int16Le a) => (short)((ushort)(a.hi << 8) | a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int16Le(short a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int16Le a) => (short)a;

        #endregion
    }
}
