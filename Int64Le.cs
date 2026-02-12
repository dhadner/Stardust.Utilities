using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 64-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int64LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Int64Le : IComparable, IComparable<Int64Le>, IEquatable<Int64Le>,
                             IFormattable, ISpanFormattable, IParsable<Int64Le>, ISpanParsable<Int64Le>
    {
        [FieldOffset(0)] internal UInt32Le lo;
        [FieldOffset(4)] internal UInt32Le hi;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Le(long num)
        {
            lo = (UInt32Le)(uint)((ulong)num & 0xffffffff);
            hi = (UInt32Le)(uint)((ulong)num >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8) throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            lo = new UInt32Le(bytes);
            hi = new UInt32Le(bytes.Slice(4));
        }

        public Int64Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 8) throw new ArgumentException("Array is too short");
            lo = new UInt32Le(bytes, offset);
            hi = new UInt32Le(bytes, offset + 4);
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0) { lo.ToBytes(bytes, offset); hi.ToBytes(bytes, offset + 4); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8) throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            lo.WriteTo(destination); hi.WriteTo(destination.Slice(4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(4));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static Int64Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(long.Parse(s, style));
        }

        public static Int64Le Parse(string s, IFormatProvider? provider) => new(long.Parse(s, provider));
        public static Int64Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(long.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out Int64Le result)
        {
            if (long.TryParse(s, provider, out long v)) { result = new(v); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int64Le result)
        {
            if (long.TryParse(s, provider, out long v)) { result = new(v); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => ((long)this).ToString();
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((long)this).ToString(format, formatProvider);
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((long)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int64Le)obj); }
        public readonly int CompareTo(Int64Le other) => ((long)this).CompareTo((long)other);
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int64Le)obj);
        public readonly bool Equals(Int64Le other) => this == other;
        public override readonly int GetHashCode() => ((long)this).GetHashCode();

        #region Operators

        public static Int64Le operator +(Int64Le a) => a;
        public static Int64Le operator -(Int64Le a) => new(-(long)a);
        public static Int64Le operator +(Int64Le a, Int64Le b) => new((long)a + (long)b);
        public static Int64Le operator -(Int64Le a, Int64Le b) => new((long)a - (long)b);
        public static bool operator >(Int64Le a, Int64Le b) => (long)a > (long)b;
        public static bool operator <(Int64Le a, Int64Le b) => (long)a < (long)b;
        public static bool operator >=(Int64Le a, Int64Le b) => (long)a >= (long)b;
        public static bool operator <=(Int64Le a, Int64Le b) => (long)a <= (long)b;
        public static bool operator ==(Int64Le a, Int64Le b) => (long)a == (long)b;
        public static bool operator !=(Int64Le a, Int64Le b) => (long)a != (long)b;
        public static Int64Le operator ++(Int64Le a) => new((long)a + 1);
        public static Int64Le operator --(Int64Le a) => new((long)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int64Le a) => (long)(((ulong)(uint)a.hi << 32) | (uint)a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int64Le(long a) => new(a);

        public static explicit operator ulong(Int64Le a) => (ulong)(long)a;

        #endregion
    }
}
