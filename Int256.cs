using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// 256-bit signed integer type (two's complement). Backed by four native <see cref="ulong"/>
    /// limbs (the same layout as <see cref="UInt256"/>); the sign lives in the top bit of
    /// <c>_p3</c>. The 4-limb layout was adopted in v0.9.13 (replacing an earlier two-<see cref="UInt128"/>
    /// layout) so that arithmetic operators that delegate to <see cref="UInt256"/> avoid an
    /// unnecessary 4-ulong → 2-UInt128 → 4-ulong round-trip on every cast.
    /// </summary>
    /// <remarks>
    /// Storage layout is host-native (endian-agnostic). For byte-ordered wire formats use
    /// <see cref="Int256Le"/> or <see cref="Int256Be"/>.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct Int256 : IComparable, IComparable<Int256>, IEquatable<Int256>,
                                            IFormattable, ISpanFormattable, IParsable<Int256>, ISpanParsable<Int256>
    {
        internal readonly ulong _p0; // bits 0-63   (LSB)
        internal readonly ulong _p1; // bits 64-127
        internal readonly ulong _p2; // bits 128-191
        internal readonly ulong _p3; // bits 192-255 (top bit is the sign)

        private const ulong SIGN_BIT = 0x8000_0000_0000_0000UL;

        /// <summary>The value zero.</summary>
        public static Int256 Zero => default;
        /// <summary>The value one.</summary>
        public static Int256 One => new(0UL, 0UL, 0UL, 1UL);
        /// <summary>The value negative one (all bits set).</summary>
        public static Int256 NegativeOne => new(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        /// <summary>The maximum representable value (2^255 - 1).</summary>
        public static Int256 MaxValue => new(ulong.MaxValue ^ SIGN_BIT, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        /// <summary>The minimum representable value (-2^255).</summary>
        public static Int256 MinValue => new(SIGN_BIT, 0UL, 0UL, 0UL);

        /// <summary>Initializes a new <see cref="Int256"/> from four <see cref="ulong"/> limbs (most-significant first).</summary>
        /// <param name="u3">Bits 255..192 (top bit is the sign).</param>
        /// <param name="u2">Bits 191..128.</param>
        /// <param name="u1">Bits 127..64.</param>
        /// <param name="u0">Bits 63..0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(ulong u3, ulong u2, ulong u1, ulong u0)
        {
            _p0 = u0; _p1 = u1; _p2 = u2; _p3 = u3;
        }

        /// <summary>Initializes a new <see cref="Int256"/> from two <see cref="UInt128"/> halves.</summary>
        /// <param name="hi">The high 128 bits (top bit is the sign).</param>
        /// <param name="lo">The low 128 bits.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(UInt128 hi, UInt128 lo)
        {
            _p0 = (ulong)lo;
            _p1 = (ulong)(lo >> 64);
            _p2 = (ulong)hi;
            _p3 = (ulong)(hi >> 64);
        }

        /// <summary>Initializes a new <see cref="Int256"/> from a signed <see cref="Int128"/> value (sign-extending).</summary>
        /// <param name="num">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(Int128 num)
        {
            UInt128 lo = (UInt128)num;
            _p0 = (ulong)lo;
            _p1 = (ulong)(lo >> 64);
            ulong sext = num < Int128.Zero ? ulong.MaxValue : 0UL;
            _p2 = sext;
            _p3 = sext;
        }

        /// <summary>Initializes a new <see cref="Int256"/> from a signed <see cref="long"/> value (sign-extending).</summary>
        /// <param name="num">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int256(long num)
        {
            _p0 = (ulong)num;
            ulong sext = num < 0 ? ulong.MaxValue : 0UL;
            _p1 = sext;
            _p2 = sext;
            _p3 = sext;
        }

        /// <summary>Initializes a new <see cref="Int256"/> from a byte span. The span is 32 bytes.</summary>
        /// <param name="bytes">The byte span containing the value.</param>
        /// <param name="isBigEndian">Whether the byte span is in big-endian order (default: false for little-endian).</param>
        public Int256(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
        {
            if (bytes.Length < 32) throw new ArgumentException("Span must be at least 32 bytes.", nameof(bytes));
            if (isBigEndian)
            {
                _p3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(0, 8));
                _p2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));
                _p1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(16, 8));
                _p0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(24, 8));
            }
            else
            {
                _p0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(0, 8));
                _p1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(8, 8));
                _p2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(16, 8));
                _p3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(24, 8));
            }
        }

        /// <summary>
        /// Creates a new <see cref="Int256"/> from a byte span with the specified offset. The span must have at least 32 bytes starting from the offset.
        /// </summary>
        /// <param name="bytes">The byte span containing the value.</param>
        /// <param name="offset">The starting offset in the span.</param>
        /// <param name="isBigEndian">Whether the byte span is in big-endian order (default: false for little-endian).</param>
        /// <exception cref="ArgumentException">Thrown if the span is too short.</exception>
        public Int256(ReadOnlySpan<byte> bytes, int offset, bool isBigEndian = false)
        {
            if (bytes.Length - offset < 32) throw new ArgumentException("Span must be at least 32 bytes.", nameof(bytes));
            if (isBigEndian)
            {
                _p3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset, 8));
                _p2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset + 8, 8));
                _p1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset + 16, 8));
                _p0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset + 24, 8));
            }
            else
            {
                _p0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset, 8));
                _p1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset + 8, 8));
                _p2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset + 16, 8));
                _p3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(offset + 24, 8));
            }
        }

        /// <summary>The low 128 bits.</summary>
        public UInt128 Lower => new(_p1, _p0);
        /// <summary>The high 128 bits.</summary>
        public UInt128 Upper => new(_p3, _p2);

        /// <inheritdoc/>
        public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (string.IsNullOrEmpty(format)) format = "G";
            char fmt = format[0];
            if ((fmt == 'X' || fmt == 'x') && format.Length <= 3)
            {
                string hiHex = Upper.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
                string loHex = Lower.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
                return hiHex + loHex;
            }
            // Decimal / 'D' / 'G' / 'R' formats: use native base-10 conversion on
            // the unsigned magnitude, then prepend '-' if negative.
            if (fmt == 'D' || fmt == 'd' || fmt == 'G' || fmt == 'g' || fmt == 'R' || fmt == 'r')
            {
                bool neg = IsNegative(this);
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
        /// <param name="s">The input.</param>
        /// <param name="style">Number style.</param>
        /// <returns>The parsed value.</returns>
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
                    if ((mag._p3 & SIGN_BIT) != 0)
                        throw new OverflowException("Value exceeds Int256 range.");
                    return new Int256(mag._p3, mag._p2, mag._p1, mag._p0);
                }
                // Negative: representable range is [-2^255, -1]. Magnitude must be <= 2^255.
                UInt256 absMin = new UInt256(SIGN_BIT, 0UL, 0UL, 0UL);
                if (mag > absMin) throw new OverflowException("Value exceeds Int256 range.");
                // Two's complement negate.
                UInt256 neg256 = UInt256.Zero - mag;
                return new Int256(neg256._p3, neg256._p2, neg256._p1, neg256._p0);
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
        public readonly int CompareTo(object? obj)
        {
            if (obj is null) return 1;
            if (obj is Int256 other) return CompareTo(other);
            throw new ArgumentException("Object must be of type Int256.", nameof(obj));
        }

        /// <inheritdoc/>
        public readonly int CompareTo(Int256 other)
        {
            bool aNeg = IsNegative(this);
            bool bNeg = IsNegative(other);
            if (aNeg != bNeg) return aNeg ? -1 : 1;
            if (_p3 != other._p3) return _p3.CompareTo(other._p3);
            if (_p2 != other._p2) return _p2.CompareTo(other._p2);
            if (_p1 != other._p1) return _p1.CompareTo(other._p1);
            return _p0.CompareTo(other._p0);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is Int256 other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(Int256 other) => _p0 == other._p0 && _p1 == other._p1 && _p2 == other._p2 && _p3 == other._p3;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(_p0, _p1, _p2, _p3);

        /// <summary>Converts this value to a <see cref="BigInteger"/> (signed, two's complement).</summary>
        /// <returns>The equivalent <see cref="BigInteger"/>.</returns>
        public BigInteger ToBigInteger()
        {
            Span<byte> buf = stackalloc byte[32];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(0, 8), _p0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(8, 8), _p1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(16, 8), _p2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(24, 8), _p3);
            return new BigInteger(buf.ToArray());
        }

        /// <summary>Creates an <see cref="Int256"/> from a <see cref="BigInteger"/>.</summary>
        /// <param name="value">The source value.</param>
        /// <returns>The converted <see cref="Int256"/>.</returns>
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
            ulong p0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(0, 8));
            ulong p1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(8, 8));
            ulong p2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(16, 8));
            ulong p3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(24, 8));
            return new Int256(p3, p2, p1, p0);
        }

        internal byte[] ToByteArrayLE()
        {
            byte[] buf = new byte[32];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(0, 8), _p0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(8, 8), _p1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(16, 8), _p2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(24, 8), _p3);
            return buf;
        }

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        /// <param name="a">The operand.</param>
        /// <returns><paramref name="a"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator +(Int256 a) => a;

        /// <summary>Computes the two's complement negation (wraps at <see cref="MinValue"/>).</summary>
        /// <param name="a">The operand.</param>
        /// <returns>-<paramref name="a"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator -(Int256 a) => (Int256)(-(UInt256)a);

        /// <summary>Adds two values (wraps on overflow).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator +(Int256 a, Int256 b) => (Int256)((UInt256)a + (UInt256)b);
        /// <summary>Subtracts two values (wraps on underflow).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator -(Int256 a, Int256 b) => (Int256)((UInt256)a - (UInt256)b);
        /// <summary>Multiplies two values (low 256 bits of the full product; two's complement semantics).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator *(Int256 a, Int256 b) => (Int256)((UInt256)a * (UInt256)b);

        /// <summary>Divides two values (truncating toward zero).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The quotient.</returns>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static Int256 operator /(Int256 a, Int256 b)
        {
            if ((b._p0 | b._p1 | b._p2 | b._p3) == 0UL) throw new DivideByZeroException();
            bool aNeg = IsNegative(a);
            bool bNeg = IsNegative(b);
            UInt256 au = aNeg ? (UInt256)(-a) : (UInt256)a;
            UInt256 bu = bNeg ? (UInt256)(-b) : (UInt256)b;
            UInt256 q;
            if (au._p2 == 0 && au._p3 == 0 && bu._p2 == 0 && bu._p3 == 0)
                q = new UInt256(UInt128.Zero, au.Lower / bu.Lower);
            else
                q = UInt256Math.DivRem(au, bu, out _);
            return (aNeg ^ bNeg) ? (Int256)(-q) : (Int256)q;
        }

        /// <summary>Computes the remainder of division (takes the sign of the dividend).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The remainder.</returns>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static Int256 operator %(Int256 a, Int256 b)
        {
            if ((b._p0 | b._p1 | b._p2 | b._p3) == 0UL) throw new DivideByZeroException();
            bool aNeg = IsNegative(a);
            bool bNeg = IsNegative(b);
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

        /// <summary>Computes the bitwise AND of two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The AND of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator &(Int256 a, Int256 b) => new(a._p3 & b._p3, a._p2 & b._p2, a._p1 & b._p1, a._p0 & b._p0);
        /// <summary>Computes the bitwise OR of two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The OR of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator |(Int256 a, Int256 b) => new(a._p3 | b._p3, a._p2 | b._p2, a._p1 | b._p1, a._p0 | b._p0);
        /// <summary>Computes the bitwise XOR of two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>The XOR of the two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ^(Int256 a, Int256 b) => new(a._p3 ^ b._p3, a._p2 ^ b._p2, a._p1 ^ b._p1, a._p0 ^ b._p0);
        /// <summary>Computes the bitwise complement of the value.</summary>
        /// <param name="a">The operand.</param>
        /// <returns>The bitwise complement.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ~(Int256 a) => new(~a._p3, ~a._p2, ~a._p1, ~a._p0);

        /// <summary>Shifts left by the specified amount.</summary>
        /// <param name="a">The operand.</param>
        /// <param name="b">Shift amount.</param>
        /// <returns>The shifted value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator <<(Int256 a, int b) => (Int256)((UInt256)a << b);

        /// <summary>Arithmetic (sign-extending) right shift.</summary>
        /// <param name="a">The operand.</param>
        /// <param name="b">Shift amount.</param>
        /// <returns>The shifted value.</returns>
        public static Int256 operator >>(Int256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            bool neg = IsNegative(a);
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
        /// <param name="a">The operand.</param>
        /// <param name="b">Shift amount.</param>
        /// <returns>The shifted value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator >>>(Int256 a, int b) => (Int256)((UInt256)a >>> b);

        /// <summary>Determines whether two values are equal.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Int256 a, Int256 b) => a._p0 == b._p0 && a._p1 == b._p1 && a._p2 == b._p2 && a._p3 == b._p3;
        /// <summary>Determines whether two values are not equal.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if not equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Int256 a, Int256 b) => !(a == b);

        /// <summary>Determines whether the left operand is less than the right (signed comparison).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if a is less than b.</returns>
        public static bool operator <(Int256 a, Int256 b)
        {
            bool an = IsNegative(a), bn = IsNegative(b);
            if (an != bn) return an;
            if (a._p3 != b._p3) return a._p3 < b._p3;
            if (a._p2 != b._p2) return a._p2 < b._p2;
            if (a._p1 != b._p1) return a._p1 < b._p1;
            return a._p0 < b._p0;
        }
        /// <summary>Determines whether the left operand is greater than the right (signed comparison).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if a is greater than b.</returns>
        public static bool operator >(Int256 a, Int256 b) => b < a;
        /// <summary>Determines whether the left operand is less than or equal to the right (signed comparison).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if a is less than or equal to b.</returns>
        public static bool operator <=(Int256 a, Int256 b) => !(a > b);
        /// <summary>Determines whether the left operand is greater than or equal to the right (signed comparison).</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>True if a is greater than or equal to b.</returns>
        public static bool operator >=(Int256 a, Int256 b) => !(a < b);

        /// <summary>Increments the value by one.</summary>
        /// <param name="a">The operand.</param>
        /// <returns>The incremented value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator ++(Int256 a) => a + One;
        /// <summary>Decrements the value by one.</summary>
        /// <param name="a">The operand.</param>
        /// <returns>The decremented value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 operator --(Int256 a) => a - One;

        #endregion

        #region Conversions

        /// <summary>Implicitly widens an <see cref="Int128"/> to an <see cref="Int256"/> (sign-extending).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(Int128 a) => new(a);
        /// <summary>Implicitly widens a <see cref="long"/> to an <see cref="Int256"/> (sign-extending).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(long a) => new(a);
        /// <summary>Implicitly widens an <see cref="int"/> to an <see cref="Int256"/> (sign-extending).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(int a) => new((long)a);
        /// <summary>Implicitly widens a <see cref="short"/> to an <see cref="Int256"/> (sign-extending).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(short a) => new((long)a);
        /// <summary>Implicitly widens an <see cref="sbyte"/> to an <see cref="Int256"/> (sign-extending).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int256(sbyte a) => new((long)a);

        /// <summary>Narrowing conversion to <see cref="Int128"/> (keeps low 128 bits).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128(Int256 a) => (Int128)new UInt128(a._p1, a._p0);
        /// <summary>Narrowing conversion to <see cref="long"/> (keeps low 64 bits).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(Int256 a) => (long)a._p0;
        /// <summary>Narrowing conversion to <see cref="int"/> (keeps low 32 bits).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Int256 a) => (int)(uint)a._p0;
        /// <summary>Narrowing conversion to <see cref="short"/> (keeps low 16 bits).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator short(Int256 a) => (short)(ushort)a._p0;
        /// <summary>Narrowing conversion to <see cref="sbyte"/> (keeps low 8 bits).</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator sbyte(Int256 a) => (sbyte)(byte)a._p0;

        /// <summary>Reinterprets the bit pattern as an unsigned <see cref="UInt256"/>.</summary>
        /// <param name="a">The source value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt256(Int256 a) => new(a._p3, a._p2, a._p1, a._p0);

        #endregion
    }
}
