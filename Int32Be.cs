using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 32-bit signed integer type.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Int32Be : IComparable, IComparable<Int32Be>, IEquatable<Int32Be>
    {
        [FieldOffset(0)] internal UInt16Be hi;
        [FieldOffset(2)] internal UInt16Be lo;

        public Int32Be(UInt16Be hi, UInt16Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        public Int32Be(IList<byte> bytes, int offset = 0)
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

        public Int32Be(uint num)
        {
            hi = (UInt16Be)(ushort)(num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        public Int32Be(int num)
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

        public static void ToBytes(int num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)((uint)num >> 24);
            bytes[offset + 1] = (byte)((num >> 16) & 0xff);
            bytes[offset + 2] = (byte)((num >> 8) & 0xff);
            bytes[offset + 3] = (byte)(num & 0xff);
        }

        public static Int32Be operator +(Int32Be a) => a;

        public static Int32Be operator -(Int32Be a) => new(-(int)a);

        public static Int32Be operator +(Int32Be a, Int32Be b)
            => new((int)a + (int)b);

        public static bool operator >(Int32Be a, Int32Be b)
           => (int)a > (int)b;

        public static bool operator <(Int32Be a, Int32Be b)
           => (int)a < (int)b;

        public static bool operator >=(Int32Be a, Int32Be b)
           => (int)a >= (int)b;

        public static bool operator <=(Int32Be a, Int32Be b)
           => (int)a <= (int)b;

        public static bool operator ==(Int32Be a, Int32Be b)
           => (int)a == (int)b;

        public static bool operator !=(Int32Be a, Int32Be b)
           => (int)a != (int)b;

        public static Int32Be operator -(Int32Be a, Int32Be b)
            => a + (-b);

        public static Int32Be operator *(Int32Be a, Int32Be b)
            => new((int)a * (int)b);

        public static Int32Be operator &(Int32Be a, Int32Be b)
            => new((int)a & (int)b);

        public static Int32Be operator |(Int32Be a, Int32Be b)
            => new((int)a | (int)b);

        public static Int32Be operator >>(Int32Be a, Int32Be b)
            => new((int)a >> b.lo.lo);

        public static Int32Be operator <<(Int32Be a, Int32Be b)
            => new((int)a << b.lo.lo);

        public static Int32Be operator /(Int32Be a, Int32Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int32Be((int)a / (int)b);
        }

        public static implicit operator int(Int32Be a) => a.hi.hi << 24 | a.hi.lo << 16 | a.lo.hi << 8 | a.lo.lo;
        public static implicit operator Int32Be(int a) => new(a);

        public static explicit operator ushort(Int32Be a) => (ushort)a.hi;
        public static explicit operator byte(Int32Be a) => a.hi.hi;

        public static explicit operator Int32Be(Int16Be v)
        {
            throw new NotImplementedException();
        }

        public static Int32Be Parse(string s, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer)
        {
            if (style == System.Globalization.NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int32Be(int.Parse(s, style));
        }

        public override readonly string ToString() => $"0x{hi.hi:x2}{hi.lo:x2}{lo.hi:x2}{lo.lo:x2}";

        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((Int32Be)obj);
        }

        public readonly int CompareTo(Int32Be other)
        {
            int a = (int)other;
            int b = (int)this;
            return b.CompareTo(a);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int32Be)obj);
        }

        public readonly bool Equals(Int32Be other)
        {
            return this == other;
        }

        public override readonly int GetHashCode()
        {
            return ((int)this).GetHashCode();
        }
    }
}
