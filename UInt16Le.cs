using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Little-endian 16-bit unsigned integer type.
    /// Stores bytes in little-endian order (least significant byte first).
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(UInt16LeTypeConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct UInt16Le : IComparable, IComparable<UInt16Le>, IEquatable<UInt16Le>,
                              IFormattable, ISpanFormattable, IParsable<UInt16Le>, ISpanParsable<UInt16Le>
    {
        [FieldOffset(0)] internal byte lo;
        [FieldOffset(1)] internal byte hi;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Le(ushort num)
        {
            lo = (byte)(num & 0xff);
            hi = (byte)(num >> 8);
        }

        public UInt16Le(int num)
        {
            if (num > ushort.MaxValue || num < ushort.MinValue)
                throw new ArgumentOutOfRangeException(nameof(num));
            lo = (byte)(num & 0xff);
            hi = (byte)((num & 0xffff) >> 8);
        }

        public UInt16Le(uint num)
        {
            if (num > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(num));
            lo = (byte)(num & 0xff);
            hi = (byte)((num & 0xffff) >> 8);
        }

        public UInt16Le(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
                throw new ArgumentException("offset too large");
            lo = bytes[offset + 0];
            hi = bytes[offset + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Le(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            lo = bytes[0];
            hi = bytes[1];
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = lo;
            bytes[offset + 1] = hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
                throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            destination[0] = lo;
            destination[1] = hi;
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

        public static void ToBytes(ushort num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)(num >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(ushort num, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, num);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Le ReadFrom(ReadOnlySpan<byte> source) => new(source);

        public static UInt16Le Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
                s = s.ToLower().Replace("0x", "");
            return new UInt16Le(ushort.Parse(s, style));
        }

        public static UInt16Le Parse(string s, IFormatProvider? provider) => new(ushort.Parse(s, provider));

        public static UInt16Le Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(ushort.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out UInt16Le result)
        {
            if (ushort.TryParse(s, provider, out ushort value)) { result = new(value); return true; }
            result = default; return false;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt16Le result)
        {
            if (ushort.TryParse(s, provider, out ushort value)) { result = new(value); return true; }
            result = default; return false;
        }

        public override readonly string ToString() => $"0x{(ushort)this:x4}";

        public readonly string ToString(string? format, IFormatProvider? formatProvider) =>
            ((ushort)this).ToString(format, formatProvider);

        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
            ((ushort)this).TryFormat(destination, out charsWritten, format, provider);

        public readonly int CompareTo(object? obj)
        {
            if (obj == null) throw new ArgumentException("obj is null");
            return CompareTo((UInt16Le)obj);
        }

        public readonly int CompareTo(UInt16Le other) => ((ushort)this).CompareTo((ushort)other);

        public override readonly bool Equals(object? obj) => obj != null && Equals((UInt16Le)obj);

        public readonly bool Equals(UInt16Le other) => this == other;

        public override readonly int GetHashCode() => ((ushort)this).GetHashCode();

        #region Operators

        public static UInt16Le operator +(UInt16Le a) => a;
        public static UInt16Le operator -(UInt16Le a) => new((ushort)-(ushort)a);
        public static UInt16Le operator +(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a + (ushort)b));
        public static UInt16Le operator -(UInt16Le a, UInt16Le b) => a + (-b);
        public static bool operator >(UInt16Le a, UInt16Le b) => (ushort)a > (ushort)b;
        public static bool operator <(UInt16Le a, UInt16Le b) => (ushort)a < (ushort)b;
        public static bool operator >=(UInt16Le a, UInt16Le b) => (ushort)a >= (ushort)b;
        public static bool operator <=(UInt16Le a, UInt16Le b) => (ushort)a <= (ushort)b;
        public static bool operator ==(UInt16Le a, UInt16Le b) => (ushort)a == (ushort)b;
        public static bool operator !=(UInt16Le a, UInt16Le b) => (ushort)a != (ushort)b;
        public static UInt16Le operator *(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a * (ushort)b));
        public static UInt16Le operator /(UInt16Le a, UInt16Le b)
        {
            if (b.hi == 0 && b.lo == 0) throw new DivideByZeroException();
            return new((ushort)((ushort)a / (ushort)b));
        }
        public static UInt16Le operator &(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a & (ushort)b));
        public static UInt16Le operator |(UInt16Le a, UInt16Le b) => new((ushort)((ushort)a | (ushort)b));
        public static UInt16Le operator >>(UInt16Le a, uint b) => new((ushort)((ushort)a >> (byte)b));
        public static UInt16Le operator <<(UInt16Le a, uint b) => new((ushort)((ushort)a << (byte)b));
        public static UInt16Le operator %(UInt16Le a, uint b) => new((ushort)((ushort)a % b));
        public static UInt16Le operator ^(UInt16Le a, uint b) => new((ushort)((ushort)a ^ b));
        public static UInt16Le operator ~(UInt16Le a) => new((ushort)~(ushort)a);
        public static UInt16Le operator ++(UInt16Le a) => new((ushort)((ushort)a + 1));
        public static UInt16Le operator --(UInt16Le a) => new((ushort)((ushort)a - 1));

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(UInt16Le a) => (ushort)((ushort)(a.hi << 8) | a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UInt16Le a) => (ushort)a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt16Le(ushort a) => new(a);

        public static explicit operator byte(UInt16Le a) => a.lo;

        public static explicit operator UInt16Le(uint a)
        {
            if (a > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(a));
            return new((ushort)a);
        }

        public static explicit operator UInt16Le(int a) => (UInt16Le)(uint)a;

        #endregion
    }
}
