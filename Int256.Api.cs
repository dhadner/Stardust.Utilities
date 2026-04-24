using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Stardust.Utilities
{
    /// <summary>
    /// Extended API surface for <see cref="Int256"/> that mirrors the BCL
    /// <see cref="Int128"/> type: static math helpers, bit-level operations,
    /// endian read/write, checked operators, float / decimal conversions, and
    /// the full <see cref="INumber{TSelf}"/> / <see cref="IBinaryInteger{TSelf}"/>
    /// generic-math interface set.
    /// </summary>
    public readonly partial struct Int256 :
        INumber<Int256>,
        IBinaryInteger<Int256>,
        IBinaryNumber<Int256>,
        IMinMaxValue<Int256>,
        ISignedNumber<Int256>
#if NET8_0_OR_GREATER
        ,
        IUtf8SpanFormattable,
        IUtf8SpanParsable<Int256>
#endif
    {
        private const double TWO_POW_64_D  = 1.8446744073709552e+19;
        private const double TWO_POW_128_D = 3.4028236692093846e+38;
        private const double TWO_POW_192_D = 6.277101735386680e+57;
        private const double TWO_POW_255_D = 5.7896044618658097e+76; // 2^255

        #region A. Static math helpers

        /// <summary>Returns the absolute value.</summary>
        /// <param name="value">The value.</param>
        /// <returns>|value|.</returns>
        /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MinValue"/>.</exception>
        public static Int256 Abs(Int256 value)
        {
            if (IsNegative(value))
            {
                Int256 r = -value;
                if (IsNegative(r)) throw new OverflowException();
                return r;
            }
            return value;
        }

        /// <summary>Clamps a value to an inclusive range.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">Inclusive lower bound.</param>
        /// <param name="max">Inclusive upper bound.</param>
        /// <returns>The clamped value.</returns>
        /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        public static Int256 Clamp(Int256 value, Int256 min, Int256 max)
        {
            if (min > max) throw new ArgumentException("min must be <= max", nameof(min));
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>Returns <paramref name="value"/> with the sign of <paramref name="sign"/>.</summary>
        /// <param name="value">Magnitude source.</param>
        /// <param name="sign">Sign source.</param>
        /// <returns>|value| with sign(sign).</returns>
        public static Int256 CopySign(Int256 value, Int256 sign)
        {
            Int256 abs = IsNegative(value) ? -value : value;
            return IsNegative(sign) ? -abs : abs;
        }

        /// <summary>Computes the quotient and remainder of two values (truncating toward zero).</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>A tuple of (Quotient, Remainder).</returns>
        /// <exception cref="DivideByZeroException"><paramref name="right"/> is zero.</exception>
        public static (Int256 Quotient, Int256 Remainder) DivRem(Int256 left, Int256 right)
        {
            Int256 q = left / right;
            Int256 r = left - q * right;
            return (q, r);
        }

        /// <summary>Returns the greater of two values (signed comparison).</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The greater of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 Max(Int256 x, Int256 y) => x >= y ? x : y;

        /// <summary>Returns the lesser of two values (signed comparison).</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The lesser of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 Min(Int256 x, Int256 y) => x <= y ? x : y;

        /// <summary>Returns the value with the greater magnitude (by |x| vs |y|).</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The value with greater magnitude.</returns>
        public static Int256 MaxMagnitude(Int256 x, Int256 y)
        {
            Int256 ax = IsNegative(x) ? -x : x;
            Int256 ay = IsNegative(y) ? -y : y;
            if (ax > ay) return x;
            if (ax < ay) return y;
            return IsNegative(x) ? y : x;
        }

        /// <summary>Returns the value with the lesser magnitude.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The value with lesser magnitude.</returns>
        public static Int256 MinMagnitude(Int256 x, Int256 y)
        {
            Int256 ax = IsNegative(x) ? -x : x;
            Int256 ay = IsNegative(y) ? -y : y;
            if (ax < ay) return x;
            if (ax > ay) return y;
            return IsNegative(x) ? x : y;
        }

        /// <summary>INumberBase-required: alias for <see cref="MaxMagnitude"/>.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>See <see cref="MaxMagnitude"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 MaxMagnitudeNumber(Int256 x, Int256 y) => MaxMagnitude(x, y);

        /// <summary>INumberBase-required: alias for <see cref="MinMagnitude"/>.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>See <see cref="MinMagnitude"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 MinMagnitudeNumber(Int256 x, Int256 y) => MinMagnitude(x, y);

        /// <summary>Returns the sign of a value: -1 if negative, 0 if zero, 1 if positive.</summary>
        /// <param name="value">The value.</param>
        /// <returns>-1, 0, or 1.</returns>
        public static int Sign(Int256 value)
        {
            if (IsNegative(value)) return -1;
            if ((value._p0 | value._p1 | value._p2 | value._p3) == 0UL) return 0;
            return 1;
        }

        #endregion

        #region B. Bit-level operations

        /// <summary>Counts the number of leading zero bits (0..256).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of leading zero bits as an <see cref="Int256"/>.</returns>
        public static Int256 LeadingZeroCount(Int256 value)
        {
            ulong p3 = value._p3;
            ulong p2 = value._p2;
            ulong p1 = value._p1;
            ulong p0 = value._p0;
            if (p3 != 0) return BitOperations.LeadingZeroCount(p3);
            if (p2 != 0) return 64 + BitOperations.LeadingZeroCount(p2);
            if (p1 != 0) return 128 + BitOperations.LeadingZeroCount(p1);
            return 192 + BitOperations.LeadingZeroCount(p0);
        }

        /// <summary>Counts the number of trailing zero bits (0..256).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of trailing zero bits as an <see cref="Int256"/>.</returns>
        public static Int256 TrailingZeroCount(Int256 value)
        {
            ulong p0 = value._p0;
            ulong p1 = value._p1;
            ulong p2 = value._p2;
            ulong p3 = value._p3;
            if (p0 != 0) return BitOperations.TrailingZeroCount(p0);
            if (p1 != 0) return 64 + BitOperations.TrailingZeroCount(p1);
            if (p2 != 0) return 128 + BitOperations.TrailingZeroCount(p2);
            return 192 + BitOperations.TrailingZeroCount(p3);
        }

        /// <summary>Counts the number of set bits (population count).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of set bits as an <see cref="Int256"/>.</returns>
        public static Int256 PopCount(Int256 value)
        {
            ulong p0 = value._p0;
            ulong p1 = value._p1;
            ulong p2 = value._p2;
            ulong p3 = value._p3;
            return BitOperations.PopCount(p0) + BitOperations.PopCount(p1)
                 + BitOperations.PopCount(p2) + BitOperations.PopCount(p3);
        }

        /// <summary>Computes the integer (floor) base-2 logarithm.</summary>
        /// <param name="value">The value. Must be non-negative.</param>
        /// <returns>Floor(log2(value)) as an <see cref="Int256"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
        public static Int256 Log2(Int256 value)
        {
            if (IsNegative(value)) throw new ArgumentOutOfRangeException(nameof(value));
            ulong p0 = value._p0;
            ulong p1 = value._p1;
            ulong p2 = value._p2;
            ulong p3 = value._p3;
            if (p3 != 0) return 192 + BitOperations.Log2(p3);
            if (p2 != 0) return 128 + BitOperations.Log2(p2);
            if (p1 != 0) return 64 + BitOperations.Log2(p1);
            return BitOperations.Log2(p0);
        }

        /// <summary>Rotates the value left by the specified number of bits (mod 256).</summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="rotateAmount">Rotate amount (interpreted mod 256).</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 RotateLeft(Int256 value, int rotateAmount)
            => (Int256)UInt256.RotateLeft((UInt256)value, rotateAmount);

        /// <summary>Rotates the value right by the specified number of bits (mod 256).</summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="rotateAmount">Rotate amount (interpreted mod 256).</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 RotateRight(Int256 value, int rotateAmount)
            => (Int256)UInt256.RotateRight((UInt256)value, rotateAmount);

        /// <summary>Returns the fixed byte size of the type: 32.</summary>
        /// <returns>Always 32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByteCount() => 32;

        /// <summary>Returns the number of bits needed to represent the current value. Matches BCL <see cref="Int128"/> semantics.</summary>
        /// <returns>Significant bit length.</returns>
        public int GetShortestBitLength()
        {
            if (IsNegative(this))
            {
                UInt256 u = ~(UInt256)this;
                int lzc = u._p3 != 0 ? BitOperations.LeadingZeroCount(u._p3)
                        : u._p2 != 0 ? 64 + BitOperations.LeadingZeroCount(u._p2)
                        : u._p1 != 0 ? 128 + BitOperations.LeadingZeroCount(u._p1)
                        : u._p0 != 0 ? 192 + BitOperations.LeadingZeroCount(u._p0)
                        : 256;
                return 257 - lzc;
            }
            UInt256 up = (UInt256)this;
            if (up._p3 != 0) return 256 - BitOperations.LeadingZeroCount(up._p3);
            if (up._p2 != 0) return 192 - BitOperations.LeadingZeroCount(up._p2);
            if (up._p1 != 0) return 128 - BitOperations.LeadingZeroCount(up._p1);
            if (up._p0 != 0) return 64 - BitOperations.LeadingZeroCount(up._p0);
            return 0;
        }

        #endregion

        #region C. Is-predicates

        /// <summary>Determines whether the value is zero.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(Int256 value) => (value._p0 | value._p1 | value._p2 | value._p3) == 0UL;

        /// <summary>Determines whether the value is an even integer.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if even.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenInteger(Int256 value) => (value._p0 & 1UL) == 0UL;

        /// <summary>Determines whether the value is an odd integer.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if odd.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOddInteger(Int256 value) => (value._p0 & 1UL) != 0UL;

        /// <summary>Determines whether the value is a (positive) power of two.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is a positive power of two.</returns>
        public static bool IsPow2(Int256 value)
        {
            if (IsNegative(value) || IsZero(value)) return false;
            return UInt256.PopCount((UInt256)value) == UInt256.One;
        }

        /// <summary>Returns <c>true</c> when the value is negative.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if negative.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(Int256 value) => (value._p3 & SIGN_BIT) != 0UL;

        /// <summary>Returns <c>true</c> when the value is zero or positive (matches BCL Int128 semantics).</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if non-negative.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(Int256 value) => (value._p3 & SIGN_BIT) == 0UL;

        /// <summary>Always returns <c>true</c> for fixed-width integers.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanonical(Int256 value) => true;

        /// <summary>Always returns <c>false</c> for real integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComplexNumber(Int256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(Int256 value) => true;

        /// <summary>Always returns <c>false</c> for real integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImaginaryNumber(Int256 value) => false;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(Int256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(Int256 value) => true;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(Int256 value) => false;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(Int256 value) => false;

        /// <summary>Returns <c>true</c> when the value is non-zero.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if non-zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(Int256 value) => !IsZero(value);

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(Int256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(Int256 value) => true;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubnormal(Int256 value) => false;

        #endregion

        #region D. Endian Read/Write

        /// <summary>Reads an <see cref="Int256"/> value from a big-endian byte sequence.</summary>
        /// <param name="source">The source bytes in big-endian order.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source"/> represents an unsigned value;
        /// <c>false</c> if signed with sign-extension.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="OverflowException">The source exceeds 256 bits or has sign overflow.</exception>
        public static Int256 ReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned)
        {
            if (!TryReadBigEndian(source, isUnsigned, out Int256 value))
                throw new OverflowException("Value too large for Int256.");
            return value;
        }

        /// <summary>Reads an <see cref="Int256"/> value from a little-endian byte sequence.</summary>
        /// <param name="source">The source bytes in little-endian order.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source"/> represents an unsigned value.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="OverflowException">The source exceeds 256 bits or has sign overflow.</exception>
        public static Int256 ReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned)
        {
            if (!TryReadLittleEndian(source, isUnsigned, out Int256 value))
                throw new OverflowException("Value too large for Int256.");
            return value;
        }

        /// <summary>Attempts to read an <see cref="Int256"/> from a big-endian byte sequence.</summary>
        /// <param name="source">The source bytes.</param>
        /// <param name="isUnsigned">Whether the source is unsigned.</param>
        /// <param name="value">The parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Int256 value)
        {
            if (source.IsEmpty) { value = default; return true; }
            int len = source.Length;
            bool sourceNeg = !isUnsigned && (source[0] & 0x80) != 0;
            byte signByte = sourceNeg ? (byte)0xFF : (byte)0;
            if (len > 32)
            {
                int excess = len - 32;
                for (int i = 0; i < excess; i++)
                    if (source[i] != signByte) { value = default; return false; }
                source = source[excess..];
                len = 32;
            }
            if (isUnsigned && len == 32 && (source[0] & 0x80) != 0) { value = default; return false; }
            Span<byte> buf = stackalloc byte[32];
            if (sourceNeg) buf.Fill(0xFF);
            source.CopyTo(buf[(32 - len)..]);
            value = new Int256(buf, isBigEndian: true);
            return true;
        }

        /// <summary>Attempts to read an <see cref="Int256"/> from a little-endian byte sequence.</summary>
        /// <param name="source">The source bytes.</param>
        /// <param name="isUnsigned">Whether the source is unsigned.</param>
        /// <param name="value">The parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Int256 value)
        {
            if (source.IsEmpty) { value = default; return true; }
            int len = source.Length;
            bool sourceNeg = !isUnsigned && (source[len - 1] & 0x80) != 0;
            byte signByte = sourceNeg ? (byte)0xFF : (byte)0;
            if (len > 32)
            {
                for (int i = 32; i < len; i++)
                    if (source[i] != signByte) { value = default; return false; }
                source = source[..32];
                len = 32;
            }
            if (isUnsigned && len == 32 && (source[len - 1] & 0x80) != 0) { value = default; return false; }
            Span<byte> buf = stackalloc byte[32];
            if (sourceNeg) buf.Fill(0xFF);
            source.CopyTo(buf);
            value = new Int256(buf, isBigEndian: false);
            return true;
        }

        /// <summary>Attempts to write this value in big-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <param name="bytesWritten">Bytes written on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < 32) { bytesWritten = 0; return false; }
            ulong p3 = _p3;
            ulong p2 = _p2;
            ulong p1 = _p1;
            ulong p0 = _p0;
            BinaryPrimitives.WriteUInt64BigEndian(destination[..8], p3);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8, 8), p2);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(16, 8), p1);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(24, 8), p0);
            bytesWritten = 32;
            return true;
        }

        /// <summary>Attempts to write this value in little-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <param name="bytesWritten">Bytes written on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < 32) { bytesWritten = 0; return false; }
            ulong p0 = _p0;
            ulong p1 = _p1;
            ulong p2 = _p2;
            ulong p3 = _p3;
            BinaryPrimitives.WriteUInt64LittleEndian(destination[..8], p0);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8, 8), p1);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(16, 8), p2);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(24, 8), p3);
            bytesWritten = 32;
            return true;
        }

        /// <summary>Writes this value in big-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <returns>Bytes written.</returns>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public int WriteBigEndian(Span<byte> destination)
        {
            if (!TryWriteBigEndian(destination, out int n))
                throw new ArgumentException("Destination too short.", nameof(destination));
            return n;
        }

        /// <summary>Writes this value in little-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <returns>Bytes written.</returns>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public int WriteLittleEndian(Span<byte> destination)
        {
            if (!TryWriteLittleEndian(destination, out int n))
                throw new ArgumentException("Destination too short.", nameof(destination));
            return n;
        }

        #endregion

        #region E. Checked operators

        /// <summary>Checked addition; throws on signed overflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The sum.</returns>
        /// <exception cref="OverflowException">The signed result does not fit in <see cref="Int256"/>.</exception>
        public static Int256 operator checked +(Int256 a, Int256 b)
        {
            Int256 r = a + b;
            bool aN = IsNegative(a), bN = IsNegative(b), rN = IsNegative(r);
            if (aN == bN && aN != rN) throw new OverflowException();
            return r;
        }

        /// <summary>Checked subtraction; throws on signed overflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The difference.</returns>
        /// <exception cref="OverflowException">The signed result does not fit in <see cref="Int256"/>.</exception>
        public static Int256 operator checked -(Int256 a, Int256 b)
        {
            Int256 r = a - b;
            bool aN = IsNegative(a), bN = IsNegative(b), rN = IsNegative(r);
            if (aN != bN && aN != rN) throw new OverflowException();
            return r;
        }

        /// <summary>Checked unary negation; throws on <see cref="MinValue"/>.</summary>
        /// <param name="a">The operand.</param>
        /// <returns>-<paramref name="a"/>.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is <see cref="MinValue"/>.</exception>
        public static Int256 operator checked -(Int256 a)
        {
            if (a == MinValue) throw new OverflowException();
            return -a;
        }

        /// <summary>Checked multiplication; throws on signed overflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The product.</returns>
        /// <exception cref="OverflowException">The signed result does not fit in <see cref="Int256"/>.</exception>
        public static Int256 operator checked *(Int256 a, Int256 b)
        {
            if (IsZero(a) || IsZero(b)) return Zero;
            bool aN = IsNegative(a), bN = IsNegative(b);
            UInt256 au = aN ? (UInt256)(-a) : (UInt256)a;
            UInt256 bu = bN ? (UInt256)(-b) : (UInt256)b;
            UInt256 prod = checked(au * bu);
            bool resultNeg = aN ^ bN;
            UInt256 limit = resultNeg
                ? new UInt256((UInt128)1 << 127, UInt128.Zero) // |MinValue|
                : new UInt256(~UInt128.Zero ^ ((UInt128)1 << 127), ~UInt128.Zero); // MaxValue
            if (prod > limit) throw new OverflowException();
            if (resultNeg)
            {
                if (prod == new UInt256((UInt128)1 << 127, UInt128.Zero)) return MinValue;
                return (Int256)(UInt256.Zero - prod);
            }
            return new Int256(prod.Upper, prod.Lower);
        }

        /// <summary>Checked division; throws on <see cref="MinValue"/> / -1.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The quotient.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is <see cref="MinValue"/> and <paramref name="b"/> is -1.</exception>
        /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
        public static Int256 operator checked /(Int256 a, Int256 b)
        {
            if (a == MinValue && b == NegativeOne) throw new OverflowException();
            return a / b;
        }

        /// <summary>Checked increment; throws on <see cref="MaxValue"/>.</summary>
        /// <param name="a">The value to increment.</param>
        /// <returns>The incremented value.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is <see cref="MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator checked ++(Int256 a) => checked(a + One);

        /// <summary>Checked decrement; throws on <see cref="MinValue"/>.</summary>
        /// <param name="a">The value to decrement.</param>
        /// <returns>The decremented value.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is <see cref="MinValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator checked --(Int256 a) => checked(a - One);

        #endregion

        #region F. Additional parsing

        /// <summary>Parses using the specified <paramref name="style"/> and culture.</summary>
        /// <param name="s">The input string.</param>
        /// <param name="style">Number style to apply.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static Int256 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>Parses a span using the specified <paramref name="style"/> and culture.</summary>
        /// <param name="s">The input span.</param>
        /// <param name="style">Number style to apply.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int256 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            if (style == NumberStyles.Integer || style == NumberStyles.None)
                return Parse(s.ToString(), NumberStyles.Integer);
            if ((style & NumberStyles.HexNumber) == NumberStyles.HexNumber)
                return Parse(s.ToString(), NumberStyles.HexNumber);
            BigInteger big = BigInteger.Parse(s.ToString(), style, provider ?? CultureInfo.InvariantCulture);
            return FromBigInteger(big);
        }

        /// <summary>Attempts to parse a string using the specified style and culture.</summary>
        /// <param name="s">The input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Int256 result)
        {
            if (s is null) { result = default; return false; }
            try { result = Parse(s, style, provider); return true; }
            catch { result = default; return false; }
        }

        /// <summary>Attempts to parse a span using the specified style and culture.</summary>
        /// <param name="s">The input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Int256 result)
        {
            try { result = Parse(s, style, provider); return true; }
            catch { result = default; return false; }
        }

        #endregion

        #region G. UTF-8 parsing/formatting

        /// <summary>Formats this value into a UTF-8 destination span.</summary>
        /// <param name="utf8Destination">The destination span.</param>
        /// <param name="bytesWritten">Bytes written on success.</param>
        /// <param name="format">Format string.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns><c>true</c> on success.</returns>
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            string s = ToString(format.IsEmpty ? "G" : format.ToString(), provider);
            int needed = Encoding.UTF8.GetByteCount(s);
            if (utf8Destination.Length < needed) { bytesWritten = 0; return false; }
            bytesWritten = Encoding.UTF8.GetBytes(s, utf8Destination);
            return true;
        }

