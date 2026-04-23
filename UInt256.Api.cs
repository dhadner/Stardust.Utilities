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
    /// Extended API surface for <see cref="UInt256"/> that mirrors the BCL
    /// <see cref="UInt128"/> type: static math helpers, bit-level operations,
    /// endian read/write, checked operators, float / decimal conversions, and
    /// the full <see cref="INumber{TSelf}"/> / <see cref="IBinaryInteger{TSelf}"/>
    /// generic-math interface set.
    /// </summary>
    public readonly partial struct UInt256 :
        INumber<UInt256>,
        IBinaryInteger<UInt256>,
        IBinaryNumber<UInt256>,
        IMinMaxValue<UInt256>,
        IUnsignedNumber<UInt256>
#if NET8_0_OR_GREATER
        ,
        IUtf8SpanFormattable,
        IUtf8SpanParsable<UInt256>
#endif
    {
        #region A. Static math helpers

        /// <summary>Returns the absolute value (no-op for unsigned).</summary>
        /// <param name="value">The value.</param>
        /// <returns>The input unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 Abs(UInt256 value) => value;

        /// <summary>Clamps a value to an inclusive range.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">Inclusive lower bound.</param>
        /// <param name="max">Inclusive upper bound.</param>
        /// <returns>The clamped value.</returns>
        /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        public static UInt256 Clamp(UInt256 value, UInt256 min, UInt256 max)
        {
            if (min > max) throw new ArgumentException("min must be <= max", nameof(min));
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>Returns <paramref name="value"/> (no-op; unsigned has no sign to copy).</summary>
        /// <param name="value">The value whose magnitude is preserved.</param>
        /// <param name="sign">Ignored for unsigned types.</param>
        /// <returns><paramref name="value"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 CopySign(UInt256 value, UInt256 sign) => value;

        /// <summary>Computes the quotient and remainder of two values.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>A tuple of (Quotient, Remainder).</returns>
        /// <exception cref="DivideByZeroException"><paramref name="right"/> is zero.</exception>
        public static (UInt256 Quotient, UInt256 Remainder) DivRem(UInt256 left, UInt256 right)
        {
            if (right._p0 == 0 && right._p1 == 0 && right._p2 == 0 && right._p3 == 0)
                throw new DivideByZeroException();
            if (left._p2 == 0 && left._p3 == 0 && right._p2 == 0 && right._p3 == 0)
            {
                UInt128 ll = new(left._p1, left._p0);
                UInt128 rr = new(right._p1, right._p0);
                UInt128 qq = ll / rr;
                UInt128 rm = ll - qq * rr;
                return (new UInt256(UInt128.Zero, qq), new UInt256(UInt128.Zero, rm));
            }
            UInt256 q = UInt256Math.DivRem(left, right, out UInt256 r);
            return (q, r);
        }

        /// <summary>Returns the greater of two values.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The greater of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 Max(UInt256 x, UInt256 y) => x >= y ? x : y;

        /// <summary>Returns the lesser of two values.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The lesser of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 Min(UInt256 x, UInt256 y) => x <= y ? x : y;

        /// <summary>Returns the value with the greater magnitude (same as <see cref="Max"/> for unsigned).</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The value with greater magnitude.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 MaxMagnitude(UInt256 x, UInt256 y) => Max(x, y);

        /// <summary>Returns the value with the lesser magnitude (same as <see cref="Min"/> for unsigned).</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>The value with lesser magnitude.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 MinMagnitude(UInt256 x, UInt256 y) => Min(x, y);

        /// <summary>Returns the sign of a value: 0 if zero, 1 otherwise.</summary>
        /// <param name="value">The value.</param>
        /// <returns>0 if <paramref name="value"/> is zero, otherwise 1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(UInt256 value) => (value._p0 | value._p1 | value._p2 | value._p3) == 0UL ? 0 : 1;

        /// <summary>INumberBase-required: alias for <see cref="MaxMagnitude"/>.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>See <see cref="MaxMagnitude"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 MaxMagnitudeNumber(UInt256 x, UInt256 y) => Max(x, y);

        /// <summary>INumberBase-required: alias for <see cref="MinMagnitude"/>.</summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <returns>See <see cref="MinMagnitude"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 MinMagnitudeNumber(UInt256 x, UInt256 y) => Min(x, y);

        #endregion

        #region B. Bit-level operations

        /// <summary>Counts the number of leading zero bits (0..256).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of leading zero bits as a <see cref="UInt256"/>.</returns>
        public static UInt256 LeadingZeroCount(UInt256 value)
        {
            if (value._p3 != 0) return (ulong)BitOperations.LeadingZeroCount(value._p3);
            if (value._p2 != 0) return (ulong)(64 + BitOperations.LeadingZeroCount(value._p2));
            if (value._p1 != 0) return (ulong)(128 + BitOperations.LeadingZeroCount(value._p1));
            return (ulong)(192 + BitOperations.LeadingZeroCount(value._p0));
        }

        /// <summary>Counts the number of trailing zero bits (0..256).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of trailing zero bits as a <see cref="UInt256"/>.</returns>
        public static UInt256 TrailingZeroCount(UInt256 value)
        {
            if (value._p0 != 0) return (ulong)BitOperations.TrailingZeroCount(value._p0);
            if (value._p1 != 0) return (ulong)(64 + BitOperations.TrailingZeroCount(value._p1));
            if (value._p2 != 0) return (ulong)(128 + BitOperations.TrailingZeroCount(value._p2));
            return (ulong)(192 + BitOperations.TrailingZeroCount(value._p3));
        }

        /// <summary>Counts the number of set bits (population count).</summary>
        /// <param name="value">The value.</param>
        /// <returns>Number of set bits as a <see cref="UInt256"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 PopCount(UInt256 value) =>
            (ulong)(BitOperations.PopCount(value._p0)
                  + BitOperations.PopCount(value._p1)
                  + BitOperations.PopCount(value._p2)
                  + BitOperations.PopCount(value._p3));

        /// <summary>Computes the integer (floor) base-2 logarithm.</summary>
        /// <param name="value">The value. For zero, returns zero (matching BCL).</param>
        /// <returns>Floor(log2(value)) as a <see cref="UInt256"/>.</returns>
        public static UInt256 Log2(UInt256 value)
        {
            if (value._p3 != 0) return (ulong)(192 + BitOperations.Log2(value._p3));
            if (value._p2 != 0) return (ulong)(128 + BitOperations.Log2(value._p2));
            if (value._p1 != 0) return (ulong)(64 + BitOperations.Log2(value._p1));
            return (ulong)BitOperations.Log2(value._p0);
        }

        /// <summary>Rotates the value left by the specified number of bits (mod 256).</summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="rotateAmount">Rotate amount (interpreted mod 256).</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 RotateLeft(UInt256 value, int rotateAmount)
        {
            int r = rotateAmount & 255;
            if (r == 0) return value;
            return (value << r) | (value >>> (256 - r));
        }

        /// <summary>Rotates the value right by the specified number of bits (mod 256).</summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="rotateAmount">Rotate amount (interpreted mod 256).</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 RotateRight(UInt256 value, int rotateAmount)
        {
            int r = rotateAmount & 255;
            if (r == 0) return value;
            return (value >>> r) | (value << (256 - r));
        }

        /// <summary>Returns the fixed byte size of the type: 32.</summary>
        /// <returns>Always 32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByteCount() => 32;

        /// <summary>Returns the number of bits needed to represent the current value (0..256).</summary>
        /// <returns>Significant bit length.</returns>
        public int GetShortestBitLength()
        {
            if (_p3 != 0) return 256 - BitOperations.LeadingZeroCount(_p3);
            if (_p2 != 0) return 192 - BitOperations.LeadingZeroCount(_p2);
            if (_p1 != 0) return 128 - BitOperations.LeadingZeroCount(_p1);
            if (_p0 != 0) return 64 - BitOperations.LeadingZeroCount(_p0);
            return 0;
        }

        #endregion

        #region C. Is-predicates

        /// <summary>Determines whether the value is zero.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if zero, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(UInt256 value) => (value._p0 | value._p1 | value._p2 | value._p3) == 0UL;

        /// <summary>Determines whether the value is an even integer.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if even.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEvenInteger(UInt256 value) => (value._p0 & 1UL) == 0UL;

        /// <summary>Determines whether the value is an odd integer.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if odd.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOddInteger(UInt256 value) => (value._p0 & 1UL) != 0UL;

        /// <summary>Determines whether the value is a power of two.</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is a non-zero power of two.</returns>
        public static bool IsPow2(UInt256 value)
        {
            if (IsZero(value)) return false;
            int popcount = BitOperations.PopCount(value._p0)
                         + BitOperations.PopCount(value._p1)
                         + BitOperations.PopCount(value._p2)
                         + BitOperations.PopCount(value._p3);
            return popcount == 1;
        }

        /// <summary>Always returns <c>false</c> for unsigned types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(UInt256 value) => false;

        /// <summary>Always returns <c>true</c> for unsigned types (matches BCL UInt128 semantics).</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(UInt256 value) => true;

        /// <summary>Determines whether the value is a canonical representation (always true for fixed-width integers).</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanonical(UInt256 value) => true;

        /// <summary>Always returns <c>false</c> for real integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComplexNumber(UInt256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(UInt256 value) => true;

        /// <summary>Always returns <c>false</c> for real integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImaginaryNumber(UInt256 value) => false;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(UInt256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(UInt256 value) => true;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(UInt256 value) => false;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(UInt256 value) => false;

        /// <summary>Returns <c>true</c> when the value is non-zero (integer types have no subnormal range).</summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if non-zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(UInt256 value) => !IsZero(value);

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(UInt256 value) => false;

        /// <summary>Always returns <c>true</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(UInt256 value) => true;

        /// <summary>Always returns <c>false</c> for integer types.</summary>
        /// <param name="value">Ignored.</param>
        /// <returns>Always <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubnormal(UInt256 value) => false;

        #endregion

        #region D. Endian Read/Write

        /// <summary>
        /// Reads a <see cref="UInt256"/> value from a big-endian byte sequence.
        /// </summary>
        /// <param name="source">The source bytes in big-endian order.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source"/> represents an unsigned value;
        /// <c>false</c> if it represents a signed value (in which case the high bit must be zero).</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="OverflowException">The source exceeds 256 bits or has a set sign bit when <paramref name="isUnsigned"/> is false.</exception>
        public static UInt256 ReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned)
        {
            if (!TryReadBigEndian(source, isUnsigned, out UInt256 value))
                throw new OverflowException("Value too large for UInt256.");
            return value;
        }

        /// <summary>
        /// Reads a <see cref="UInt256"/> value from a little-endian byte sequence.
        /// </summary>
        /// <param name="source">The source bytes in little-endian order.</param>
        /// <param name="isUnsigned"><c>true</c> if <paramref name="source"/> represents an unsigned value.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="OverflowException">The source exceeds 256 bits or has a set sign bit when <paramref name="isUnsigned"/> is false.</exception>
        public static UInt256 ReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned)
        {
            if (!TryReadLittleEndian(source, isUnsigned, out UInt256 value))
                throw new OverflowException("Value too large for UInt256.");
            return value;
        }

        /// <summary>Attempts to read a <see cref="UInt256"/> from a big-endian byte sequence.</summary>
        /// <param name="source">The source bytes.</param>
        /// <param name="isUnsigned">Whether the source is unsigned.</param>
        /// <param name="value">The parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out UInt256 value)
        {
            if (source.IsEmpty) { value = default; return true; }
            int len = source.Length;
            if (len > 32)
            {
                int excess = len - 32;
                for (int i = 0; i < excess; i++)
                    if (source[i] != 0) { value = default; return false; }
                source = source[excess..];
                len = 32;
            }
            if (!isUnsigned && (source[0] & 0x80) != 0) { value = default; return false; }
            Span<byte> buf = stackalloc byte[32];
            source.CopyTo(buf[(32 - len)..]);
            ulong p3 = BinaryPrimitives.ReadUInt64BigEndian(buf[..8]);
            ulong p2 = BinaryPrimitives.ReadUInt64BigEndian(buf.Slice(8, 8));
            ulong p1 = BinaryPrimitives.ReadUInt64BigEndian(buf.Slice(16, 8));
            ulong p0 = BinaryPrimitives.ReadUInt64BigEndian(buf.Slice(24, 8));
            value = new UInt256(p3, p2, p1, p0);
            return true;
        }

        /// <summary>Attempts to read a <see cref="UInt256"/> from a little-endian byte sequence.</summary>
        /// <param name="source">The source bytes.</param>
        /// <param name="isUnsigned">Whether the source is unsigned.</param>
        /// <param name="value">The parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out UInt256 value)
        {
            if (source.IsEmpty) { value = default; return true; }
            int len = source.Length;
            if (len > 32)
            {
                for (int i = 32; i < len; i++)
                    if (source[i] != 0) { value = default; return false; }
                source = source[..32];
                len = 32;
            }
            if (!isUnsigned && (source[len - 1] & 0x80) != 0) { value = default; return false; }
            Span<byte> buf = stackalloc byte[32];
            source.CopyTo(buf);
            ulong p0 = BinaryPrimitives.ReadUInt64LittleEndian(buf[..8]);
            ulong p1 = BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(8, 8));
            ulong p2 = BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(16, 8));
            ulong p3 = BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(24, 8));
            value = new UInt256(p3, p2, p1, p0);
            return true;
        }

        /// <summary>Attempts to write this value in big-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <param name="bytesWritten">Bytes written on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < 32) { bytesWritten = 0; return false; }
            BinaryPrimitives.WriteUInt64BigEndian(destination[..8], _p3);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8, 8), _p2);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(16, 8), _p1);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(24, 8), _p0);
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
            BinaryPrimitives.WriteUInt64LittleEndian(destination[..8], _p0);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8, 8), _p1);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(16, 8), _p2);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(24, 8), _p3);
            bytesWritten = 32;
            return true;
        }

        /// <summary>Writes this value in big-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <returns>Bytes written (always 32 on success).</returns>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public int WriteBigEndian(Span<byte> destination)
        {
            if (!TryWriteBigEndian(destination, out int n))
                throw new ArgumentException("Destination too short.", nameof(destination));
            return n;
        }

        /// <summary>Writes this value in little-endian order to a destination span.</summary>
        /// <param name="destination">The destination span (must be at least 32 bytes).</param>
        /// <returns>Bytes written (always 32 on success).</returns>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        public int WriteLittleEndian(Span<byte> destination)
        {
            if (!TryWriteLittleEndian(destination, out int n))
                throw new ArgumentException("Destination too short.", nameof(destination));
            return n;
        }

        #endregion

        #region E. Checked operators

        /// <summary>Checked addition; throws on overflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The sum.</returns>
        /// <exception cref="OverflowException">The result does not fit in 256 bits.</exception>
        public static UInt256 operator checked +(UInt256 a, UInt256 b)
        {
            UInt256 r = a + b;
            if (r < a) throw new OverflowException();
            return r;
        }

        /// <summary>Checked subtraction; throws on underflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The difference.</returns>
        /// <exception cref="OverflowException"><paramref name="b"/> is greater than <paramref name="a"/>.</exception>
        public static UInt256 operator checked -(UInt256 a, UInt256 b)
        {
            if (a < b) throw new OverflowException();
            return a - b;
        }

        /// <summary>Checked unary negation; throws unless <paramref name="a"/> is zero.</summary>
        /// <param name="a">The operand.</param>
        /// <returns>Zero when <paramref name="a"/> is zero.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is not zero.</exception>
        public static UInt256 operator checked -(UInt256 a)
        {
            if ((a._p0 | a._p1 | a._p2 | a._p3) != 0UL) throw new OverflowException();
            return Zero;
        }

        /// <summary>Checked multiplication; throws on overflow.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The product.</returns>
        /// <exception cref="OverflowException">The result does not fit in 256 bits.</exception>
        public static UInt256 operator checked *(UInt256 a, UInt256 b)
        {
            if (IsZero(a) || IsZero(b)) return Zero;
            UInt256 r = a * b;
            if (r / b != a) throw new OverflowException();
            return r;
        }

        /// <summary>Checked division; same as unchecked (division cannot overflow for unsigned).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The quotient.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator checked /(UInt256 a, UInt256 b) => a / b;

        /// <summary>Checked increment; throws on overflow.</summary>
        /// <param name="a">The value to increment.</param>
        /// <returns>The incremented value.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is <see cref="MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator checked ++(UInt256 a) => checked(a + One);

        /// <summary>Checked decrement; throws on underflow.</summary>
        /// <param name="a">The value to decrement.</param>
        /// <returns>The decremented value.</returns>
        /// <exception cref="OverflowException"><paramref name="a"/> is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator checked --(UInt256 a) => checked(a - One);

        #endregion

        #region F. Additional parsing

        /// <summary>Parses using the specified <paramref name="style"/> and culture.</summary>
        /// <param name="s">The input string.</param>
        /// <param name="style">Number style to apply.</param>
        /// <param name="provider">Culture provider (used only for culture-sensitive formats).</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        public static UInt256 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>Parses a span using the specified <paramref name="style"/> and culture.</summary>
        /// <param name="s">The input span.</param>
        /// <param name="style">Number style to apply.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt256 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            // Fast path for plain integer / hex: delegate to native routines.
            if (style == NumberStyles.Integer || style == NumberStyles.None)
                return Parse(s.ToString(), NumberStyles.Integer);
            if ((style & NumberStyles.HexNumber) == NumberStyles.HexNumber)
                return Parse(s.ToString(), NumberStyles.HexNumber);
            BigInteger big = BigInteger.Parse(s.ToString(), style, provider ?? CultureInfo.InvariantCulture);
            if (big.Sign < 0) throw new OverflowException("UInt256 cannot represent negative values.");
            if (big.GetByteCount(isUnsigned: true) > 32) throw new OverflowException("Value exceeds 256 bits.");
            return FromBigInteger(big);
        }

        /// <summary>Attempts to parse a string using the specified style and culture.</summary>
        /// <param name="s">The input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UInt256 result)
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
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UInt256 result)
        {
            try { result = Parse(s, style, provider); return true; }
            catch { result = default; return false; }
        }

        #endregion

        #region G. UTF-8 parsing/formatting

        /// <summary>Formats this value into a UTF-8 destination span.</summary>
        /// <param name="utf8Destination">The destination span.</param>
        /// <param name="bytesWritten">Bytes written on success.</param>
        /// <param name="format">Format string (e.g., "G", "X", "D10").</param>
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
        public static UInt256 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
            => Parse(Encoding.UTF8.GetString(utf8Text), NumberStyles.Integer, provider);

        /// <summary>Parses a value from a UTF-8 byte sequence with the given number style.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <returns>The parsed value.</returns>
        public static UInt256 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
            => Parse(Encoding.UTF8.GetString(utf8Text), style, provider);

        /// <summary>Attempts to parse a value from a UTF-8 byte sequence.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out UInt256 result)
            => TryParse(Encoding.UTF8.GetString(utf8Text), NumberStyles.Integer, provider, out result);

        /// <summary>Attempts to parse a value from a UTF-8 byte sequence.</summary>
        /// <param name="utf8Text">The UTF-8 input.</param>
        /// <param name="style">Number style.</param>
        /// <param name="provider">Culture provider.</param>
        /// <param name="result">Parsed value on success.</param>
        /// <returns><c>true</c> on success.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out UInt256 result)
            => TryParse(Encoding.UTF8.GetString(utf8Text), style, provider, out result);
