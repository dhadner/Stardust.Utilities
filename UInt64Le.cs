using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 64-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt64LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct UInt64Le : IComparable, IComparable<UInt64Le>, IEquatable<UInt64Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt64Le>, ISpanParsable<UInt64Le>
    {
        [FieldOffset(0)] internal UInt32Le lo;
        [FieldOffset(4)] internal UInt32Le hi;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Le(ulong num)
        {
            lo = (UInt32Le)(uint)(num & 0xffffffff);
            hi = (UInt32Le)(uint)(num >> 32);
        }

        public UInt64Le(long num)
        {
            lo = (UInt32Le)(uint)((ulong)num & 0xffffffff);
            hi = (UInt32Le)(uint)((ulong)num >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
                throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            lo = new UInt32Le(bytes);
            hi = new UInt32Le(bytes.Slice(4));
        }

        public UInt64Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 8)
                throw new ArgumentException("Array is too short");
            lo = new UInt32Le(bytes, offset);
            hi = new UInt32Le(bytes, offset + 4);
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(4));
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
        public static UInt64Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static UInt64Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(ulong.Parse(s, style));
        }

        public static UInt64Le Parse(string s, IFormatProvider? provider) => new(ulong.Parse(s, provider));
        public static UInt64Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(ulong.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out UInt64Le result)
        {
            if (ulong.TryParse(s, provider, out ulong v)) { result = new(v); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt64Le result)
        {
            if (ulong.TryParse(s, provider, out ulong v)) { result = new(v); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => $"0x{(ulong)this:x16}";
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((ulong)this).ToString(format, formatProvider);
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((ulong)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt64Le)obj); }
        public readonly int CompareTo(UInt64Le other) => ((ulong)this).CompareTo((ulong)other);
        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt64Le)obj);
        public readonly bool Equals(UInt64Le other) => this == other;
        public override readonly int GetHashCode() => ((ulong)this).GetHashCode();

        #region Operators

        public static UInt64Le operator +(UInt64Le a) => a;
        public static UInt64Le operator -(UInt64Le a) => new((ulong)-(long)(ulong)a);
        public static UInt64Le operator +(UInt64Le a, UInt64Le b) => new((ulong)a + (ulong)b);
        public static UInt64Le operator -(UInt64Le a, UInt64Le b) => new((ulong)a - (ulong)b);
        public static bool operator >(UInt64Le a, UInt64Le b) => (ulong)a > (ulong)b;
        public static bool operator <(UInt64Le a, UInt64Le b) => (ulong)a < (ulong)b;
        public static bool operator >=(UInt64Le a, UInt64Le b) => (ulong)a >= (ulong)b;
        public static bool operator <=(UInt64Le a, UInt64Le b) => (ulong)a <= (ulong)b;
        public static bool operator ==(UInt64Le a, UInt64Le b) => (ulong)a == (ulong)b;
        public static bool operator !=(UInt64Le a, UInt64Le b) => (ulong)a != (ulong)b;
        public static UInt64Le operator *(UInt64Le a, UInt64Le b) => new((ulong)a * (ulong)b);
        public static UInt64Le operator /(UInt64Le a, UInt64Le b)
        {
            if ((ulong)b == 0) throw new DivideByZeroException();
            return new((ulong)a / (ulong)b);
        }
        public static UInt64Le operator &(UInt64Le a, UInt64Le b) => new((ulong)a & (ulong)b);
        public static UInt64Le operator |(UInt64Le a, UInt64Le b) => new((ulong)a | (ulong)b);
        public static UInt64Le operator >>(UInt64Le a, int b) => new((ulong)a >> b);
        public static UInt64Le operator <<(UInt64Le a, int b) => new((ulong)a << b);
        public static UInt64Le operator ~(UInt64Le a) => new(~(ulong)a);
        public static UInt64Le operator ++(UInt64Le a) => new((ulong)a + 1);
        public static UInt64Le operator --(UInt64Le a) => new((ulong)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt64Le a) => ((ulong)(uint)a.hi << 32) | (uint)a.lo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt64Le(ulong a) => new(a);

        public static explicit operator uint(UInt64Le a) => (uint)(ulong)a;
        public static explicit operator long(UInt64Le a) => (long)(ulong)a;

        #endregion
    }
}