#if NET8_0_OR_GREATER
        /// <summary>Parses a value from a UTF-8 byte sequence.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int256 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
            => Parse(Encoding.UTF8.GetString(utf8Text), NumberStyles.Integer, provider);

        /// <summary>Parses a value from a UTF-8 byte sequence with the given number style.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        public static Int256 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
            => Parse(Encoding.UTF8.GetString(utf8Text), style, provider);

        /// <summary>Attempts to parse a value from a UTF-8 byte sequence.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Int256 result)
            => TryParse(Encoding.UTF8.GetString(utf8Text), NumberStyles.Integer, provider, out result);

        /// <summary>Attempts to parse a value from a UTF-8 byte sequence.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Int256 result)
            => TryParse(Encoding.UTF8.GetString(utf8Text), style, provider, out result);
#endif

        #endregion

        #region H. Additional conversions

        /// <summary>Implicitly widens a <see cref="char"/> to an <see cref="Int256"/>.</summary>
        /// <param name="a">The input character.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(char a) => new((long)a);

        /// <summary>Implicitly widens a <see cref="byte"/> to an <see cref="Int256"/>.</summary>
        /// <param name="a">The input byte.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(byte a) => new((long)a);

        /// <summary>Implicitly widens a <see cref="ushort"/> to an <see cref="Int256"/>.</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(ushort a) => new((long)a);

        /// <summary>Implicitly widens a <see cref="uint"/> to an <see cref="Int256"/>.</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(uint a) => new((long)a);

        /// <summary>Explicit widening from <see cref="ulong"/> (matches BCL cross-sign policy).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(ulong a) => new(UInt128.Zero, a);

        /// <summary>Explicit widening from <see cref="UInt128"/> (matches BCL cross-sign policy).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt128 a) => new(UInt128.Zero, a);

        /// <summary>Converts a <see cref="float"/> to <see cref="Int256"/>; truncates toward zero.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(float value) => (Int256)(double)value;

        /// <summary>Converts a <see cref="double"/> to <see cref="Int256"/>; truncates toward zero.</summary>
        /// <param name="value">The input.</param>
        public static explicit operator Int256(double value)
        {
            if (double.IsNaN(value)) return Zero;
            if (value >= TWO_POW_255_D) return MinValue;                // matches BCL: positive overflow wraps to MinValue
            if (value < -TWO_POW_255_D) return MinValue;
            if (value >= 0.0)
            {
                UInt256 u = (UInt256)value;
                return new Int256(u.Upper, u.Lower);
            }
            UInt256 mag = (UInt256)(-value);
            return (Int256)(UInt256.Zero - mag);
        }

        /// <summary>Converts a <see cref="decimal"/> to <see cref="Int256"/>.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(decimal value)
            => FromBigInteger(new BigInteger(decimal.Truncate(value)));

        /// <summary>Checked conversion from <see cref="float"/>; throws on NaN, Infinity, or overflow.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">Value is outside valid range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator checked Int256(float value) => checked((Int256)(double)value);

        /// <summary>Checked conversion from <see cref="double"/>; throws on NaN, Infinity, or overflow.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">Value is outside valid range.</exception>
        public static explicit operator checked Int256(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException();
            if (value >= TWO_POW_255_D || value < -TWO_POW_255_D) throw new OverflowException();
            if (value >= 0.0)
            {
                UInt256 u = (UInt256)value;
                return new Int256(u.Upper, u.Lower);
            }
            UInt256 mag = (UInt256)(-value);
            return (Int256)(UInt256.Zero - mag);
        }

        /// <summary>Checked conversion from <see cref="decimal"/>.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator checked Int256(decimal value)
            => FromBigInteger(new BigInteger(decimal.Truncate(value)));

        /// <summary>Converts <see cref="Int256"/> to <see cref="float"/>.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(Int256 value) => (float)(double)value;

        /// <summary>Converts <see cref="Int256"/> to <see cref="double"/>.</summary>
        /// <param name="value">The input.</param>
        public static explicit operator double(Int256 value)
        {
            if (IsNegative(value))
            {
                UInt256 mag = (UInt256)(-value);
                return -(double)mag;
            }
            return (double)(UInt256)value;
        }

        /// <summary>Converts <see cref="Int256"/> to <see cref="decimal"/>.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">Value does not fit in <see cref="decimal"/>.</exception>
        public static explicit operator decimal(Int256 value)
        {
            bool neg = IsNegative(value);
            UInt256 mag = neg ? (UInt256)(-value) : (UInt256)value;
            if (mag._p3 != 0 || mag._p2 != 0 || mag._p1 > uint.MaxValue)
                throw new OverflowException("Value does not fit in decimal.");
            return new decimal(
                (int)mag._p0,
                (int)(mag._p0 >> 32),
                (int)mag._p1,
                isNegative: neg,
                scale: 0);
        }

        /// <summary>Narrowing conversion to <see cref="byte"/> (keeps low 8 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator byte(Int256 a) => (byte)a._p0;

        /// <summary>Narrowing conversion to <see cref="ushort"/> (keeps low 16 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ushort(Int256 a) => (ushort)a._p0;

        /// <summary>Narrowing conversion to <see cref="uint"/> (keeps low 32 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(Int256 a) => (uint)a._p0;

        /// <summary>Narrowing conversion to <see cref="ulong"/> (keeps low 64 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ulong(Int256 a) => a._p0;

        /// <summary>Narrowing conversion to <see cref="UInt128"/> (keeps low 128 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(Int256 a) => new(a._p1, a._p0);

        /// <summary>Narrowing conversion to <see cref="char"/> (keeps low 16 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator char(Int256 a) => (char)(ushort)a._p0;

        /// <summary>Checked narrowing to <see cref="byte"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside byte range.</exception>
        public static explicit operator checked byte(Int256 a)
        {
            if (IsNegative(a)) throw new OverflowException();
            if ((a._p1 | a._p2 | a._p3) != 0UL || a._p0 > byte.MaxValue) throw new OverflowException();
            return (byte)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="sbyte"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside sbyte range.</exception>
        public static explicit operator checked sbyte(Int256 a)
        {
            long v = checked((long)a);
            return checked((sbyte)v);
        }

        /// <summary>Checked narrowing to <see cref="ushort"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside ushort range.</exception>
        public static explicit operator checked ushort(Int256 a)
        {
            if (IsNegative(a) || (a._p1 | a._p2 | a._p3) != 0UL || a._p0 > ushort.MaxValue) throw new OverflowException();
            return (ushort)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="short"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside short range.</exception>
        public static explicit operator checked short(Int256 a)
        {
            long v = checked((long)a);
            return checked((short)v);
        }

        /// <summary>Checked narrowing to <see cref="uint"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside uint range.</exception>
        public static explicit operator checked uint(Int256 a)
        {
            if (IsNegative(a) || (a._p1 | a._p2 | a._p3) != 0UL || a._p0 > uint.MaxValue) throw new OverflowException();
            return (uint)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="int"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside int range.</exception>
        public static explicit operator checked int(Int256 a)
        {
            long v = checked((long)a);
            return checked((int)v);
        }

        /// <summary>Checked narrowing to <see cref="ulong"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside ulong range.</exception>
        public static explicit operator checked ulong(Int256 a)
        {
            if (IsNegative(a) || (a._p1 | a._p2 | a._p3) != 0UL) throw new OverflowException();
            return a._p0;
        }

        /// <summary>Checked narrowing to <see cref="long"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside long range.</exception>
        public static explicit operator checked long(Int256 a)
        {
            // Valid range: value fits in signed 64 bits. Upper 192 bits must be sign-extension of bit 63 of _p0.
            bool neg = IsNegative(a);
            ulong expected = neg ? ulong.MaxValue : 0UL;
            if (a._p1 != expected || a._p2 != expected || a._p3 != expected) throw new OverflowException();
            if (neg && (a._p0 & SIGN_BIT) == 0UL) throw new OverflowException();
            if (!neg && (a._p0 & SIGN_BIT) != 0UL) throw new OverflowException();
            return (long)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="UInt128"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside <see cref="UInt128"/> range.</exception>
        public static explicit operator checked UInt128(Int256 a)
        {
            if (IsNegative(a) || (a._p2 | a._p3) != 0UL) throw new OverflowException();
            return new UInt128(a._p1, a._p0);
        }

        /// <summary>Checked narrowing to <see cref="Int128"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside <see cref="Int128"/> range.</exception>
        public static explicit operator checked Int128(Int256 a)
        {
            // Valid range: low 128 bits form a valid signed Int128. Upper 128 bits must
            // be sign-extension of bit 127 (= top bit of _p1).
            bool neg = IsNegative(a);
            ulong expected = neg ? ulong.MaxValue : 0UL;
            if (a._p2 != expected || a._p3 != expected) throw new OverflowException();
            if (neg && (a._p1 & SIGN_BIT) == 0UL) throw new OverflowException();
            if (!neg && (a._p1 & SIGN_BIT) != 0UL) throw new OverflowException();
            return (Int128)new UInt128(a._p1, a._p0);
        }

        /// <summary>Checked narrowing to <see cref="char"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is outside char range.</exception>
        public static explicit operator checked char(Int256 a)
        {
            if (IsNegative(a) || (a._p1 | a._p2 | a._p3) != 0UL || a._p0 > char.MaxValue) throw new OverflowException();
            return (char)(ushort)a._p0;
        }

        #endregion

        #region I. Identity properties / generic-math scaffolding

        /// <summary>The additive identity (zero).</summary>
        public static Int256 AdditiveIdentity => Zero;

        /// <summary>The multiplicative identity (one).</summary>
        public static Int256 MultiplicativeIdentity => One;

        /// <summary>The radix (base) of this numeric type: 2.</summary>
        public static int Radix => 2;

#if NET8_0_OR_GREATER
        /// <summary>A value with every bit set (same as -1).</summary>
        public static Int256 AllBitsSet => NegativeOne;
#endif

        /// <summary>Creates an <see cref="Int256"/> from <paramref name="value"/>; throws on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        /// <exception cref="OverflowException">The source value is out of range.</exception>
        public static Int256 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromCheckedImpl(value, out Int256 result)) return result;
            if (TOther.TryConvertToChecked(value, out result)) return result;
            throw new NotSupportedException();
        }

        /// <summary>Creates an <see cref="Int256"/> from <paramref name="value"/>; saturates on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        public static Int256 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromSaturatingImpl(value, out Int256 result)) return result;
            if (TOther.TryConvertToSaturating(value, out result)) return result;
            throw new NotSupportedException();
        }

        /// <summary>Creates an <see cref="Int256"/> from <paramref name="value"/>; truncates on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        public static Int256 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromTruncatingImpl(value, out Int256 result)) return result;
            if (TOther.TryConvertToTruncating(value, out result)) return result;
            throw new NotSupportedException();
        }

        #endregion

        #region INumberBase<Int256>.TryConvert* (explicit interface impl)

        static bool INumberBase<Int256>.TryConvertFromChecked<TOther>(TOther value, out Int256 result) => TryConvertFromCheckedImpl(value, out result);
        static bool INumberBase<Int256>.TryConvertFromSaturating<TOther>(TOther value, out Int256 result) => TryConvertFromSaturatingImpl(value, out result);
        static bool INumberBase<Int256>.TryConvertFromTruncating<TOther>(TOther value, out Int256 result) => TryConvertFromTruncatingImpl(value, out result);
        static bool INumberBase<Int256>.TryConvertToChecked<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToCheckedImpl(value, out result);
        static bool INumberBase<Int256>.TryConvertToSaturating<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToSaturatingImpl(value, out result);
        static bool INumberBase<Int256>.TryConvertToTruncating<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToTruncatingImpl(value, out result);

        private static bool TryConvertFromCheckedImpl<TOther>(TOther value, out Int256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!; return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!; return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!; return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!; return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (Int256)(ulong)(object)value!; return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (Int256)(UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (Int256)(ulong)(nuint)(object)value!; return true; }

            if (typeof(TOther) == typeof(sbyte))   { result = (long)(sbyte)(object)value!; return true; }
            if (typeof(TOther) == typeof(short))   { result = (long)(short)(object)value!; return true; }
            if (typeof(TOther) == typeof(int))     { result = (long)(int)(object)value!; return true; }
            if (typeof(TOther) == typeof(long))    { result = (long)(object)value!; return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (Int128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nint))    { result = (long)(nint)(object)value!; return true; }

            if (typeof(TOther) == typeof(float))   { result = checked((Int256)(float)(object)value!); return true; }
            if (typeof(TOther) == typeof(double))  { result = checked((Int256)(double)(object)value!); return true; }
            if (typeof(TOther) == typeof(decimal)) { result = checked((Int256)(decimal)(object)value!); return true; }
            if (typeof(TOther) == typeof(Half))    { result = checked((Int256)(double)(Half)(object)value!); return true; }

            result = default;
            return false;
        }

        private static bool TryConvertFromSaturatingImpl<TOther>(TOther value, out Int256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!; return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!; return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!; return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!; return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (Int256)(ulong)(object)value!; return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (Int256)(UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (Int256)(ulong)(nuint)(object)value!; return true; }

            if (typeof(TOther) == typeof(sbyte))   { result = (long)(sbyte)(object)value!; return true; }
            if (typeof(TOther) == typeof(short))   { result = (long)(short)(object)value!; return true; }
            if (typeof(TOther) == typeof(int))     { result = (long)(int)(object)value!; return true; }
            if (typeof(TOther) == typeof(long))    { result = (long)(object)value!; return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (Int128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nint))    { result = (long)(nint)(object)value!; return true; }

            if (typeof(TOther) == typeof(float))
            {
                float v = (float)(object)value!;
                result = v >= (float)TWO_POW_255_D ? MaxValue : v < -(float)TWO_POW_255_D ? MinValue : (Int256)v;
                return true;
            }
            if (typeof(TOther) == typeof(double))
            {
                double v = (double)(object)value!;
                result = v >= TWO_POW_255_D ? MaxValue : v < -TWO_POW_255_D ? MinValue : (Int256)v;
                return true;
            }
            if (typeof(TOther) == typeof(decimal))
            {
                result = (Int256)(decimal)(object)value!;
                return true;
            }
            if (typeof(TOther) == typeof(Half))
            {
                double v = (double)(Half)(object)value!;
                result = v >= TWO_POW_255_D ? MaxValue : v < -TWO_POW_255_D ? MinValue : (Int256)v;
                return true;
            }

            result = default;
            return false;
        }

        private static bool TryConvertFromTruncatingImpl<TOther>(TOther value, out Int256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!; return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!; return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!; return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!; return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (Int256)(ulong)(object)value!; return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (Int256)(UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (Int256)(ulong)(nuint)(object)value!; return true; }

            if (typeof(TOther) == typeof(sbyte))   { result = (long)(sbyte)(object)value!; return true; }
            if (typeof(TOther) == typeof(short))   { result = (long)(short)(object)value!; return true; }
            if (typeof(TOther) == typeof(int))     { result = (long)(int)(object)value!; return true; }
            if (typeof(TOther) == typeof(long))    { result = (long)(object)value!; return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (Int128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nint))    { result = (long)(nint)(object)value!; return true; }

            if (typeof(TOther) == typeof(float))   { result = (Int256)(float)(object)value!; return true; }
            if (typeof(TOther) == typeof(double))  { result = (Int256)(double)(object)value!; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (Int256)(decimal)(object)value!; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (Int256)(double)(Half)(object)value!; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToCheckedImpl<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { byte   r = checked((byte)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(char))    { char   r = checked((char)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ushort))  { ushort r = checked((ushort)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(uint))    { uint   r = checked((uint)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ulong))   { ulong  r = checked((ulong)value);  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(UInt128)) { UInt128 r = checked((UInt128)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nuint))   { nuint r = checked((nuint)checked((ulong)value)); result = (TOther)(object)r; return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte r = checked((sbyte)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(short))   { short r = checked((short)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(int))     { int   r = checked((int)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(long))    { long  r = checked((long)value);  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 r = checked((Int128)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nint))    { nint r = checked((nint)checked((long)value)); result = (TOther)(object)r; return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToSaturatingImpl<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { byte r = IsNegative(value) ? (byte)0 : value > new Int256(0, byte.MaxValue) ? byte.MaxValue : (byte)value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(char))    { char r = IsNegative(value) ? (char)0 : value > new Int256(0, char.MaxValue) ? char.MaxValue : (char)(ushort)value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ushort))  { ushort r = IsNegative(value) ? (ushort)0 : value > new Int256(0, ushort.MaxValue) ? ushort.MaxValue : (ushort)value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(uint))    { uint r = IsNegative(value) ? 0u : value > new Int256(0, uint.MaxValue) ? uint.MaxValue : (uint)value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ulong))   { ulong r = IsNegative(value) ? 0UL : value > new Int256(0, ulong.MaxValue) ? ulong.MaxValue : value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(UInt128)) { UInt128 r = IsNegative(value) ? UInt128.Zero : (value._p2 | value._p3) != 0UL ? UInt128.MaxValue : new UInt128(value._p1, value._p0); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nuint))   { ulong r = IsNegative(value) ? 0UL : value > new Int256(0, ulong.MaxValue) ? ulong.MaxValue : value._p0; nuint n = r > nuint.MaxValue ? nuint.MaxValue : (nuint)r; result = (TOther)(object)n; return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte r = value < new Int256((long)sbyte.MinValue) ? sbyte.MinValue : value > new Int256(sbyte.MaxValue) ? sbyte.MaxValue : (sbyte)(long)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(short))   { short r = value < new Int256((long)short.MinValue) ? short.MinValue : value > new Int256(short.MaxValue) ? short.MaxValue : (short)(long)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(int))     { int r = value < new Int256(int.MinValue) ? int.MinValue : value > new Int256(int.MaxValue) ? int.MaxValue : (int)(long)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(long))    { long r = value < new Int256(long.MinValue) ? long.MinValue : value > new Int256(long.MaxValue) ? long.MaxValue : (long)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 r = value < (Int256)Int128.MinValue ? Int128.MinValue : value > (Int256)Int128.MaxValue ? Int128.MaxValue : (Int128)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nint))    { long r = value < new Int256(long.MinValue) ? long.MinValue : value > new Int256(long.MaxValue) ? long.MaxValue : (long)value; nint n = r < nint.MinValue ? nint.MinValue : r > nint.MaxValue ? nint.MaxValue : (nint)r; result = (TOther)(object)n; return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToTruncatingImpl<TOther>(Int256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (TOther)(object)(byte)value._p0; return true; }
            if (typeof(TOther) == typeof(char))    { result = (TOther)(object)(char)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (TOther)(object)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(uint))    { result = (TOther)(object)(uint)value._p0; return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (TOther)(object)value._p0; return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (TOther)(object)new UInt128(value._p1, value._p0); return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (TOther)(object)(nuint)value._p0; return true; }

            if (typeof(TOther) == typeof(sbyte))   { result = (TOther)(object)(sbyte)(byte)value._p0; return true; }
            if (typeof(TOther) == typeof(short))   { result = (TOther)(object)(short)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(int))     { result = (TOther)(object)(int)(uint)value._p0; return true; }
            if (typeof(TOther) == typeof(long))    { result = (TOther)(object)(long)value._p0; return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (TOther)(object)(Int128)new UInt128(value._p1, value._p0); return true; }
            if (typeof(TOther) == typeof(nint))    { result = (TOther)(object)(nint)(long)value._p0; return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        #endregion
    }
}