#endif

        #endregion

        #region H. Additional conversions

        private const double TWO_POW_64_D  = 1.8446744073709552e+19;
        private const double TWO_POW_128_D = 3.4028236692093846e+38;
        private const double TWO_POW_192_D = 6.277101735386680e+57;
        private const double TWO_POW_256_D = 1.1579208923731619e+77;

        /// <summary>Implicitly widens a <see cref="char"/> to a <see cref="UInt256"/>.</summary>
        /// <param name="a">The input character.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(char a) => new(UInt128.Zero, (ulong)a);

        /// <summary>Explicit conversion from a signed <see cref="sbyte"/> (two's complement reinterpret).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(sbyte a) => new(UInt128.Zero, (ulong)(long)a);

        /// <summary>Explicit conversion from a signed <see cref="short"/> (two's complement reinterpret).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(short a) => new(UInt128.Zero, (ulong)(long)a);

        /// <summary>Explicit conversion from a signed <see cref="int"/> (two's complement reinterpret).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(int a) => new(UInt128.Zero, (ulong)(long)a);

        /// <summary>Explicit conversion from a signed <see cref="long"/> (two's complement reinterpret).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(long a) => new(UInt128.Zero, (ulong)a);

        /// <summary>Explicit conversion from a signed <see cref="Int128"/> (two's complement reinterpret).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(Int128 a)
        {
            UInt128 lo = (UInt128)a;
            UInt128 hi = Int128.IsNegative(a) ? ~UInt128.Zero : UInt128.Zero;
            return new UInt256(hi, lo);
        }

        /// <summary>Checked conversion from <see cref="sbyte"/>; throws if negative.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is negative.</exception>
        public static explicit operator checked UInt256(sbyte a)
        {
            if (a < 0) throw new OverflowException();
            return new UInt256(UInt128.Zero, (ulong)a);
        }

        /// <summary>Checked conversion from <see cref="short"/>; throws if negative.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is negative.</exception>
        public static explicit operator checked UInt256(short a)
        {
            if (a < 0) throw new OverflowException();
            return new UInt256(UInt128.Zero, (ulong)a);
        }

        /// <summary>Checked conversion from <see cref="int"/>; throws if negative.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is negative.</exception>
        public static explicit operator checked UInt256(int a)
        {
            if (a < 0) throw new OverflowException();
            return new UInt256(UInt128.Zero, (ulong)a);
        }

        /// <summary>Checked conversion from <see cref="long"/>; throws if negative.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is negative.</exception>
        public static explicit operator checked UInt256(long a)
        {
            if (a < 0) throw new OverflowException();
            return new UInt256(UInt128.Zero, (ulong)a);
        }

        /// <summary>Checked conversion from <see cref="Int128"/>; throws if negative.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value is negative.</exception>
        public static explicit operator checked UInt256(Int128 a)
        {
            if (Int128.IsNegative(a)) throw new OverflowException();
            return new UInt256(UInt128.Zero, (UInt128)a);
        }

        /// <summary>Converts a <see cref="float"/> to <see cref="UInt256"/>; truncates toward zero.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(float value) => (UInt256)(double)value;

        /// <summary>Converts a <see cref="double"/> to <see cref="UInt256"/>; truncates toward zero. Out-of-range inputs wrap.</summary>
        /// <param name="value">The input.</param>
        public static explicit operator UInt256(double value)
        {
            if (double.IsNaN(value) || value <= 0.0) return Zero;
            if (value >= TWO_POW_256_D) return Zero;   // BCL: positive overflow wraps to 0 for unsigned.
            return FromDoubleUnchecked(value);
        }

        /// <summary>Converts a <see cref="decimal"/> to <see cref="UInt256"/>; truncates toward zero.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(decimal value)
        {
            if (value < 0m) return Zero;
            return FromBigInteger(new BigInteger(decimal.Truncate(value)));
        }

        /// <summary>Checked conversion from <see cref="float"/>; throws on NaN, Infinity, negative, or overflow.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">The value is outside the valid range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator checked UInt256(float value) => checked((UInt256)(double)value);

        /// <summary>Checked conversion from <see cref="double"/>; throws on NaN, Infinity, negative, or overflow.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">The value is outside the valid range.</exception>
        public static explicit operator checked UInt256(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException();
            if (value < 0.0 || value >= TWO_POW_256_D) throw new OverflowException();
            return FromDoubleUnchecked(value);
        }

        /// <summary>Checked conversion from <see cref="decimal"/>; throws on negative values.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">The value is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator checked UInt256(decimal value)
        {
            if (value < 0m) throw new OverflowException();
            return FromBigInteger(new BigInteger(decimal.Truncate(value)));
        }

        private static UInt256 FromDoubleUnchecked(double value)
        {
            if (value < 1.0) return Zero;
            ulong p3 = 0, p2 = 0, p1 = 0;
            if (value >= TWO_POW_192_D)
            {
                p3 = (ulong)(value / TWO_POW_192_D);
                value -= p3 * TWO_POW_192_D;
                if (value < 0) { p3--; value += TWO_POW_192_D; }
            }
            if (value >= TWO_POW_128_D)
            {
                p2 = (ulong)(value / TWO_POW_128_D);
                value -= p2 * TWO_POW_128_D;
                if (value < 0) { p2--; value += TWO_POW_128_D; }
            }
            if (value >= TWO_POW_64_D)
            {
                p1 = (ulong)(value / TWO_POW_64_D);
                value -= p1 * TWO_POW_64_D;
                if (value < 0) { p1--; value += TWO_POW_64_D; }
            }
            ulong p0 = (ulong)value;
            return new UInt256(p3, p2, p1, p0);
        }

        /// <summary>Converts <see cref="UInt256"/> to <see cref="float"/>.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(UInt256 value) => (float)(double)value;

        /// <summary>Converts <see cref="UInt256"/> to <see cref="double"/>.</summary>
        /// <param name="value">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(UInt256 value)
            => (double)value._p0
             + (double)value._p1 * TWO_POW_64_D
             + (double)value._p2 * TWO_POW_128_D
             + (double)value._p3 * TWO_POW_192_D;

        /// <summary>Converts <see cref="UInt256"/> to <see cref="decimal"/>; throws when value exceeds <see cref="decimal.MaxValue"/>.</summary>
        /// <param name="value">The input.</param>
        /// <exception cref="OverflowException">Value does not fit in <see cref="decimal"/>.</exception>
        public static explicit operator decimal(UInt256 value)
        {
            if (value._p3 != 0 || value._p2 != 0 || value._p1 > uint.MaxValue)
                throw new OverflowException("Value does not fit in decimal.");
            return new decimal(
                (int)value._p0,
                (int)(value._p0 >> 32),
                (int)value._p1,
                isNegative: false,
                scale: 0);
        }

        /// <summary>Narrowing conversion to <see cref="sbyte"/> (keeps low 8 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator sbyte(UInt256 a) => (sbyte)(byte)a._p0;

        /// <summary>Narrowing conversion to <see cref="short"/> (keeps low 16 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator short(UInt256 a) => (short)(ushort)a._p0;

        /// <summary>Narrowing conversion to <see cref="int"/> (keeps low 32 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(UInt256 a) => (int)(uint)a._p0;

        /// <summary>Narrowing conversion to <see cref="long"/> (keeps low 64 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(UInt256 a) => (long)a._p0;

        /// <summary>Narrowing conversion to <see cref="Int128"/> (keeps low 128 bits).</summary>
        /// <param name="a">The input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128(UInt256 a) => (Int128)new UInt128(a._p1, a._p0);

        /// <summary>Checked narrowing to <see cref="byte"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="byte.MaxValue"/>.</exception>
        public static explicit operator checked byte(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > byte.MaxValue) throw new OverflowException();
            return (byte)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="sbyte"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="sbyte.MaxValue"/>.</exception>
        public static explicit operator checked sbyte(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > (ulong)sbyte.MaxValue) throw new OverflowException();
            return (sbyte)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="ushort"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="ushort.MaxValue"/>.</exception>
        public static explicit operator checked ushort(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > ushort.MaxValue) throw new OverflowException();
            return (ushort)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="short"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="short.MaxValue"/>.</exception>
        public static explicit operator checked short(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > (ulong)short.MaxValue) throw new OverflowException();
            return (short)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="uint"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="uint.MaxValue"/>.</exception>
        public static explicit operator checked uint(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > uint.MaxValue) throw new OverflowException();
            return (uint)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="int"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="int.MaxValue"/>.</exception>
        public static explicit operator checked int(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > int.MaxValue) throw new OverflowException();
            return (int)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="ulong"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="ulong.MaxValue"/>.</exception>
        public static explicit operator checked ulong(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0) throw new OverflowException();
            return a._p0;
        }

        /// <summary>Checked narrowing to <see cref="long"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="long.MaxValue"/>.</exception>
        public static explicit operator checked long(UInt256 a)
        {
            if (a._p1 != 0 || a._p2 != 0 || a._p3 != 0 || a._p0 > long.MaxValue) throw new OverflowException();
            return (long)a._p0;
        }

        /// <summary>Checked narrowing to <see cref="UInt128"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="UInt128.MaxValue"/>.</exception>
        public static explicit operator checked UInt128(UInt256 a)
        {
            if (a._p2 != 0 || a._p3 != 0) throw new OverflowException();
            return new UInt128(a._p1, a._p0);
        }

        /// <summary>Checked narrowing to <see cref="Int128"/>.</summary>
        /// <param name="a">The input.</param>
        /// <exception cref="OverflowException">Value exceeds <see cref="Int128.MaxValue"/>.</exception>
        public static explicit operator checked Int128(UInt256 a)
        {
            if (a._p2 != 0 || a._p3 != 0 || (a._p1 & 0x8000_0000_0000_0000UL) != 0UL) throw new OverflowException();
            return (Int128)new UInt128(a._p1, a._p0);
        }

        #endregion

        #region I. Identity properties / generic-math scaffolding

        /// <summary>The additive identity (zero).</summary>
        public static UInt256 AdditiveIdentity => Zero;

        /// <summary>The multiplicative identity (one).</summary>
        public static UInt256 MultiplicativeIdentity => One;

        /// <summary>The radix (base) of this numeric type: 2.</summary>
        public static int Radix => 2;

