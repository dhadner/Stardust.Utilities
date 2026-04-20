using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// 256-bit signed integer type (two's complement). Shares its bit layout with
    /// <see cref="UInt256"/>; the sign lives in the top bit of the high half.
    /// </summary>
    /// <remarks>
    /// Storage layout is host-native (endian-agnostic). For byte-ordered wire formats use
    /// <see cref="Int256Le"/> or <see cref="Int256Be"/>.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Int256 : IComparable, IComparable<Int256>, IEquatable<Int256>,
                                    IFormattable, ISpanFormattable, IParsable<Int256>, ISpanParsable<Int256>
    {
        internal readonly UInt128 _lo;
        internal readonly UInt128 _hi;

        private static readonly UInt128 SignMask = (UInt128)1 << 127;

        /// <summary>The value zero.</summary>
        public static Int256 Zero => default;
        /// <summary>The value one.</summary>
        public static Int256 One => new(UInt128.Zero, UInt128.One);
        /// <summary>The value negative one (all bits set).</summary>
        public static Int256 NegativeOne => new(~UInt128.Zero, ~UInt128.Zero);
        /// <summary>The maximum representable value (2^255 - 1).</summary>
        public static Int256 MaxValue => new(~UInt128.Zero ^ SignMask, ~UInt128.Zero);
        /// <summary>The minimum representable value (-2^255).</summary>
        public static Int256 MinValue => new(SignMask, UInt128.Zero);

        /// <summary>Initializes a new <see cref="Int256"/> from two <see cref="UInt128"/> halves.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(UInt128 hi, UInt128 lo) { _hi = hi; _lo = lo; }

        /// <summary>Initializes a new <see cref="Int256"/> from a signed <see cref="Int128"/> value (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(Int128 num)
        {
            _lo = (UInt128)num;
            _hi = num < Int128.Zero ? ~UInt128.Zero : UInt128.Zero;
        }

        /// <summary>Initializes a new <see cref="Int256"/> from a signed <see cref="long"/> value (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(long num)
        {
            _lo = (UInt128)(Int128)num;
            _hi = num < 0 ? ~UInt128.Zero : UInt128.Zero;
        }

        /// <summary>True if this value is negative.</summary>
        public bool IsNegative => (_hi & SignMask) != UInt128.Zero;

        /// <summary>The low 128 bits.</summary>
        public UInt128 Lower => _lo;
        /// <summary>The high 128 bits.</summary>
        public UInt128 Upper => _hi;

        /// <inheritdoc/>
        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (string.IsNullOrEmpty(format)) format = "G";
            char fmt = format[0];
            if ((fmt == 'X' || fmt == 'x') && format.Length <= 3)
            {
                string hiHex = _hi.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
                string loHex = _lo.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
                return hiHex + loHex;
            }
            // Decimal / 'D' / 'G' / 'R' formats: use native base-10 conversion on
            // the unsigned magnitude, then prepend '-' if negative.
            if (fmt == 'D' || fmt == 'd' || fmt == 'G' || fmt == 'g' || fmt == 'R' || fmt == 'r')
            {
                bool neg = IsNegative;
                UInt256 mag = neg
                    ? (UInt256)(-this) // two's complement negation; Int256.MinValue wraps to itself (|min| = 2^255)
                    : (UInt256)this;
                string digits = UInt256Math.FormatDecimal(mag, negative: neg);
                if ((fmt == 'D' || fmt == 'd') && format.Length > 1 &&
                    int.TryParse(format.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int minDigits))
                {
                    // Insert leading zeros between the sign and the first digit.
                    int digitCount = neg ? digits.Length - 1 : digits.Length;
                    if (digitCount < minDigits)
                    {
                        string pad = new string('0', minDigits - digitCount);
                        return neg ? "-" + pad + digits.Substring(1) : pad + digits;
                    }
                }
                return digits;
            }
            return ToBigInteger().ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            string s = ToString(format.IsEmpty ? "G" : format.ToString(), provider);
            if (s.Length > destination.Length) { charsWritten = 0; return false; }
            s.AsSpan().CopyTo(destination);
            charsWritten = s.Length;
            return true;
        }

        /// <summary>Parses a string into an <see cref="Int256"/>.</summary>
        public static Int256 Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if ((style & NumberStyles.HexNumber) == NumberStyles.HexNumber)
            {
                string cleaned = s;
                if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) cleaned = cleaned.Substring(2);
                if (cleaned.Length == 0) throw new FormatException("Empty hex string.");
                if (cleaned.Length > 64) throw new OverflowException("Value exceeds 256 bits.");
                string padded = cleaned.PadLeft(64, '0');
                UInt128 hi = UInt128.Parse(padded.AsSpan(0, 32), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                UInt128 lo = UInt128.Parse(padded.AsSpan(32, 32), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return new Int256(hi, lo);
            }
            // Decimal / integer path: native base-10 parse on the magnitude,
            // then apply sign with range-check (Int256.MinValue magnitude = 2^255).
            if (style == NumberStyles.Integer || style == NumberStyles.None)
            {
                if (!UInt256Math.TryParseDecimal(s.AsSpan(), out UInt256 mag, out bool neg, out bool overflow))
                {
                    if (overflow) throw new OverflowException("Value exceeds Int256 range.");
                    throw new FormatException("Input string was not in a correct format.");
                }
                if (!neg)
                {
                    if ((mag.Upper & ((UInt128)1 << 127)) != UInt128.Zero)
                        throw new OverflowException("Value exceeds Int256 range.");
                    return new Int256(mag.Upper, mag.Lower);
                }
                // Negative: representable range is [-2^255, -1]. Magnitude must be <= 2^255.
                UInt256 absMin = new UInt256((UInt128)1 << 127, UInt128.Zero);
                if (mag > absMin) throw new OverflowException("Value exceeds Int256 range.");
                // Two's complement negate.
                UInt256 neg256 = UInt256.Zero - mag;
                return new Int256(neg256.Upper, neg256.Lower);
            }
            BigInteger big = BigInteger.Parse(s, style, CultureInfo.InvariantCulture);
            return FromBigInteger(big);
        }

        /// <inheritdoc/>
        public static Int256 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer);
        /// <inheritdoc/>
        public static Int256 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s.ToString(), NumberStyles.Integer);
        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int256 result)
        {
            if (s is null) { result = default; return false; }
            try { result = Parse(s, NumberStyles.Integer); return true; }
            catch { result = default; return false; }
        }
        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int256 result)
            => TryParse(s.ToString(), provider, out result);

        /// <inheritdoc/>
        public int CompareTo(object? obj)
        {
            if (obj is null) return 1;
            if (obj is Int256 other) return CompareTo(other);
            throw new ArgumentException("Object must be of type Int256.", nameof(obj));
        }

        /// <inheritdoc/>
        public int CompareTo(Int256 other)
        {
            bool aNeg = IsNegative;
            bool bNeg = other.IsNegative;
            if (aNeg != bNeg) return aNeg ? -1 : 1;
            int c = _hi.CompareTo(other._hi);
            return c != 0 ? c : _lo.CompareTo(other._lo);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Int256 other && Equals(other);
        /// <inheritdoc/>
        public bool Equals(Int256 other) => _hi == other._hi && _lo == other._lo;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_hi, _lo);

        /// <summary>Converts this value to a <see cref="BigInteger"/> (signed, two's complement).</summary>
        public BigInteger ToBigInteger()
        {
            Span<byte> buf = stackalloc byte[32];
            // Write as little-endian 32 bytes; interpret as signed two's complement
            ulong u0 = (ulong)_lo;
            ulong u1 = (ulong)(_lo >> 64);
            ulong u2 = (ulong)_hi;
            ulong u3 = (ulong)(_hi >> 64);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(0, 8), u0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(8, 8), u1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(16, 8), u2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(24, 8), u3);
            return new BigInteger(buf.ToArray());
        }

        /// <summary>Creates an <see cref="Int256"/> from a <see cref="BigInteger"/>.</summary>
        /// <exception cref="OverflowException">The value falls outside the Int256 range.</exception>
        public static Int256 FromBigInteger(BigInteger value)
        {
            byte[] bytes = value.ToByteArray(isUnsigned: false, isBigEndian: false);
            if (bytes.Length > 32)
            {
                // Excess bytes must all be sign-extension bytes.
                byte signByte = (byte)(value.Sign < 0 ? 0xFF : 0x00);
                for (int i = 32; i < bytes.Length; i++)
                {
                    if (bytes[i] != signByte) throw new OverflowException("Value exceeds Int256 range.");
                }
                // The sign bit of byte 31 must match the overall sign.
                if (((bytes[31] & 0x80) != 0) != (value.Sign < 0))
                    throw new OverflowException("Value exceeds Int256 range.");
            }
            Span<byte> buf = stackalloc byte[32];
            if (value.Sign < 0) buf.Fill(0xFF);
            bytes.AsSpan(0, Math.Min(bytes.Length, 32)).CopyTo(buf);
            ulong u0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(0, 8));
            ulong u1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(8, 8));
            ulong u2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(16, 8));
            ulong u3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(24, 8));
            return new Int256(((UInt128)u3 << 64) | u2, ((UInt128)u1 << 64) | u0);
        }

        internal byte[] ToByteArrayLE()
        {
            byte[] buf = new byte[32];
            ulong u0 = (ulong)_lo;
            ulong u1 = (ulong)(_lo >> 64);
            ulong u2 = (ulong)_hi;
            ulong u3 = (ulong)(_hi >> 64);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(0, 8), u0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(8, 8), u1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(16, 8), u2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(24, 8), u3);
            return buf;
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator +(Int256 a) => a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator -(Int256 a) => (Int256)(-(UInt256)a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator +(Int256 a, Int256 b) => (Int256)((UInt256)a + (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator -(Int256 a, Int256 b) => (Int256)((UInt256)a - (UInt256)b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator *(Int256 a, Int256 b) => (Int256)((UInt256)a * (UInt256)b);

        public static Int256 operator /(Int256 a, Int256 b)
        {
            if (b._hi == UInt128.Zero && b._lo == UInt128.Zero) throw new DivideByZeroException();
            bool aNeg = a.IsNegative;
            bool bNeg = b.IsNegative;
            UInt256 au = aNeg ? (UInt256)(-a) : (UInt256)a;
            UInt256 bu = bNeg ? (UInt256)(-b) : (UInt256)b;
            UInt256 q;
            if (au._p2 == 0 && au._p3 == 0 && bu._p2 == 0 && bu._p3 == 0)
                q = new UInt256(UInt128.Zero, au.Lower / bu.Lower);
            else
                q = UInt256Math.DivRem(au, bu, out _);
            return (aNeg ^ bNeg) ? (Int256)(-q) : (Int256)q;
        }

        public static Int256 operator %(Int256 a, Int256 b)
        {
            if (b._hi == UInt128.Zero && b._lo == UInt128.Zero) throw new DivideByZeroException();
            bool aNeg = a.IsNegative;
            bool bNeg = b.IsNegative;
            UInt256 au = aNeg ? (UInt256)(-a) : (UInt256)a;
            UInt256 bu = bNeg ? (UInt256)(-b) : (UInt256)b;
            UInt256 r;
            if (au._p2 == 0 && au._p3 == 0 && bu._p2 == 0 && bu._p3 == 0)
                r = new UInt256(UInt128.Zero, au.Lower % bu.Lower);
            else
                _ = UInt256Math.DivRem(au, bu, out r);
            // Remainder takes the sign of the dividend (C# / BigInteger convention).
            return aNeg ? (Int256)(-r) : (Int256)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator &(Int256 a, Int256 b) => new(a._hi & b._hi, a._lo & b._lo);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator |(Int256 a, Int256 b) => new(a._hi | b._hi, a._lo | b._lo);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ^(Int256 a, Int256 b) => new(a._hi ^ b._hi, a._lo ^ b._lo);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ~(Int256 a) => new(~a._hi, ~a._lo);

        /// <summary>Shifts left by the specified amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator <<(Int256 a, int b) => (Int256)((UInt256)a << b);

        /// <summary>Arithmetic (sign-extending) right shift.</summary>
        public static Int256 operator >>(Int256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            bool neg = a.IsNegative;
            UInt256 u = (UInt256)a >>> b;
            if (!neg) return (Int256)u;
            // Fill top b bits with ones
            UInt256 mask;
            if (b >= 256) mask = UInt256.MaxValue;
            else
            {
                // mask = ~((UInt256)1 << (256 - b)) is complicated; build directly
                mask = ~(UInt256.MaxValue >> b);
            }
            return (Int256)(u | mask);
        }

        /// <summary>Unsigned (logical, zero-filling) right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator >>>(Int256 a, int b) => (Int256)((UInt256)a >>> b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Int256 a, Int256 b) => a._hi == b._hi && a._lo == b._lo;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Int256 a, Int256 b) => !(a == b);

        public static bool operator <(Int256 a, Int256 b)
        {
            bool an = a.IsNegative, bn = b.IsNegative;
            if (an != bn) return an;
            return a._hi < b._hi || (a._hi == b._hi && a._lo < b._lo);
        }
        public static bool operator >(Int256 a, Int256 b) => b < a;
        public static bool operator <=(Int256 a, Int256 b) => !(a > b);
        public static bool operator >=(Int256 a, Int256 b) => !(a < b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ++(Int256 a) => a + One;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator --(Int256 a) => a - One;

        #endregion

        #region Conversions

        /// <summary>Implicitly widens an <see cref="Int128"/> to an <see cref="Int256"/> (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(Int128 a) => new(a);
        /// <summary>Implicitly widens a <see cref="long"/> to an <see cref="Int256"/> (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(long a) => new(a);
        /// <summary>Implicitly widens an <see cref="int"/> to an <see cref="Int256"/> (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(int a) => new((long)a);
        /// <summary>Implicitly widens a <see cref="short"/> to an <see cref="Int256"/> (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(short a) => new((long)a);
        /// <summary>Implicitly widens an <see cref="sbyte"/> to an <see cref="Int256"/> (sign-extending).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(sbyte a) => new((long)a);

        /// <summary>Narrowing conversion to <see cref="Int128"/> (keeps low 128 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128(Int256 a) => (Int128)a._lo;
        /// <summary>Narrowing conversion to <see cref="long"/> (keeps low 64 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(Int256 a) => (long)(ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="int"/> (keeps low 32 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Int256 a) => (int)(uint)(ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="short"/> (keeps low 16 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator short(Int256 a) => (short)(ushort)(ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="sbyte"/> (keeps low 8 bits).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator sbyte(Int256 a) => (sbyte)(byte)(ulong)a._lo;

        /// <summary>Reinterprets the bit pattern as an unsigned <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(Int256 a) => new(a._hi, a._lo);

        #endregion
    }
}
