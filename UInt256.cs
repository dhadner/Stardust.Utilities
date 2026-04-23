using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// 256-bit unsigned integer type. Backed by four native <see cref="ulong"/> limbs so that
    /// arithmetic operators access limbs directly without any field-extraction overhead.
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
        internal readonly ulong _p0; // bits 0-63
        internal readonly ulong _p1; // bits 64-127
        internal readonly ulong _p2; // bits 128-191
        internal readonly ulong _p3; // bits 192-255

        /// <summary>The value zero.</summary>
        public static UInt256 Zero => default;
        /// <summary>The value one.</summary>
        public static UInt256 One => new(0UL, 0UL, 0UL, 1UL);
        /// <summary>The maximum value (all bits set).</summary>
        public static UInt256 MaxValue => new(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        /// <summary>The minimum value (zero).</summary>
        public static UInt256 MinValue => default;

        /// <summary>Initializes a new <see cref="UInt256"/> from two <see cref="UInt128"/> halves.</summary>
        /// <param name="hi">The high 128 bits.</param>
        /// <param name="lo">The low 128 bits.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(UInt128 hi, UInt128 lo)
        {
            _p0 = (ulong)lo;
            _p1 = (ulong)(lo >> 64);
            _p2 = (ulong)hi;
            _p3 = (ulong)(hi >> 64);
        }

        /// <summary>Initializes a new <see cref="UInt256"/> from four <see cref="ulong"/> limbs (most-significant first).</summary>
        /// <param name="u3">Bits 255..192.</param>
        /// <param name="u2">Bits 191..128.</param>
        /// <param name="u1">Bits 127..64.</param>
        /// <param name="u0">Bits 63..0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(ulong u3, ulong u2, ulong u1, ulong u0)
        {
            _p0 = u0; _p1 = u1; _p2 = u2; _p3 = u3;
        }

        /// <summary>Initializes a new <see cref="UInt256"/> from a <see cref="UInt128"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(UInt128 num) { _p0 = (ulong)num; _p1 = (ulong)(num >> 64); _p2 = 0; _p3 = 0; }

        /// <summary>Initializes a new <see cref="UInt256"/> from a <see cref="ulong"/> value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt256(ulong num) { _p0 = num; _p1 = 0; _p2 = 0; _p3 = 0; }

        /// <summary>Initializes a new <see cref="UInt256"/> from a byte span. The span is 32 bytes.</summary>
        public UInt256(ReadOnlySpan<byte> bytes, bool isBigEndian = false)
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

        /// <summary>The low 128 bits.</summary>
        public UInt128 Lower => new UInt128(_p1, _p0);
        /// <summary>The high 128 bits.</summary>
        public UInt128 Upper => new UInt128(_p3, _p2);

        /// <inheritdoc/>
        public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (string.IsNullOrEmpty(format)) format = "G";
            char fmt = format[0];
            if ((fmt == 'X' || fmt == 'x') && format.Length <= 3)
            {
                // Hex: concatenate the two halves, 32 hex digits each
                UInt128 hi = Upper, lo = Lower;
                string hiHex = hi.ToString(format, formatProvider);
                string loHex = lo.ToString(format == "X" || format == "x" ? format + "32" : format, formatProvider);
                // If hi is zero and no width specifier, allow natural trim; otherwise pad lo to 32
                if (hi == UInt128.Zero && format.Length == 1) return loHex;
                return hiHex + lo.ToString((fmt == 'X' ? "X32" : "x32"), formatProvider);
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
        public readonly int CompareTo(object? obj)
        {
            if (obj is null) return 1;
            if (obj is UInt256 other) return CompareTo(other);
            throw new ArgumentException("Object must be of type UInt256.", nameof(obj));
        }

        /// <inheritdoc/>
        public readonly int CompareTo(UInt256 other)
        {
            if (_p3 != other._p3) return _p3.CompareTo(other._p3);
            if (_p2 != other._p2) return _p2.CompareTo(other._p2);
            if (_p1 != other._p1) return _p1.CompareTo(other._p1);
            return _p0.CompareTo(other._p0);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) => obj is UInt256 other && Equals(other);
        /// <inheritdoc/>
        public readonly bool Equals(UInt256 other) => _p0 == other._p0 && _p1 == other._p1 && _p2 == other._p2 && _p3 == other._p3;
        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(_p0, _p1, _p2, _p3);

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
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(0, 8), _p0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(8, 8), _p1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(16, 8), _p2);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(24, 8), _p3);
        }

        #region Operators

        /// <summary>Returns the value (unary plus).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator +(UInt256 a) => a;

        /// <summary>Computes the two's complement negation (modulo 2^256).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator -(UInt256 a) => Zero - a;

        /// <summary>Adds two values (wraps on overflow).</summary>
        /// <remarks>
        /// Direct 4-limb carry chain on the raw ulong fields. No field extraction
        /// overhead -- <c>_p0</c>..<c>_p3</c> are read once per call.
        /// The carry condition <c>(r &lt; a || (c == 1 &amp;&amp; r == a))</c>
        /// handles the edge case where both limbs are <c>UINT64_MAX</c> with an
        /// incoming carry (the simpler <c>r &lt; a</c> alone misses that case).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator +(UInt256 a, UInt256 b)
        {
            ulong r0 = unchecked(a._p0 + b._p0);
            ulong c = r0 < a._p0 ? 1UL : 0UL;
            ulong r1 = unchecked(a._p1 + b._p1 + c);
            c = (r1 < a._p1 || (c == 1UL && r1 == a._p1)) ? 1UL : 0UL;
            ulong r2 = unchecked(a._p2 + b._p2 + c);
            c = (r2 < a._p2 || (c == 1UL && r2 == a._p2)) ? 1UL : 0UL;
            ulong r3 = unchecked(a._p3 + b._p3 + c);
            return new UInt256(r3, r2, r1, r0);
        }

        /// <summary>Subtracts two values (wraps on underflow).</summary>
        /// <remarks>
        /// Mirrors <c>operator +</c> using a borrow chain with the same
        /// two-condition edge-case guard.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator -(UInt256 a, UInt256 b)
        {
            ulong r0 = unchecked(a._p0 - b._p0);
            ulong bw = r0 > a._p0 ? 1UL : 0UL;
            ulong r1 = unchecked(a._p1 - b._p1 - bw);
            bw = (r1 > a._p1 || (bw == 1UL && r1 == a._p1)) ? 1UL : 0UL;
            ulong r2 = unchecked(a._p2 - b._p2 - bw);
            bw = (r2 > a._p2 || (bw == 1UL && r2 == a._p2)) ? 1UL : 0UL;
            ulong r3 = unchecked(a._p3 - b._p3 - bw);
            return new UInt256(r3, r2, r1, r0);
        }

        /// <summary>Multiplies two values (low 256 bits of the full product).</summary>
        public static UInt256 operator *(UInt256 a, UInt256 b)
        {
            // Split each into four 64-bit limbs: a = a3:a2:a1:a0, b = b3:b2:b1:b0
            ulong a0 = a._p0, a1 = a._p1, a2 = a._p2, a3 = a._p3;
            ulong b0 = b._p0, b1 = b._p1, b2 = b._p2, b3 = b._p3;

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
            if (b._p0 == 0 && b._p1 == 0 && b._p2 == 0 && b._p3 == 0) throw new DivideByZeroException();
            // Fast path: both fit in UInt128 - delegate to the native UInt128 divider.
            if (a._p2 == 0 && a._p3 == 0 && b._p2 == 0 && b._p3 == 0)
                return new UInt256(UInt128.Zero, new UInt128(a._p1, a._p0) / new UInt128(b._p1, b._p0));
            return UInt256Math.DivRem(a, b, out _);
        }

        /// <summary>Computes the remainder of division.</summary>
        /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
        public static UInt256 operator %(UInt256 a, UInt256 b)
        {
            if (b._p0 == 0 && b._p1 == 0 && b._p2 == 0 && b._p3 == 0) throw new DivideByZeroException();
            if (a._p2 == 0 && a._p3 == 0 && b._p2 == 0 && b._p3 == 0)
                return new UInt256(UInt128.Zero, new UInt128(a._p1, a._p0) % new UInt128(b._p1, b._p0));
            _ = UInt256Math.DivRem(a, b, out UInt256 rem);
            return rem;
        }

        /// <summary>Computes the bitwise AND of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator &(UInt256 a, UInt256 b) => new(a._p3 & b._p3, a._p2 & b._p2, a._p1 & b._p1, a._p0 & b._p0);
        /// <summary>Computes the bitwise OR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator |(UInt256 a, UInt256 b) => new(a._p3 | b._p3, a._p2 | b._p2, a._p1 | b._p1, a._p0 | b._p0);
        /// <summary>Computes the bitwise XOR of two values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator ^(UInt256 a, UInt256 b) => new(a._p3 ^ b._p3, a._p2 ^ b._p2, a._p1 ^ b._p1, a._p0 ^ b._p0);
        /// <summary>Computes the bitwise complement of the value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator ~(UInt256 a) => new(~a._p3, ~a._p2, ~a._p1, ~a._p0);

        /// <summary>Shifts the value left by the specified amount.</summary>
        public static UInt256 operator <<(UInt256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            var lo = new UInt128(a._p1, a._p0);
            var hi = new UInt128(a._p3, a._p2);
            if (b >= 128) return new UInt256(lo << (b - 128), UInt128.Zero);
            // b in 1..127
            return new UInt256((hi << b) | (lo >> (128 - b)), lo << b);
        }

        /// <summary>Shifts the value right by the specified amount (logical for unsigned).</summary>
        public static UInt256 operator >>(UInt256 a, int b)
        {
            b &= 255;
            if (b == 0) return a;
            var lo = new UInt128(a._p1, a._p0);
            var hi = new UInt128(a._p3, a._p2);
            if (b >= 128) return new UInt256(UInt128.Zero, hi >> (b - 128));
            return new UInt256(hi >> b, (lo >> b) | (hi << (128 - b)));
        }

        /// <summary>Unsigned (logical) right shift. Identical to <c>&gt;&gt;</c> for unsigned types.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt256 operator >>>(UInt256 a, int b) => a >> b;

        /// <summary>Determines whether two values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt256 a, UInt256 b) => a._p0 == b._p0 && a._p1 == b._p1 && a._p2 == b._p2 && a._p3 == b._p3;
        /// <summary>Determines whether two values are not equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt256 a, UInt256 b) => !(a == b);
        /// <summary>Determines whether the left operand is less than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(UInt256 a, UInt256 b)
            => a._p3 != b._p3 ? a._p3 < b._p3
             : a._p2 != b._p2 ? a._p2 < b._p2
             : a._p1 != b._p1 ? a._p1 < b._p1
             : a._p0 < b._p0;
        /// <summary>Determines whether the left operand is greater than the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(UInt256 a, UInt256 b) => b < a;
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
        public static explicit operator UInt128(UInt256 a) => new UInt128(a._p1, a._p0);
        /// <summary>Narrowing conversion to <see cref="ulong"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ulong(UInt256 a) => a._p0;
        /// <summary>Narrowing conversion to <see cref="uint"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(UInt256 a) => (uint)a._p0;
        /// <summary>Narrowing conversion to <see cref="ushort"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ushort(UInt256 a) => (ushort)a._p0;
        /// <summary>Narrowing conversion to <see cref="byte"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator byte(UInt256 a) => (byte)a._p0;

        /// <summary>Reinterprets the bit pattern as a signed <see cref="Int256"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int256(UInt256 a) => new(new UInt128(a._p3, a._p2), new UInt128(a._p1, a._p0));

        #endregion
    }
}
