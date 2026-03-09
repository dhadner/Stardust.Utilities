using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 32-bit signed integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(Int32LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Int32Le : IComparable, IComparable<Int32Le>, IEquatable<Int32Le>,
                             IFormattable, ISpanFormattable, IParsable<Int32Le>, ISpanParsable<Int32Le>
    {
        [FieldOffset(0)] internal UInt16Le lo;
        [FieldOffset(2)] internal UInt16Le hi;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Le(int num)
        {
            lo = (UInt16Le)(ushort)((uint)num & 0xffff);
            hi = (UInt16Le)(ushort)((uint)num >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4) throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            lo = new UInt16Le(bytes);
            hi = new UInt16Le(bytes.Slice(2));
        }

        public Int32Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 4) throw new ArgumentException("Array is too short");
            lo = new UInt16Le(bytes, offset);
            hi = new UInt16Le(bytes, offset + 2);
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            lo.ToBytes(bytes, offset);
            hi.ToBytes(bytes, offset + 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4) throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                return false;
            lo.WriteTo(destination);
            hi.WriteTo(destination.Slice(2));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static Int32Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(int.Parse(s, style));
        }

        public static Int32Le Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider));
        public static Int32Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(int.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out Int32Le result)
        {
            if (int.TryParse(s, provider, out int v)) { result = new(v); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int32Le result)
        {
            if (int.TryParse(s, provider, out int v)) { result = new(v); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => ((int)this).ToString();
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((int)this).ToString(format, formatProvider);
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((int)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((Int32Le)obj); }
        public readonly int CompareTo(Int32Le other) => ((int)this).CompareTo((int)other);
        public override readonly bool Equals(object? obj) => obj != null && Equals((Int32Le)obj);
        public readonly bool Equals(Int32Le other) => this == other;
        public override readonly int GetHashCode() => ((int)this).GetHashCode();

        #region Operators

        public static Int32Le operator +(Int32Le a) => a;
        public static Int32Le operator -(Int32Le a) => new(-(int)a);
        public static Int32Le operator +(Int32Le a, Int32Le b) => new((int)a + (int)b);
        public static Int32Le operator -(Int32Le a, Int32Le b) => new((int)a - (int)b);
        public static bool operator >(Int32Le a, Int32Le b) => (int)a > (int)b;
        public static bool operator <(Int32Le a, Int32Le b) => (int)a < (int)b;
        public static bool operator >=(Int32Le a, Int32Le b) => (int)a >= (int)b;
        public static bool operator <=(Int32Le a, Int32Le b) => (int)a <= (int)b;
        public static bool operator ==(Int32Le a, Int32Le b) => (int)a == (int)b;
        public static bool operator !=(Int32Le a, Int32Le b) => (int)a != (int)b;
        public static Int32Le operator ++(Int32Le a) => new((int)a + 1);
        public static Int32Le operator --(Int32Le a) => new((int)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int32Le a) => (int)(((uint)(ushort)a.hi << 16) | (ushort)a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int32Le(int a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int32Le a) => (int)a;

        public static explicit operator uint(Int32Le a) => (uint)(int)a;

        #endregion
    }
}
