using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 32-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt32LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct UInt32Le : IComparable, IComparable<UInt32Le>, IEquatable<UInt32Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt32Le>, ISpanParsable<UInt32Le>
    {
        [FieldOffset(0)] internal UInt16Le lo;
        [FieldOffset(2)] internal UInt16Le hi;

        public UInt32Le(UInt16Le hi, UInt16Le lo) { this.hi = hi; this.lo = lo; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Le(uint num)
        {
            lo = (UInt16Le)(ushort)(num & 0xffff);
            hi = (UInt16Le)(ushort)(num >> 16);
        }

        public UInt32Le(int num)
        {
            lo = (UInt16Le)(ushort)((uint)num & 0xffff);
            hi = (UInt16Le)(ushort)((uint)num >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            lo = new UInt16Le(bytes);
            hi = new UInt16Le(bytes.Slice(2));
        }

        public UInt32Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 4)
                throw new ArgumentException("Array is too short");
            lo.lo = bytes[offset + 0];
            lo.hi = bytes[offset + 1];
            hi.lo = bytes[offset + 2];
            hi.hi = bytes[offset + 3];
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = lo.lo;
            bytes[offset + 1] = lo.hi;
            bytes[offset + 2] = hi.lo;
            bytes[offset + 3] = hi.hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            destination[0] = lo.lo;
            destination[1] = lo.hi;
            destination[2] = hi.lo;
            destination[3] = hi.hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4) return false;
            destination[0] = lo.lo;
            destination[1] = lo.hi;
            destination[2] = hi.lo;
            destination[3] = hi.hi;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static UInt32Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber) s = s.ToLower().Replace("0x", "");
            return new(uint.Parse(s, style));
        }

        public static UInt32Le Parse(string s, IFormatProvider? provider) => new(uint.Parse(s, provider));
        public static UInt32Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(uint.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out UInt32Le result)
        {
            if (uint.TryParse(s, provider, out uint v)) { result = new(v); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt32Le result)
        {
            if (uint.TryParse(s, provider, out uint v)) { result = new(v); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => $"0x{(uint)this:x8}";
        public readonly string ToString(string? format, IFormatProvider? formatProvider) => ((uint)this).ToString(format, formatProvider);
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((uint)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj) { if (obj == null) throw new ArgumentException("obj is null"); return CompareTo((UInt32Le)obj); }
        public readonly int CompareTo(UInt32Le other) => ((uint)this).CompareTo((uint)other);
        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt32Le)obj);
        public readonly bool Equals(UInt32Le other) => this == other;
        public override readonly int GetHashCode() => ((uint)this).GetHashCode();

        #region Operators

        public static UInt32Le operator +(UInt32Le a) => a;
        public static UInt32Le operator -(UInt32Le a) => new((uint)-(int)(uint)a);
        public static UInt32Le operator +(UInt32Le a, UInt32Le b) => new((uint)a + (uint)b);
        public static UInt32Le operator -(UInt32Le a, UInt32Le b) => new((uint)a - (uint)b);
        public static bool operator >(UInt32Le a, UInt32Le b) => (uint)a > (uint)b;
        public static bool operator <(UInt32Le a, UInt32Le b) => (uint)a < (uint)b;
        public static bool operator >=(UInt32Le a, UInt32Le b) => (uint)a >= (uint)b;
        public static bool operator <=(UInt32Le a, UInt32Le b) => (uint)a <= (uint)b;
        public static bool operator ==(UInt32Le a, UInt32Le b) => (uint)a == (uint)b;
        public static bool operator !=(UInt32Le a, UInt32Le b) => (uint)a != (uint)b;
        public static UInt32Le operator *(UInt32Le a, UInt32Le b) => new((uint)a * (uint)b);
        public static UInt32Le operator /(UInt32Le a, UInt32Le b)
        {
            if ((uint)b == 0) throw new DivideByZeroException();
            return new((uint)a / (uint)b);
        }
        public static UInt32Le operator &(UInt32Le a, UInt32Le b) => new((uint)a & (uint)b);
        public static UInt32Le operator |(UInt32Le a, UInt32Le b) => new((uint)a | (uint)b);
        public static UInt32Le operator >>(UInt32Le a, int b) => new((uint)a >> b);
        public static UInt32Le operator <<(UInt32Le a, int b) => new((uint)a << b);
        public static UInt32Le operator ~(UInt32Le a) => new(~(uint)a);
        public static UInt32Le operator ++(UInt32Le a) => new((uint)a + 1);
        public static UInt32Le operator --(UInt32Le a) => new((uint)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UInt32Le a) => (uint)((uint)(ushort)a.hi << 16) | (ushort)a.lo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Le(uint a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt32Le a) => (uint)a;

        public static explicit operator ushort(UInt32Le a) => (ushort)(uint)a;
        public static explicit operator int(UInt32Le a) => (int)(uint)a;

        #endregion
    }
}
