using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 32-bit unsigned integer type.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct UInt32Be : IComparable, IComparable<UInt32Be>, IEquatable<UInt32Be>
    {
        [FieldOffset(0)] internal UInt16Be hi;
        [FieldOffset(2)] internal UInt16Be lo;

        public UInt32Be(UInt16Be hi, UInt16Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        public UInt32Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 4)
            {
                throw new ArgumentException("List is too short");
            }
            hi.hi = bytes[offset + 0];
            hi.lo = bytes[offset + 1];
            lo.hi = bytes[offset + 2];
            lo.lo = bytes[offset + 3];
        }

        public UInt32Be(uint num)
        {
            hi = (UInt16Be)(ushort)(num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        public UInt32Be(int num)
        {
            hi = (UInt16Be)(ushort)((uint)num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = hi.hi;
            bytes[offset + 1] = hi.lo;
            bytes[offset + 2] = lo.hi;
            bytes[offset + 3] = lo.lo;
        }

        public static void ToBytes(uint num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 24);
            bytes[offset + 1] = (byte)((num >> 16) & 0xff);
            bytes[offset + 2] = (byte)((num >> 8) & 0xff);
            bytes[offset + 3] = (byte)(num & 0xff);
        }

        public static UInt32Be operator +(UInt32Be a) => a;

        public static UInt32Be operator -(UInt32Be a) => new((uint)-(uint)a);

        public static UInt32Be operator +(UInt32Be a, UInt32Be b)
            => new((uint)a + (uint)b);

        public static bool operator >(UInt32Be a, UInt32Be b)
           => (uint)a > (uint)b;

        public static bool operator <(UInt32Be a, UInt32Be b)
           => (uint)a < (uint)b;

        public static bool operator >=(UInt32Be a, UInt32Be b)
           => (uint)a >= (uint)b;

        public static bool operator <=(UInt32Be a, UInt32Be b)
           => (uint)a <= (uint)b;

        public static bool operator ==(UInt32Be a, UInt32Be b)
           => (uint)a == (uint)b;

        public static bool operator !=(UInt32Be a, UInt32Be b)
           => (uint)a != (uint)b;

        public static UInt32Be operator -(UInt32Be a, UInt32Be b)
            => a + (-b);

        public static UInt32Be operator *(UInt32Be a, UInt32Be b)
            => new((uint)a * (uint)b);

        public static UInt32Be operator &(UInt32Be a, UInt32Be b)
            => new((uint)a & (uint)b);

        public static UInt32Be operator |(UInt32Be a, UInt32Be b)
            => new((uint)a | (uint)b);

        public static UInt32Be operator >>(UInt32Be a, UInt32Be b)
            => new((uint)a >> b.lo.lo);

        public static UInt32Be operator <<(UInt32Be a, UInt32Be b)
            => new((uint)a << b.lo.lo);

        public static UInt32Be operator /(UInt32Be a, UInt32Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt32Be((uint)a / (uint)b);
        }

        public static implicit operator uint(UInt32Be a) => (uint)a.hi.hi << 24 | (uint)a.hi.lo << 16 | (uint)a.lo.hi << 8 | a.lo.lo;
        public static implicit operator UInt32Be(uint a) => new(a);

        public static explicit operator ushort(UInt32Be a) => (ushort)a.hi;
        public static explicit operator byte(UInt32Be a) => a.hi.hi;
        public static explicit operator UInt16Be(UInt32Be a) => a.lo;

        public override readonly string ToString() => $"0x{hi.hi:x2}{hi.lo:x2}{lo.hi:x2}{lo.lo:x2}";

        public static UInt32Be Parse(string s, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer)
        {
            if (style == System.Globalization.NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt32Be(uint.Parse(s, style));
        }

        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((UInt32Be)obj);
        }

        public readonly int CompareTo(UInt32Be other)
        {
            uint a = (uint)other;
            uint b = (uint)this;
            return b.CompareTo(a);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt32Be)obj);
        }

        public readonly bool Equals(UInt32Be other)
        {
            return this == other;
        }

        public override readonly int GetHashCode()
        {
            return ((uint)this).GetHashCode();
        }
    }
}