#if NET8_0_OR_GREATER
        /// <summary>A value with every bit set (same as <see cref="MaxValue"/>).</summary>
        public static UInt256 AllBitsSet => MaxValue;
#endif

        /// <summary>Creates a <see cref="UInt256"/> from <paramref name="value"/>; throws on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        /// <exception cref="OverflowException">The source value is out of range.</exception>
        public static UInt256 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromCheckedImpl(value, out UInt256 result)) return result;
            if (TOther.TryConvertToChecked(value, out result)) return result;
            throw new NotSupportedException();
        }

        /// <summary>Creates a <see cref="UInt256"/> from <paramref name="value"/>; saturates on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        public static UInt256 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromSaturatingImpl(value, out UInt256 result)) return result;
            if (TOther.TryConvertToSaturating(value, out result)) return result;
            throw new NotSupportedException();
        }

        /// <summary>Creates a <see cref="UInt256"/> from <paramref name="value"/>; truncates on overflow.</summary>
        /// <typeparam name="TOther">Source numeric type.</typeparam>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NotSupportedException">The source type is not supported.</exception>
        public static UInt256 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
        {
            if (TryConvertFromTruncatingImpl(value, out UInt256 result)) return result;
            if (TOther.TryConvertToTruncating(value, out result)) return result;
            throw new NotSupportedException();
        }

        #endregion

        #region INumberBase<UInt256>.TryConvert* (explicit interface impl)

        static bool INumberBase<UInt256>.TryConvertFromChecked<TOther>(TOther value, out UInt256 result) => TryConvertFromCheckedImpl(value, out result);
        static bool INumberBase<UInt256>.TryConvertFromSaturating<TOther>(TOther value, out UInt256 result) => TryConvertFromSaturatingImpl(value, out result);
        static bool INumberBase<UInt256>.TryConvertFromTruncating<TOther>(TOther value, out UInt256 result) => TryConvertFromTruncatingImpl(value, out result);
        static bool INumberBase<UInt256>.TryConvertToChecked<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToCheckedImpl(value, out result);
        static bool INumberBase<UInt256>.TryConvertToSaturating<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToSaturatingImpl(value, out result);
        static bool INumberBase<UInt256>.TryConvertToTruncating<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) => TryConvertToTruncatingImpl(value, out result);

        private static bool TryConvertFromCheckedImpl<TOther>(TOther value, out UInt256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!;    return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!;  return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (ulong)(object)value!;   return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (nuint)(object)value!;   return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte v = (sbyte)(object)value!; if (v < 0) throw new OverflowException(); result = (ulong)v; return true; }
            if (typeof(TOther) == typeof(short))   { short v = (short)(object)value!; if (v < 0) throw new OverflowException(); result = (ulong)v; return true; }
            if (typeof(TOther) == typeof(int))     { int   v = (int)(object)value!;   if (v < 0) throw new OverflowException(); result = (ulong)v; return true; }
            if (typeof(TOther) == typeof(long))    { long  v = (long)(object)value!;  if (v < 0) throw new OverflowException(); result = (ulong)v; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 v = (Int128)(object)value!; if (Int128.IsNegative(v)) throw new OverflowException(); result = (UInt256)(Int256)v; return true; }
            if (typeof(TOther) == typeof(nint))    { nint  v = (nint)(object)value!;  if (v < 0) throw new OverflowException(); result = (ulong)(long)v; return true; }

            if (typeof(TOther) == typeof(float))   { result = checked((UInt256)(float)(object)value!); return true; }
            if (typeof(TOther) == typeof(double))  { result = checked((UInt256)(double)(object)value!); return true; }
            if (typeof(TOther) == typeof(decimal)) { result = checked((UInt256)(decimal)(object)value!); return true; }
            if (typeof(TOther) == typeof(Half))    { result = checked((UInt256)(double)(Half)(object)value!); return true; }

            result = default;
            return false;
        }

        private static bool TryConvertFromSaturatingImpl<TOther>(TOther value, out UInt256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!;    return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!;  return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (ulong)(object)value!;   return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (nuint)(object)value!;   return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte v = (sbyte)(object)value!; result = v < 0 ? Zero : (ulong)v; return true; }
            if (typeof(TOther) == typeof(short))   { short v = (short)(object)value!; result = v < 0 ? Zero : (ulong)v; return true; }
            if (typeof(TOther) == typeof(int))     { int v = (int)(object)value!;   result = v < 0 ? Zero : (ulong)v; return true; }
            if (typeof(TOther) == typeof(long))    { long v = (long)(object)value!;  result = v < 0 ? Zero : (ulong)v; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 v = (Int128)(object)value!; result = Int128.IsNegative(v) ? Zero : (UInt256)(Int256)v; return true; }
            if (typeof(TOther) == typeof(nint))    { nint v = (nint)(object)value!;  result = v < 0 ? Zero : (ulong)(long)v; return true; }

            if (typeof(TOther) == typeof(float))   { float v = (float)(object)value!; result = v <= 0f ? Zero : v >= (float)TWO_POW_256_D ? MaxValue : (UInt256)v; return true; }
            if (typeof(TOther) == typeof(double))  { double v = (double)(object)value!; result = v <= 0.0 ? Zero : v >= TWO_POW_256_D ? MaxValue : (UInt256)v; return true; }
            if (typeof(TOther) == typeof(decimal)) { decimal v = (decimal)(object)value!; result = v <= 0m ? Zero : (UInt256)v; return true; }
            if (typeof(TOther) == typeof(Half))    { double v = (double)(Half)(object)value!; result = v <= 0.0 ? Zero : v >= TWO_POW_256_D ? MaxValue : (UInt256)v; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertFromTruncatingImpl<TOther>(TOther value, out UInt256 result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (byte)(object)value!;    return true; }
            if (typeof(TOther) == typeof(char))    { result = (char)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (ushort)(object)value!;  return true; }
            if (typeof(TOther) == typeof(uint))    { result = (uint)(object)value!;    return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (ulong)(object)value!;   return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (UInt128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (nuint)(object)value!;   return true; }

            // Signed → unsigned truncating: reinterpret bit pattern of the signed source as unsigned of the same width.
            if (typeof(TOther) == typeof(sbyte))   { result = (byte)(sbyte)(object)value!; return true; }
            if (typeof(TOther) == typeof(short))   { result = (ushort)(short)(object)value!; return true; }
            if (typeof(TOther) == typeof(int))     { result = (uint)(int)(object)value!;  return true; }
            if (typeof(TOther) == typeof(long))    { result = (ulong)(long)(object)value!; return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (UInt256)(Int256)(Int128)(object)value!; return true; }
            if (typeof(TOther) == typeof(nint))    { result = (ulong)(long)(nint)(object)value!; return true; }

            if (typeof(TOther) == typeof(float))   { result = (UInt256)(float)(object)value!; return true; }
            if (typeof(TOther) == typeof(double))  { result = (UInt256)(double)(object)value!; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (UInt256)(decimal)(object)value!; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (UInt256)(double)(Half)(object)value!; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToCheckedImpl<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { byte   r = checked((byte)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(char))    { ushort r = checked((ushort)value); result = (TOther)(object)(char)r; return true; }
            if (typeof(TOther) == typeof(ushort))  { ushort r = checked((ushort)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(uint))    { uint   r = checked((uint)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ulong))   { ulong  r = checked((ulong)value);  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(UInt128)) { UInt128 r = checked((UInt128)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nuint))   { nuint  r = checked((nuint)(ulong)checked((ulong)value)); result = (TOther)(object)r; return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte r = checked((sbyte)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(short))   { short r = checked((short)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(int))     { int   r = checked((int)value);   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(long))    { long  r = checked((long)value);  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 r = checked((Int128)value); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nint))    { nint  r = checked((nint)checked((long)value)); result = (TOther)(object)r; return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { result = (TOther)(object)(decimal)value; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToSaturatingImpl<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { byte   r = value > byte.MaxValue   ? byte.MaxValue   : (byte)value._p0;   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(char))    { ushort r = value > char.MaxValue   ? char.MaxValue   : (ushort)value._p0; result = (TOther)(object)(char)r; return true; }
            if (typeof(TOther) == typeof(ushort))  { ushort r = value > ushort.MaxValue ? ushort.MaxValue : (ushort)value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(uint))    { uint   r = value > uint.MaxValue   ? uint.MaxValue   : (uint)value._p0;   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(ulong))   { ulong  r = (value._p1 | value._p2 | value._p3) != 0UL ? ulong.MaxValue : value._p0; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(UInt128)) { UInt128 r = (value._p2 | value._p3) != 0UL ? UInt128.MaxValue : new UInt128(value._p1, value._p0); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nuint))   { ulong r = (value._p1 | value._p2 | value._p3) != 0UL ? ulong.MaxValue : value._p0; result = (TOther)(object)(nuint)(r > nuint.MaxValue ? nuint.MaxValue : (nuint)r); return true; }

            if (typeof(TOther) == typeof(sbyte))   { sbyte  r = value > (ulong)sbyte.MaxValue  ? sbyte.MaxValue  : (sbyte)value._p0;  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(short))   { short  r = value > (ulong)short.MaxValue  ? short.MaxValue  : (short)value._p0;  result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(int))     { int    r = value > int.MaxValue           ? int.MaxValue    : (int)value._p0;    result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(long))    { long   r = value > (ulong)long.MaxValue   ? long.MaxValue   : (long)value._p0;   result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Int128))  { Int128 r = value > (UInt256)Int256.MaxValue ? Int128.MaxValue : (Int128)(UInt128)value; result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(nint))    { long  r = value > (ulong)long.MaxValue    ? long.MaxValue   : (long)value._p0;   result = (TOther)(object)(nint)(r > nint.MaxValue ? nint.MaxValue : (nint)r); return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { decimal r = (value._p3 != 0 || value._p2 != 0 || value._p1 > uint.MaxValue) ? decimal.MaxValue : new decimal((int)value._p0, (int)(value._p0 >> 32), (int)value._p1, false, 0); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        private static bool TryConvertToTruncatingImpl<TOther>(UInt256 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))    { result = (TOther)(object)(byte)value._p0;   return true; }
            if (typeof(TOther) == typeof(char))    { result = (TOther)(object)(char)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(ushort))  { result = (TOther)(object)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(uint))    { result = (TOther)(object)(uint)value._p0;   return true; }
            if (typeof(TOther) == typeof(ulong))   { result = (TOther)(object)value._p0;         return true; }
            if (typeof(TOther) == typeof(UInt128)) { result = (TOther)(object)new UInt128(value._p1, value._p0); return true; }
            if (typeof(TOther) == typeof(nuint))   { result = (TOther)(object)(nuint)value._p0;  return true; }

            if (typeof(TOther) == typeof(sbyte))   { result = (TOther)(object)(sbyte)(byte)value._p0;   return true; }
            if (typeof(TOther) == typeof(short))   { result = (TOther)(object)(short)(ushort)value._p0; return true; }
            if (typeof(TOther) == typeof(int))     { result = (TOther)(object)(int)(uint)value._p0;     return true; }
            if (typeof(TOther) == typeof(long))    { result = (TOther)(object)(long)value._p0;          return true; }
            if (typeof(TOther) == typeof(Int128))  { result = (TOther)(object)(Int128)new UInt128(value._p1, value._p0); return true; }
            if (typeof(TOther) == typeof(nint))    { result = (TOther)(object)(nint)(long)value._p0;    return true; }

            if (typeof(TOther) == typeof(float))   { result = (TOther)(object)(float)value; return true; }
            if (typeof(TOther) == typeof(double))  { result = (TOther)(object)(double)value; return true; }
            if (typeof(TOther) == typeof(decimal)) { decimal r = (value._p3 != 0 || value._p2 != 0 || value._p1 > uint.MaxValue) ? decimal.MaxValue : new decimal((int)value._p0, (int)(value._p0 >> 32), (int)value._p1, false, 0); result = (TOther)(object)r; return true; }
            if (typeof(TOther) == typeof(Half))    { result = (TOther)(object)(Half)(double)value; return true; }

            result = default;
            return false;
        }

        #endregion
    }
}
