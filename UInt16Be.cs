using System;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 16-bit unsigned integer type.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct UInt16Be : IComparable, IComparable<UInt16Be>, IEquatable<UInt16Be>
    {
        [FieldOffset(0)] internal byte hi;
        [FieldOffset(1)] internal byte lo;

        public UInt16Be(ushort num)
        {
            hi = (byte)(num >> 8);
            lo = (byte)(num & 0xff);
        }

        public UInt16Be(int num)
        {
            if (num > ushort.MaxValue || num < ushort.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        public UInt16Be(uint num)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(num, ushort.MaxValue);
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        public UInt16Be(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
            {
                throw new ArgumentException("offset too large");
            }
            hi = bytes[offset + 0];
            lo = bytes[offset + 1];
        }

        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = hi;
            bytes[offset + 1] = lo;
        }

        public static void ToBytes(ushort num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 8);
            bytes[offset + 1] = (byte)(num & 0xff);
        }

        public static UInt16Be Parse(string s, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer)
        {
            if (style == System.Globalization.NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt16Be(ushort.Parse(s, style));
        }

        public override readonly string ToString() => $"0x{(ushort)this:x4}";

        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("obj is null");
            }
            return CompareTo((UInt16Be)obj);
        }

        public readonly int CompareTo(UInt16Be other)
        {
            ushort a = (ushort)other;
            ushort b = (ushort)this;
            return b.CompareTo(a);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt16Be)obj);
        }

        public readonly bool Equals(UInt16Be other)
        {
            return this == other;
        }

        public override readonly int GetHashCode()
        {
            return ((ushort)this).GetHashCode();
        }

        public static UInt16Be operator +(UInt16Be a) => a;

        public static UInt16Be operator -(UInt16Be a) => new((ushort)-(ushort)a);

        public static UInt16Be operator +(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a + (ushort)b));

        public static UInt16Be operator -(UInt16Be a, UInt16Be b)
            => a + (-b);

        public static bool operator >(UInt16Be a, UInt16Be b)
           => (ushort)a > (ushort)b;

        public static bool operator <(UInt16Be a, UInt16Be b)
           => (ushort)a < (ushort)b;

        public static bool operator >=(UInt16Be a, UInt16Be b)
           => (ushort)a >= (ushort)b;

        public static bool operator <=(UInt16Be a, UInt16Be b)
           => (ushort)a <= (ushort)b;

        public static bool operator ==(UInt16Be a, UInt16Be b)
           => (ushort)a == (ushort)b;

        public static bool operator !=(UInt16Be a, UInt16Be b)
           => (ushort)a != (ushort)b;

        public static UInt16Be operator *(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a * (ushort)b));

        public static UInt16Be operator /(UInt16Be a, UInt16Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt16Be((ushort)((ushort)a / (ushort)b));
        }

        public static UInt16Be operator &(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a & (ushort)b));

        public static UInt16Be operator |(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a | (ushort)b));

        public static UInt16Be operator >>(UInt16Be a, uint b)
            => new((ushort)((ushort)a >> (byte)b));

        /// <summary>
        /// Shift left loses bits on the left side.
        /// Must cast the argument to UInt32Be prior to shifting
        /// (i.e., use the UInt32Be version of this operator)
        /// if the intent is to widen the result.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static UInt16Be operator <<(UInt16Be a, uint b)
            => new((ushort)((ushort)a << (byte)b));

        public static UInt16Be operator %(UInt16Be a, uint b)
            => new((ushort)((ushort)a % b));

        public static UInt16Be operator ^(UInt16Be a, uint b)
            => new((ushort)((ushort)a ^ b));

        public static UInt16Be operator ~(UInt16Be a)
            => new((ushort)~(ushort)a);

        public static UInt16Be operator ++(UInt16Be a)
            => new((ushort)((ushort)a + 1));

        public static UInt16Be operator --(UInt16Be a)
            => new((ushort)((ushort)a - 1));

        public static implicit operator ushort(UInt16Be a) => (ushort)((ushort)(a.hi << 8) | a.lo);
        public static implicit operator uint(UInt16Be a) => (ushort)a;
        public static implicit operator UInt16Be(ushort a) => new(a);
        public static implicit operator UInt32Be(UInt16Be a) => new((ushort)a);

        public static explicit operator byte(UInt16Be a) => a.hi;
        public static explicit operator UInt16Be(uint a)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(a, ushort.MaxValue);
            return new UInt16Be((ushort)a);
        }
        public static explicit operator UInt16Be(int a) => (UInt16Be)(uint)a;
    }
}
