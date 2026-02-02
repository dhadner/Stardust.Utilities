using System;
using System.Runtime.CompilerServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Extension methods for byte ordering and other utilities.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this ushort value)
        {
            return (byte)(value & 0xff);
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this short value)
        {
            return (byte)(value & 0xff);
        }

        /// <summary>
        /// Least-significant ushort.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Lo(this uint value)
        {
            return (ushort)(value & 0xffff);
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this UInt16Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Lo(this UInt32Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lo(this Int16Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Lo(this Int32Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Lo(this ulong value)
        {
            return (uint)(value & 0xFFFFFFFF);
        }

        /// <summary>
        /// Least-significant uint (lower 32 bits).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Lo(this long value)
        {
            return (uint)(value & 0xFFFFFFFF);
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Lo(this UInt64Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Lo(this Int64Be value)
        {
            return value.lo;
        }

        /// <summary>
        /// Least-significant ushort.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Lo(this int value)
        {
            return (ushort)(value & 0xffff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SetLo(this ushort value, byte lo)
        {
            return (ushort)((value & 0xff00) | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SetLo(this short value, byte lo)
        {
            return (short)(((ushort)(value & 0xff00)) | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetLo(this uint value, ushort lo)
        {
            return (value & 0xffff0000) | lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetLo(this int value, ushort lo)
        {
            return (int)((value & 0xffff0000) | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be SetLo(this UInt16Be value, byte lo)
        {
            return (value & 0xff00) | ((ushort)lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be SetLo(this UInt32Be value, ushort lo)
        {
            return ((value & 0xffff0000) | ((uint)lo));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be SetLo(this Int16Be value, byte lo)
        {
            return (Int16Be)(ushort)(((ushort)(value & 0xff00)) | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be SetLo(this Int32Be value, ushort lo)
        {
            return (Int32Be)((value & 0xffff0000) | lo);
        }

        /// <summary>
        /// Set least-significant uint (lower 32 bits).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetLo(this ulong value, uint lo)
        {
            return (value & 0xFFFFFFFF00000000) | lo;
        }

        /// <summary>
        /// Set least-significant uint (lower 32 bits).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SetLo(this long value, uint lo)
        {
            return (long)((ulong)(value & unchecked((long)0xFFFFFFFF00000000)) | lo);
        }

        /// <summary>
        /// Set least-significant UInt32Be.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Be SetLo(this UInt64Be value, UInt32Be lo)
        {
            return new UInt64Be(value.hi, lo);
        }

        /// <summary>
        /// Set least-significant UInt32Be.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be SetLo(this Int64Be value, UInt32Be lo)
        {
            return new Int64Be(value.hi, lo);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this ushort value)
        {
            return (byte)(value >> 8);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this short value)
        {
            return (byte)(value >> 8);
        }

        /// <summary>
        /// Most-significant ushort.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Hi(this uint value)
        {
            return (ushort)(value >> 16);
        }

        /// <summary>
        /// Most-significant ushort.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Hi(this int value)
        {
            return (ushort)((uint)value >> 16);
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this UInt16Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt16Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Hi(this UInt32Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most significant byte.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Hi(this Int16Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt16Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be Hi(this Int32Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hi(this ulong value)
        {
            return (uint)(value >> 32);
        }

        /// <summary>
        /// Most-significant uint (upper 32 bits).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hi(this long value)
        {
            return (uint)((ulong)value >> 32);
        }

        /// <summary>
        /// Most-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Hi(this UInt64Be value)
        {
            return value.hi;
        }

        /// <summary>
        /// Most-significant UInt32Be.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32Be Hi(this Int64Be value)
        {
            return value.hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SetHi(this ushort value, byte hi)
        {
            return (ushort)((value & 0x00ff) | hi << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SetHi(this short value, byte hi)
        {
            return (short)((value & 0x00ff) | hi << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetHi(this uint value, ushort hi)
        {
            return (value & 0x0000ffff) | ((uint)hi << 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetHi(this int value, ushort hi)
        {
            return (int)((uint)(value & 0x0000ffff) | (uint)hi << 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be SetHi(this UInt16Be value, byte hi)
        {
            return (UInt16Be)(((ushort)value & 0x00ff) | hi << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be SetHi(this Int16Be value, byte hi)
        {
            return (Int16Be)(ushort)(((ushort)value & 0x00ff) | hi << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be SetHi(this Int32Be value, Int16Be hi)
        {
            return (value & 0x0000ffff) | ((Int32Be)hi << 16);
        }

        /// <summary>
        /// Set most-significant uint (upper 32 bits).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetHi(this ulong value, uint hi)
        {
            return (value & 0x00000000FFFFFFFF) | ((ulong)hi << 32);
        }

        /// <summary>
        /// Set most-significant uint (upper 32 bits).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SetHi(this long value, uint hi)
        {
            return (long)((ulong)(value & 0x00000000FFFFFFFF) | ((ulong)hi << 32));
        }

        /// <summary>
        /// Set most-significant UInt32Be.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Be SetHi(this UInt64Be value, UInt32Be hi)
        {
            return new UInt64Be(hi, value.lo);
        }

        /// <summary>
        /// Set most-significant UInt32Be.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be SetHi(this Int64Be value, UInt32Be hi)
        {
            return new Int64Be(hi, value.lo);
        }

        /// <summary>
        /// Subtracts two values, returning MinValue on underflow or MaxValue on overflow (for signed types).
        /// For unsigned types, returns 0 if the result would underflow.
        /// </summary>
        /// <param name="a">The value to subtract from.</param>
        /// <param name="b">The value to subtract.</param>
        /// <returns>The saturated difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SaturatingSub(this int a, int b)
        {
            int result = a - b;
            // Check for overflow: if signs of a and b differ and result has wrong sign
            if (((a ^ b) & (a ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? int.MinValue : int.MaxValue;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SaturatingSub(this long a, long b)
        {
            long result = a - b;
            // Check for overflow: if signs of a and b differ and result has wrong sign
            if (((a ^ b) & (a ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? long.MinValue : long.MaxValue;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SaturatingSub(this uint a, uint b)
        {
            // For unsigned, just check if b > a (would underflow)
            return b > a ? uint.MinValue : a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SaturatingSub(this ulong a, ulong b)
        {
            // For unsigned, just check if b > a (would underflow)
            return b > a ? ulong.MinValue : a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SaturatingAdd(this int a, int b)
        {
            int result = a + b;
            // Check for overflow: if signs of a and b are same and result has different sign
            if (((a ^ result) & (b ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? int.MinValue : int.MaxValue;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SaturatingAdd(this long a, long b)
        {
            long result = a + b;
            // Check for overflow: if signs of a and b are same and result has different sign
            if (((a ^ result) & (b ^ result)) < 0)
            {
                // Overflow occurred: return appropriate limit
                return a < 0 ? long.MinValue : long.MaxValue;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SaturatingAdd(this uint a, uint b)
        {
            uint result = a + b;
            // For unsigned, overflow if result < a (wrapped around)
            return result < a ? uint.MaxValue : result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SaturatingAdd(this ulong a, ulong b)
        {
            ulong result = a + b;
            // For unsigned, overflow if result < a (wrapped around)
            return result < a ? ulong.MaxValue : result;
        }
    }
}
