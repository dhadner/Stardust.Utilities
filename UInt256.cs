using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// 256-bit unsigned integer type. Backed by two native <see cref="UInt128"/> halves so that
    /// inner limb arithmetic delegates to native CPU-supported 128-bit operations where possible.
    /// </summary>
    /// <remarks>
    /// Storage layout is host-native (endian-agnostic). For byte-ordered wire formats use
    /// <see cref="UInt256Le"/> or <see cref="UInt256Be"/>.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct UInt256 : IComparable, IComparable<UInt256>, IEquatable<UInt256>,
                                     IFormattable, ISpanFormattable, IParsable<UInt256>, ISpanParsable<UInt256>
    {
        internal readonly UInt128 _lo;
        internal readonly UInt128 _hi;

        /// <summary>The value zero.</summary>
        public static UInt256 Zero => default;
        /// <summary>The value one.</summary>
        public static UInt256 One => new(UInt128.Zero, UInt128.One);
        /// <summary>The maximum value (all bits set).</summary>
        public static UInt256 MaxValue => new(~UInt128.Zero, ~UInt128.Zero);
        /// <summary>The minimum value (zero).</summary>
        public static UInt256 MinValue => default;

        /// <summary>Initializes a new <see cref="UInt256"/> from two <see cref="UInt128"/> halves.</summary>
        /// <param name="hi">The high 128 bits.</param>
        /// <param name="lo">The low 128 bits.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(UInt128 hi, UInt128 lo) { _hi = hi; _lo = lo; }

        /// <summary>Initializes a new <see cref="UInt256"/> from four <see cref="ulong"/> limbs (most-significant first).</summary>
        /// <param name="u3">Bits 255..192.</param>
        /// <param name="u2">Bits 191..128.</param>
        /// <param name="u1">Bits 127..64.</param>
        /// <param name="u0">Bits 63..0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(ulong u3, ulong u2, ulong u1, ulong u0)
        {
            _hi = ((UInt128)u3 << 64) | u2;
            _lo = ((UInt128)u1 << 64) | u0;
        }

        /// <summary>Initializes a new <see cref="UInt256"/> from a <see cref="UInt128"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(UInt128 num) { _hi = UInt128.Zero; _lo = num; }

        /// <summary>Initializes a new <see cref="UInt256"/> from a <see cref="ulong"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(ulong num) { _hi = UInt128.Zero; _lo = num; }

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
                // Hex: concatenate the two halves, 32 hex digits each
                string hiHex = _hi.ToString(format, formatProvider);
                string loHex = _lo.ToString(format == "X" || format == "x" ? format + "32" : format, formatProvider);
                // If hi is zero and no width specifier, allow natural trim; otherwise pad lo to 32
                if (_hi == UInt128.Zero && format.Length == 1) return loHex;
                return hiHex + _lo.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
            }
            // Decimal / 'D' / 'G' / 'R' formats: use native base-10 conversion.
            // Other culture-sensitive or complex format strings (e.g., 'N', 'C')
            // still go through BigInteger since they require NumberFormatInfo support.
            if (fmt == 'D' || fmt == 'd' || fmt == 'G' || fmt == 'g' || fmt == 'R' || fmt == 'r')
            {
                string digits = UInt256Math.FormatDecimal(this, negative: false);
                // Honor a numeric precision specifier on 'D' / 'd' (minimum digit count).
                if ((fmt == 'D' || fmt == 'd') && format.Length > 1 &&
                    int.TryParse(format.AsSpan(1), System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out int minDigits) &&
                    digits.Length < minDigits)
                {
                    return new string('0', minDigits - digits.Length) + digits;
                }
                return digits;
            }
            // Fallback for less common numeric formats: delegate to BigInteger.
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

        /// <summary>Parses a string into a <see cref="UInt256"/>.</summary>
        public static UInt256 Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if ((style & NumberStyles.HexNumber) == NumberStyles.HexNumber)
            {
                string cleaned = s;
                if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) cleaned = cleaned.Substring(2);
                if (cleaned.Length == 0) throw new FormatException("Empty hex string.");
                // Pad to full 64 hex digits from the left
                if (cleaned.Length > 64) throw new OverflowException("Value exceeds 256 bits.");
                string padded = cleaned.PadLeft(64, '0');
                UInt128 hi = UInt128.Parse(padded.AsSpan(0, 32), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                UInt128 lo = UInt128.Parse(padded.AsSpan(32, 32), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return new UInt256(hi, lo);
            }
            // Decimal / integer path: native base-10 parse.
            if (style == NumberStyles.Integer || style == NumberStyles.None)
            {
                if (!UInt256Math.TryParseDecimal(s.AsSpan(), out UInt256 mag, out bool neg, out bool overflow))
                {
                    if (overflow) throw new OverflowException("Value exceeds 256 bits.");
                    throw new FormatException("Input string was not in a correct format.");
                }
                if (neg && mag != Zero)
                    throw new OverflowException("UInt256 cannot represent negative values.");
                return mag;
            }
            // Less common styles (AllowThousands, AllowCurrencySymbol, etc.) fall
            // back to BigInteger for correctness.
            BigInteger big = BigInteger.Parse(s, style, CultureInfo.InvariantCulture);
            if (big.Sign < 0) throw new OverflowException("UInt256 cannot represent negative values.");
            if (big.GetByteCount(isUnsigned: true) > 32) throw new OverflowException("Value exceeds 256 bits.");
            return FromBigInteger(big);
        }

        /// <inheritdoc/>
        public static UInt256 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer);

        /// <inheritdoc/>
        public static UInt256 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s.ToString(), NumberStyles.Integer);

        /// <inheritdoc/>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt256 result)
        {
            if (s is null) { result = default; return false; }
            try { result = Parse(s, NumberStyles.Integer); return true; }
            catch { result = default; return false; }
        }

        /// <inheritdoc/>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt256 result)
            => TryParse(s.ToString(), provider, out result);

        /// <inheritdoc/>
        public int CompareTo(object? obj)
        {
            if (obj is null) return 1;
            if (obj is UInt256 other) return CompareTo(other);
            throw new ArgumentException("Object must be of type UInt256.", nameof(obj));
        }

        /// <inheritdoc/>
        public int CompareTo(UInt256 other)
        {
            int c = _hi.CompareTo(other._hi);
            return c != 0 ? c : _lo.CompareTo(other._lo);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is UInt256 other && Equals(other);
        /// <inheritdoc/>
        public bool Equals(UInt256 other) => _hi == other._hi && _lo == other._lo;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_hi, _lo);

        /// <summary>Converts this value to a <see cref="BigInteger"/> (unsigned).</summary>
        public BigInteger ToBigInteger()
        {
            // Build 32-byte little-endian buffer with trailing zero for unsigned interpretation
            Span<byte> buf = stackalloc byte[33];
            WriteLittleEndianBytes(buf);
            buf[32] = 0; // ensure positive sign
            return new BigInteger(buf.ToArray());
        }

        /// <summary>Creates a <see cref="UInt256"/> from a non-negative <see cref="BigInteger"/>.</summary>
        /// <exception cref="OverflowException">The value is negative or exceeds 256 bits.</exception>
        public static UInt256 FromBigInteger(BigInteger value)
        {
            if (value.Sign < 0) throw new OverflowException("UInt256 cannot represent negative values.");
            byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
            if (bytes.Length > 32) throw new OverflowException("Value exceeds 256 bits.");
            Span<byte> buf = stackalloc byte[32];
            bytes.AsSpan().CopyTo(buf);
            // Little-endian layout in buffer
            ulong u0 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(0, 8));
            ulong u1 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(8, 8));
            ulong u2 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(16, 8));
            ulong u3 = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buf.Slice(24, 8));
            return new UInt256(u3, u2, u1, u0);
        }

        /// <summary>Writes this value to a 32-byte little-endian buffer.</summary>
        internal void WriteLittleEndianBytes(Span<byte> dest)
        {
            ulong u0 = (ulong)_lo;
            ulong u1 = (ulong)(_lo >> 64);
            ulong u2 = (ulong)_hi;
            ulong u3 = (ulong)(_hi >> 64);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(0, 8), u0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(8, 8), u1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(16, 8), u2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(24, 8), u3);
        }

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator +(UInt256 a) => a;

        /// <summary>Computes the two's complement negation (modulo 2^256).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator -(UInt256 a) => new UInt256(UInt128.Zero, UInt128.Zero) - a;

        /// <summary>Adds two values (wraps on overflow).</summary>
        /// <remarks>
        /// Uses an explicit 64-bit carry chain (no <c>UInt128</c>) so the JIT
        /// emits a tight <c>add/adc</c> sequence with no software-UInt128
        /// fallback. Passed by value rather than by <c>in</c>: measurements
        /// showed the <c>in</c> form regresses <see cref="Parse"/> by ~4x,
        /// apparently because it inhibits operator inlining in the large
        /// benchmark loop; by-value lets the JIT keep the limbs in registers.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator +(UInt256 a, UInt256 b)
        {
            ulong a0 = (ulong)a._lo, a1 = (ulong)(a._lo >> 64);
            ulong a2 = (ulong)a._hi, a3 = (ulong)(a._hi >> 64);
            ulong b0 = (ulong)b._lo, b1 = (ulong)(b._lo >> 64);
            ulong b2 = (ulong)b._hi, b3 = (ulong)(b._hi >> 64);

            ulong r0 = unchecked(a0 + b0);
            ulong c = r0 < a0 ? 1UL : 0UL;
            ulong r1 = unchecked(a1 + b1 + c);
            c = (r1 < a1 || (c == 1UL && r1 == a1)) ? 1UL : 0UL;
            ulong r2 = unchecked(a2 + b2 + c);
            c = (r2 < a2 || (c == 1UL && r2 == a2)) ? 1UL : 0UL;
            ulong r3 = unchecked(a3 + b3 + c);

            return new UInt256(((UInt128)r3 << 64) | r2, ((UInt128)r1 << 64) | r0);
        }

        /// <summary>Subtracts two values (wraps on underflow).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator -(UInt256 a, UInt256 b)
        {
            ulong a0 = (ulong)a._lo, a1 = (ulong)(a._lo >> 64);
            ulong a2 = (ulong)a._hi, a3 = (ulong)(a._hi >> 64);
            ulong b0 = (ulong)b._lo, b1 = (ulong)(b._lo >> 64);
            ulong b2 = (ulong)b._hi, b3 = (ulong)(b._hi >> 64);

            ulong r0 = unchecked(a0 - b0);
            ulong bw = r0 > a0 ? 1UL : 0UL;
            ulong r1 = unchecked(a1 - b1 - bw);
            bw = (r1 > a1 || (bw == 1UL && r1 == a1)) ? 1UL : 0UL;
            ulong r2 = unchecked(a2 - b2 - bw);
            bw = (r2 > a2 || (bw == 1UL && r2 == a2)) ? 1UL : 0UL;
            ulong r3 = unchecked(a3 - b3 - bw);

            return new UInt256(((UInt128)r3 << 64) | r2, ((UInt128)r1 << 64) | r0);
        }

        /// <summary>Multiplies two values (low 256 bits of the full product).</summary>
        public static UInt256 operator *(UInt256 a, UInt256 b)
        {
            // Split each into four 64-bit limbs: a = a3:a2:a1:a0, b = b3:b2:b1:b0
            ulong a0 = (ulong)a._lo, a1 = (ulong)(a._lo >> 64);
            ulong a2 = (ulong)a._hi, a3 = (ulong)(a._hi >> 64);
            ulong b0 = (ulong)b._lo, b1 = (ulong)(b._lo >> 64);
            ulong b2 = (ulong)b._hi, b3 = (ulong)(b._hi >> 64);

            // Schoolbook 4x4 unsigned multiply, low 256 bits only. Uses a
            // fused multiply-add (Multiply64 + BigMulAdd) built on BMI2 MULX
            // so the widening multiply doesn't set flags - letting the JIT
            // pipeline the carry-propagation adds without flag-dependency
            // stalls. No UInt128 arithmetic in the hot path.
            //
            // Four column passes, one per limb of b. Partial products whose
            // bit-position lies at or beyond 256 are discarded.

            // ---- Pass j = 0: p0..p3 <- a_i * b0 + carry ----
            ulong carry = UInt256Math.Multiply64(a0, b0, out ulong p0);
            carry = UInt256Math.BigMulAdd(a1, b0, carry, out ulong p1);
            carry = UInt256Math.BigMulAdd(a2, b0, carry, out ulong p2);
            // Top limb: only low half of a3*b0 matters (its high half is >= bit 256).
            ulong p3 = unchecked(a3 * b0 + carry);

            // ---- Pass j = 1: add (a * b1) << 64 ----
            ulong lo, c;
            carry = UInt256Math.Multiply64(a0, b1, out lo);
            p1 = UInt256Math.AddWithCarry(p1, lo, out c);
            carry = unchecked(carry + c);
            carry = UInt256Math.BigMulAdd(a1, b1, carry, out lo);
            p2 = UInt256Math.AddWithCarry(p2, lo, out c);
            carry = unchecked(carry + c);
            // a2*b1: only low half contributes to p3.
            p3 = unchecked(p3 + a2 * b1 + carry);

            // ---- Pass j = 2: add (a * b2) << 128 ----
            carry = UInt256Math.Multiply64(a0, b2, out lo);
            p2 = UInt256Math.AddWithCarry(p2, lo, out c);
            carry = unchecked(carry + c);
            p3 = unchecked(p3 + a1 * b2 + carry);

            // ---- Pass j = 3: only a0 * b3 low contributes to p3 ----
            p3 = unchecked(p3 + a0 * b3);

            return new UInt256(p3, p2, p1, p0);
        }

        /// <summary>Divides two values.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt256 operator /(UInt256 a, UInt256 b)
        {
            if (b._hi == UInt128.Zero && b._lo == UInt128.Zero) throw new DivideByZeroException();
            // Fast path: both fit in UInt128 - delegate to the native UInt128 divider.
            if (a._hi == UInt128.Zero && b._hi == UInt128.Zero)
                return new UInt256(UInt128.Zero, a._lo / b._lo);
            return UInt256Math.DivRem(a, b, out _);
        }

        /// <summary>Computes the remainder of division.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt256 operator %(UInt256 a, UInt256 b)
        {
            if (b._hi == UInt128.Zero && b._lo == UInt128.Zero) throw new DivideByZeroException();
            if (a._hi == UInt128.Zero && b._hi == UInt128.Zero)
                return new UInt256(UInt128.Zero, a._lo % b._lo);
            _ = UInt256Math.DivRem(a, b, out UInt256 rem);
            return rem;
        }

        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator &(UInt256 a, UInt256 b) => new(a._hi & b._hi, a._lo & b._lo);
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator |(UInt256 a, UInt256 b) => new(a._hi | b._hi, a._lo | b._lo);
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator ^(UInt256 a, UInt256 b) => new(a._hi ^ b._hi, a._lo ^ b._lo);
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator ~(UInt256 a) => new(~a._hi, ~a._lo);

        /// <summary>Shifts the value left by the specified amount.</summary>
        public static UInt256 operator <<(UInt256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            if (b >= 128) return new UInt256(a._lo << (b - 128), UInt128.Zero);
            // b in 1..127
            UInt128 newHi = (a._hi << b) | (a._lo >> (128 - b));
            UInt128 newLo = a._lo << b;
            return new UInt256(newHi, newLo);
        }

        /// <summary>Shifts the value right by the specified amount (logical for unsigned).</summary>
        public static UInt256 operator >>(UInt256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            if (b >= 128) return new UInt256(UInt128.Zero, a._hi >> (b - 128));
            UInt128 newLo = (a._lo >> b) | (a._hi << (128 - b));
            UInt128 newHi = a._hi >> b;
            return new UInt256(newHi, newLo);
        }

        /// <summary>Unsigned (logical) right shift. Identical to <c>&gt;&gt;</c> for unsigned types.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator >>>(UInt256 a, int b) => a >> b;

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256 a, UInt256 b) => a._hi == b._hi && a._lo == b._lo;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256 a, UInt256 b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256 a, UInt256 b) => a._hi < b._hi || (a._hi == b._hi && a._lo < b._lo);
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256 a, UInt256 b) => a._hi > b._hi || (a._hi == b._hi && a._lo > b._lo);
        /// <summary>Determines whether the left operand is less than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(UInt256 a, UInt256 b) => !(a > b);
        /// <summary>Determines whether the left operand is greater than or equal to the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(UInt256 a, UInt256 b) => !(a < b);

        /// <summary>Increments the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator ++(UInt256 a) => a + One;
        /// <summary>Decrements the value by one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator --(UInt256 a) => a - One;

        #endregion

        #region Conversions

        /// <summary>Implicitly widens a <see cref="UInt128"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(UInt128 a) => new(UInt128.Zero, a);
        /// <summary>Implicitly widens a <see cref="ulong"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(ulong a) => new(UInt128.Zero, a);
        /// <summary>Implicitly widens a <see cref="uint"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(uint a) => new(UInt128.Zero, a);
        /// <summary>Implicitly widens a <see cref="ushort"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(ushort a) => new(UInt128.Zero, a);
        /// <summary>Implicitly widens a <see cref="byte"/> to a <see cref="UInt256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt256(byte a) => new(UInt128.Zero, a);

        /// <summary>Narrowing conversion to <see cref="UInt128"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(UInt256 a) => a._lo;
        /// <summary>Narrowing conversion to <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ulong(UInt256 a) => (ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="uint"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(UInt256 a) => (uint)(ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="ushort"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ushort(UInt256 a) => (ushort)(ulong)a._lo;
        /// <summary>Narrowing conversion to <see cref="byte"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator byte(UInt256 a) => (byte)(ulong)a._lo;

        /// <summary>Reinterprets the bit pattern as a signed <see cref="Int256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt256 a) => new(a._hi, a._lo);

        #endregion
    }
}
