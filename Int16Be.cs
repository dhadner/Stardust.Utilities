using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 16-bit signed integer type.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Int16Be : IComparable, IComparable<Int16Be>, IEquatable<Int16Be>
    {
        [FieldOffset(0)] internal byte hi;
        [FieldOffset(1)] internal byte lo;

        public Int16Be(short num)
        {
            hi = (byte)(num >> 8);
            lo = (byte)(num & 0xff);
        }

        public Int16Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 2)
            {
                throw new ArgumentException("List is too short");
            }
            hi = bytes[offset + 0];
            lo = bytes[offset + 1];
        }

        public Int16Be(uint num)
        {
            if (num > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        public Int16Be(int num)
        {
            if (num > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        public void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = hi;
            bytes[offset + 1] = lo;
        }

        public static void ToBytes(short num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 8);
            bytes[offset + 1] = (byte)(num & 0xff);
        }

        public static Int16Be operator +(Int16Be a) => a;

        public static Int16Be operator -(Int16Be a) => new((short)-(short)a);

        public static Int16Be operator +(Int16Be a, Int16Be b)
            => new((short)((short)a + (short)b));

        public static Int16Be operator -(Int16Be a, Int16Be b)
            => a + (-b);

        public static bool operator >(Int16Be a, Int16Be b)
           => (short)a > (short)b;

        public static bool operator <(Int16Be a, Int16Be b)
           => (short)a < (short)b;

        public static bool operator >=(Int16Be a, Int16Be b)
           => (short)a >= (short)b;

        public static bool operator <=(Int16Be a, Int16Be b)
           => (short)a <= (short)b;

        public static bool operator ==(Int16Be a, Int16Be b)
           => (short)a == (short)b;

        public static bool operator !=(Int16Be a, Int16Be b)
           => (short)a != (short)b;

        public static Int16Be operator *(Int16Be a, Int16Be b)
            => new((short)((short)a * (short)b));

        public static Int16Be operator /(Int16Be a, Int16Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new((short)((short)a / (short)b));
        }

        public static Int16Be operator &(Int16Be a, Int16Be b)
            => new((short)((short)a & (short)b));

        public static Int16Be operator |(Int16Be a, Int16Be b)
            => new((short)((short)a | (short)b));

        public static Int16Be operator >>(Int16Be a, int b)
            => new((short)((short)a >> (byte)b));

        /// <summary>
        /// Shift left loses bits on the left side.
        /// Must cast the argument to UInt32Be prior to shifting
        /// (i.e., use the UInt32Be version of this operator)
        /// if the intent is to widen the result.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int16Be operator <<(Int16Be a, int b)
            => new((short)((short)a << (byte)b));

        public static Int16Be operator %(Int16Be a, int b)
            => new((short)((short)a % b));

        public static Int16Be operator ^(Int16Be a, int b)
            => new((short)((short)a ^ b));

        public static Int16Be operator ~(Int16Be a)
            => new((short)~(short)a);

        public static Int16Be operator ++(Int16Be a)
            => new((short)((short)a + 1));

        public static Int16Be operator --(Int16Be a)
            => new((short)((short)a - 1));

        public static implicit operator short(Int16Be a) => (short)((a.hi << 8) | a.lo);
        public static implicit operator int(Int16Be a) => (short)a;
        public static implicit operator Int16Be(short a) => new(a);
        public static implicit operator UInt32Be(Int16Be a) => new((short)a);

        public static explicit operator byte(Int16Be a) => a.hi;
        public static explicit operator Int16Be(uint a)
        {
            if (a > short.MaxValue || (int)a < short.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(a));
            }
            return new Int16Be((short)a);
        }

        public static Int16Be Parse(string s, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer)
        {
            if (style == System.Globalization.NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int16Be(short.Parse(s, style));
        }

        public override string ToString() => $"0x{(short)this:x4}";

        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException();
            }
            return CompareTo((Int16Be)obj);
        }

        public int CompareTo(Int16Be other)
        {
            short a = (short)other;
            short b = (short)this;
            return b.CompareTo(a);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException();
            }
            return Equals((Int16Be)obj);
        }

        public bool Equals(Int16Be other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return ((short)this).GetHashCode();
        }
    }
}
