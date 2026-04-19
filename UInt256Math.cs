using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Stardust.Utilities
{
    /// <summary>
    /// Internal native 256-bit arithmetic helpers used by <see cref="UInt256"/> and
    /// <see cref="Int256"/> to avoid round-tripping through <see cref="System.Numerics.BigInteger"/>
    /// (which allocates a byte[] per operation).
    ///
    /// All routines operate on UInt256 as four little-endian 64-bit limbs
    /// (limb 0 = bits 63..0, limb 3 = bits 255..192).
    /// </summary>
    internal static class UInt256Math
    {
        // Largest power of 10 that fits in a ulong; used for base-10 conversion.
        internal const ulong DECIMAL_CHUNK = 10_000_000_000_000_000_000UL; // 10^19
        internal const int DECIMAL_CHUNK_DIGITS = 19;
        // Largest power of 10 that fits in a uint, used by the chunked Parse loop
        // to amortize the 4-limb multiply-add across 9 digits at a time.
        internal const ulong PARSE_CHUNK = 1_000_000_000UL; // 10^9
        internal const int PARSE_CHUNK_DIGITS = 9;

        // Max decimal digits of (2^256 - 1) is 78; add 1 for a potential sign.
        internal const int MAX_DECIMAL_DIGITS = 78;

        // Two-digit ASCII lookup table: TWO_DIGITS[i*2..i*2+1] is the two-character
        // zero-padded decimal representation of i (0..99). Used by FormatDecimal to
        // emit two digits per iteration, halving the per-digit division count.
        // This is the same technique used in System.Number.UInt32ToDecStr.
        private static ReadOnlySpan<char> TWO_DIGITS =>
            "00010203040506070809" +
            "10111213141516171819" +
            "20212223242526272829" +
            "30313233343536373839" +
            "40414243444546474849" +
            "50515253545556575859" +
            "60616263646566676869" +
            "70717273747576777879" +
            "80818283848586878889" +
            "90919293949596979899";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetLimbs(in UInt256 v, out ulong l0, out ulong l1, out ulong l2, out ulong l3)
        {
            l0 = (ulong)v._lo;
            l1 = (ulong)(v._lo >> 64);
            l2 = (ulong)v._hi;
            l3 = (ulong)(v._hi >> 64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt256 FromLimbs(ulong l0, ulong l1, ulong l2, ulong l3)
            => new UInt256(((UInt128)l3 << 64) | l2, ((UInt128)l1 << 64) | l0);

        /// <summary>
        /// 64x64 -> 128 widening unsigned multiply that prefers the BMI2
        /// <c>MULX</c> instruction (via <see cref="Bmi2.X64.MultiplyNoFlags"/>)
        /// over the default <see cref="Math.BigMul(ulong, ulong, out ulong)"/>.
        /// <c>MULX</c> does not set flags, so the JIT does not need to insert
        /// save/restore sequences around carry-propagation that follows, and
        /// the result lanes can live in independent registers. Falls back to
        /// <see cref="ArmBase.Arm64.MultiplyHigh"/> on ARM64 and to
        /// <see cref="Math.BigMul(ulong, ulong, out ulong)"/> elsewhere.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ulong Multiply64(ulong a, ulong b, out ulong low)
        {
            if (Bmi2.X64.IsSupported)
            {
                ulong lowLocal;
                ulong high = Bmi2.X64.MultiplyNoFlags(a, b, &lowLocal);
                low = lowLocal;
                return high;
            }
            if (ArmBase.Arm64.IsSupported)
            {
                low = a * b;
                return ArmBase.Arm64.MultiplyHigh(a, b);
            }
            return Math.BigMul(a, b, out low);
        }

        /// <summary>
        /// Adds <paramref name="a"/> and <paramref name="b"/> with an explicit
        /// carry-out flag (0 or 1). Used to build wider carry chains in the
        /// scalar multiply and add paths without allocating a <see cref="UInt128"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong AddWithCarry(ulong a, ulong b, out ulong carry)
        {
            ulong sum = unchecked(a + b);
            carry = sum < a ? 1UL : 0UL;
            return sum;
        }

        /// <summary>
        /// Fused multiply-add: returns the high and low 64 bits of
        /// <c>a * b + c</c>. The add cannot overflow the 128-bit result since
        /// <c>(2^64 - 1)^2 + (2^64 - 1) &lt; 2^128</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong BigMulAdd(ulong a, ulong b, ulong c, out ulong low)
        {
            ulong hi = Multiply64(a, b, out ulong lo);
            ulong sum = unchecked(lo + c);
            if (sum < lo) hi++;
            low = sum;
            return hi;
        }

        /// <summary>
        /// Computes the Möller-Granlund 2-by-1 reciprocal of a normalized
        /// (top-bit-set) <paramref name="d"/>, defined as
        /// <c>floor((2^128 - 1) / d) - 2^64</c>.
        /// The returned value enables replacing a 128/64 hardware division
        /// with a single 64x64 multiply plus a small fix-up, per
        /// Möller &amp; Granlund (2010), "Improved division by invariant integers".
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Reciprocal2By1(ulong d)
        {
            // (2^128 - 1) / d fits in 65 bits when d has its top bit set;
            // casting to ulong drops the single top bit (which equals 2^64).
            return (ulong)(~(UInt128)0 / d);
        }

        /// <summary>
        /// Divides the 128-bit value (<paramref name="u1"/>, <paramref name="u0"/>)
        /// by the normalized 64-bit divisor <paramref name="d"/> using the
        /// precomputed <paramref name="recip"/> (from <see cref="Reciprocal2By1"/>).
        /// The caller must ensure <paramref name="u1"/> &lt; <paramref name="d"/>;
        /// otherwise the quotient would not fit in 64 bits.
        /// </summary>
        /// <returns>
        /// The 64-bit quotient. The remainder is written to <paramref name="r"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Div2By1Recip(ulong u1, ulong u0, ulong d, ulong recip, out ulong r)
        {
            // Möller-Granlund 2.1: q' ≈ (u1:u0 + u1 * recip) / 2^64, then correct.
            UInt128 qprod = (UInt128)u1 * recip + ((UInt128)u1 << 64 | u0);
            ulong q1 = (ulong)(qprod >> 64) + 1UL;      // provisional quotient
            ulong rem = unchecked(u0 - q1 * d);         // provisional remainder (may be negative wrapped)

            // If the multiply-subtract wrapped around, we overshot by 1.
            if (rem > (ulong)qprod)                     // equivalent to "q1 too large"
            {
                q1--;
                rem = unchecked(rem + d);
            }
            // Final correction: at most one additional decrement.
            if (rem >= d)
            {
                q1++;
                rem -= d;
            }

            r = rem;
            return q1;
        }

        /// <summary>
        /// Divides <paramref name="a"/> by a single-limb (ulong) divisor and returns the
        /// quotient, writing the remainder (0..divisor-1) to <paramref name="remainder"/>.
        /// </summary>
        internal static UInt256 DivRemSmall(in UInt256 a, ulong divisor, out ulong remainder)
        {
            if (divisor == 0) throw new DivideByZeroException();
            GetLimbs(a, out ulong a0, out ulong a1, out ulong a2, out ulong a3);

            UInt128 r = 0;
            UInt128 d = (r << 64) | a3; ulong q3 = (ulong)(d / divisor); r = d % divisor;
            d = (r << 64) | a2;         ulong q2 = (ulong)(d / divisor); r = d % divisor;
            d = (r << 64) | a1;         ulong q1 = (ulong)(d / divisor); r = d % divisor;
            d = (r << 64) | a0;         ulong q0 = (ulong)(d / divisor); r = d % divisor;

            remainder = (ulong)r;
            return FromLimbs(q0, q1, q2, q3);
        }

        /// <summary>
        /// Multiplies <paramref name="a"/> by a single-limb ulong value and adds a ulong
        /// addend. Overflow (carry-out past bit 255) is indicated by a non-zero return
        /// flag; caller decides how to handle it.
        /// </summary>
        internal static bool TryMulAddSmall(in UInt256 a, ulong mul, ulong add, out UInt256 result)
        {
            GetLimbs(a, out ulong a0, out ulong a1, out ulong a2, out ulong a3);
            UInt128 p;
            p = (UInt128)a0 * mul + add;
            ulong r0 = (ulong)p;
            ulong c = (ulong)(p >> 64);
            p = (UInt128)a1 * mul + c;
            ulong r1 = (ulong)p;
            c = (ulong)(p >> 64);
            p = (UInt128)a2 * mul + c;
            ulong r2 = (ulong)p;
            c = (ulong)(p >> 64);
            p = (UInt128)a3 * mul + c;
            ulong r3 = (ulong)p;
            c = (ulong)(p >> 64);
            result = FromLimbs(r0, r1, r2, r3);
            return c == 0;
        }

        /// <summary>
        /// Full 256-bit unsigned division: returns quotient and remainder.
        /// Uses Knuth's Algorithm D (TAOCP Vol. 2 §4.3.1) on 64-bit limbs with
        /// 128-bit intermediate products, with fast paths for common cases.
        /// </summary>
        /// <exception cref="DivideByZeroException">If <paramref name="b"/> is zero.</exception>
        internal static UInt256 DivRem(in UInt256 a, in UInt256 b, out UInt256 remainder)
        {
            GetLimbs(b, out ulong b0, out ulong b1, out ulong b2, out ulong b3);

            // Determine divisor significant-limb count n (1..4).
            int n;
            if (b3 != 0) n = 4;
            else if (b2 != 0) n = 3;
            else if (b1 != 0) n = 2;
            else if (b0 != 0) n = 1;
            else throw new DivideByZeroException();

            GetLimbs(a, out ulong a0, out ulong a1, out ulong a2, out ulong a3);
            int m;
            if (a3 != 0) m = 4;
            else if (a2 != 0) m = 3;
            else if (a1 != 0) m = 2;
            else if (a0 != 0) m = 1;
            else { remainder = UInt256.Zero; return UInt256.Zero; }

            // Fast path: a < b by limb count.
            if (m < n) { remainder = a; return UInt256.Zero; }

            // Fast path: single-limb divisor.
            if (n == 1)
            {
                UInt256 q = DivRemSmall(a, b0, out ulong r);
                remainder = new UInt256(UInt128.Zero, r);
                return q;
            }

            // Specialization fast path: 3-limb divisor (the common EVM / crypto
            // case of a ~192-bit modulus). Fully unrolled, scalar locals only,
            // eliminates outer- and inner-loop overhead from the generic path.
            if (n == 3)
            {
                return DivRemN3(a0, a1, a2, a3, b0, b1, b2, out remainder);
            }

            // Knuth D on 64-bit limbs. Work arrays:
            // u: dividend (length m+1, low index = low limb)
            // v: divisor  (length n)
            // qd: quotient limbs (length m - n + 1)
            Span<ulong> u = stackalloc ulong[5];
            Span<ulong> v = stackalloc ulong[4];
            Span<ulong> qd = stackalloc ulong[4];

            u[0] = a0; u[1] = a1; u[2] = a2; u[3] = a3; u[4] = 0;
            v[0] = b0; v[1] = b1; v[2] = b2; v[3] = b3;

            // ref locals for bounds-check-free access in the hot inner loops.
            ref ulong uRef = ref u[0];
            ref ulong vRef = ref v[0];
            ref ulong qdRef = ref qd[0];

            // Step D1: Normalize so that v[n-1] has its top bit set.
            int shift = System.Numerics.BitOperations.LeadingZeroCount(v[n - 1]);
            if (shift != 0)
            {
                // Shift v left by 'shift' bits in-place.
                for (int i = n - 1; i > 0; i--)
                    v[i] = (v[i] << shift) | (v[i - 1] >> (64 - shift));
                v[0] <<= shift;
                // Shift u left by 'shift' bits, possibly writing into u[m].
                u[m] = u[m - 1] >> (64 - shift);
                for (int i = m - 1; i > 0; i--)
                    u[i] = (u[i] << shift) | (u[i - 1] >> (64 - shift));
                u[0] <<= shift;
            }

            ulong vn1 = Unsafe.Add(ref vRef, n - 1);
            ulong vn2 = Unsafe.Add(ref vRef, n - 2); // n >= 2 here

            // Precompute the 2-by-1 reciprocal of vn1 (normalized, top bit set).
            // Only used when X86Base.X64.DivRem is unavailable; on x64 .NET 8+
            // we call the hardware DIV instruction directly which is faster
            // than the reciprocal-mul fix-up sequence.
#if NET8_0_OR_GREATER
            bool hasHwDiv = X86Base.X64.IsSupported;
#else
            const bool hasHwDiv = false;
#endif
            ulong recip = hasHwDiv ? 0UL : Reciprocal2By1(vn1);

            // Step D2..D7: main loop. j goes from m-n down to 0.
            for (int j = m - n; j >= 0; j--)
            {
                // D3: Estimate qhat.
                ulong uHi = Unsafe.Add(ref uRef, j + n);
                ulong uLo = Unsafe.Add(ref uRef, j + n - 1);
                UInt128 qhat;
                UInt128 rhat;

                if (uHi < vn1)
                {
                    // Common case: qhat fits in 64 bits.
                    ulong q, r;
#if NET8_0_OR_GREATER
                    if (hasHwDiv)
                    {
                        // Hardware 128/64 DIV: (upper:lower) / divisor.
                        // X86Base.X64.DivRem(lower, upper, divisor) returns
                        // (quotient, remainder) as a ValueTuple.
                        (q, r) = X86Base.X64.DivRem(uLo, uHi, vn1);
                    }
                    else
#endif
                    {
                        // Reciprocal-multiplication fallback for non-x64 hosts
                        // and for .NET 7 (where the intrinsic is not exposed).
                        q = Div2By1Recip(uHi, uLo, vn1, recip, out r);
                    }
                    qhat = q;
                    rhat = r;
                }
                else
                {
                    // Overflow case: true qhat would exceed 2^64. Clamp to
                    // ulong.MaxValue; the while-loop below refines downward.
                    qhat = (UInt128)ulong.MaxValue;
                    UInt128 num = ((UInt128)uHi << 64) | uLo;
                    rhat = num - qhat * vn1;
                }

                while ((rhat >> 64) == 0 &&
                       qhat * vn2 > (((UInt128)(ulong)rhat << 64) | Unsafe.Add(ref uRef, j + n - 2)))
                {
                    qhat--;
                    rhat += vn1;
                }

                ulong qh = (ulong)qhat;

                // D4: u[j..j+n] -= qh * v[0..n-1], tracking a running borrow.
                // Manual 64-bit carry propagation (no Int128) using MULX via
                // Multiply64 for faster pipelining than Math.BigMul.
                ulong borrow = 0;
                for (int i = 0; i < n; i++)
                {
                    // p = qh * v[i] + borrow, via a 64x64->128 widening multiply
                    // and a carry-detecting 64-bit add.
                    ulong pHi = Multiply64(qh, Unsafe.Add(ref vRef, i), out ulong pLo);
                    ulong pLoPlusB = unchecked(pLo + borrow);
                    if (pLoPlusB < borrow) pHi++;         // carry from + borrow

                    // Subtract pLoPlusB from u[j+i]; any borrow-out bumps pHi
                    // (becomes the borrow-in for the next limb).
                    ref ulong uSlot = ref Unsafe.Add(ref uRef, j + i);
                    ulong oldU = uSlot;
                    ulong newU = unchecked(oldU - pLoPlusB);
                    uSlot = newU;
                    if (newU > oldU) pHi++;                // borrow from subtract
                    borrow = pHi;
                }
                // Final limb: subtract running borrow; record whether we
                // underflowed (signals the D6 add-back case below).
                ref ulong uJN = ref Unsafe.Add(ref uRef, j + n);
                ulong oldUn = uJN;
                ulong newUn = unchecked(oldUn - borrow);
                uJN = newUn;
                bool underflowed = newUn > oldUn;
                Unsafe.Add(ref qdRef, j) = qh;

                // D6: Add-back if we subtracted too much (rare; probability ~2/b).
                if (underflowed)
                {
                    Unsafe.Add(ref qdRef, j)--;
                    ulong addCarry = 0;
                    for (int i = 0; i < n; i++)
                    {
                        ref ulong uSlot = ref Unsafe.Add(ref uRef, j + i);
                        ulong ui = uSlot;
                        ulong vi = Unsafe.Add(ref vRef, i);
                        ulong sum = unchecked(ui + vi);
                        ulong c1 = sum < ui ? 1UL : 0UL;
                        ulong sum2 = unchecked(sum + addCarry);
                        ulong c2 = sum2 < sum ? 1UL : 0UL;
                        uSlot = sum2;
                        addCarry = c1 + c2;
                    }
                    uJN = unchecked(uJN + addCarry);
                }
            }

            // D8: Unnormalize the remainder.
            if (shift != 0)
            {
                for (int i = 0; i < n - 1; i++)
                    Unsafe.Add(ref uRef, i) = (Unsafe.Add(ref uRef, i) >> shift) | (Unsafe.Add(ref uRef, i + 1) << (64 - shift));
                Unsafe.Add(ref uRef, n - 1) >>= shift;
            }

            remainder = FromLimbs(
                n > 0 ? Unsafe.Add(ref uRef, 0) : 0UL,
                n > 1 ? Unsafe.Add(ref uRef, 1) : 0UL,
                n > 2 ? Unsafe.Add(ref uRef, 2) : 0UL,
                n > 3 ? Unsafe.Add(ref uRef, 3) : 0UL);

            // Quotient has m - n + 1 limbs (others zero).
            ulong q0x = 0, q1x = 0, q2x = 0, q3x = 0;
            int qlen = m - n + 1;
            if (qlen > 0) q0x = Unsafe.Add(ref qdRef, 0);
            if (qlen > 1) q1x = Unsafe.Add(ref qdRef, 1);
            if (qlen > 2) q2x = Unsafe.Add(ref qdRef, 2);
            if (qlen > 3) q3x = Unsafe.Add(ref qdRef, 3);
            return FromLimbs(q0x, q1x, q2x, q3x);
        }

        /// <summary>
        /// Specialized divide for the 3-limb-divisor case (~128..192-bit
        /// divisors, <c>b3 == 0</c> and <c>b2 != 0</c>). Fully unrolled with
        /// scalar locals throughout - no Span, no loops in the hot path - so
        /// the JIT can keep every limb in a register. This mirrors the
        /// structure of Nethermind's <c>DivideBy192Bits</c>, the benchmark
        /// workload's divisor shape, and is the shortest path from our
        /// general Knuth-D to hardware-bound performance.
        /// </summary>
        private static UInt256 DivRemN3(ulong a0, ulong a1, ulong a2, ulong a3,
                                        ulong b0, ulong b1, ulong b2,
                                        out UInt256 remainder)
        {
            // Dividend significant-limb count: 3 (if a3==0) or 4.
            int m = a3 != 0 ? 4 : 3;

            // D1: Normalize so the divisor's top limb has its top bit set.
            int shift = System.Numerics.BitOperations.LeadingZeroCount(b2);
            int rs = 64 - shift;

            ulong v0, v1, v2n;
            ulong u0, u1, u2, u3, u4;
            if (shift == 0)
            {
                v0 = b0; v1 = b1; v2n = b2;
                u0 = a0; u1 = a1; u2 = a2; u3 = a3; u4 = 0UL;
            }
            else
            {
                v0 = b0 << shift;
                v1 = (b1 << shift) | (b0 >> rs);
                v2n = (b2 << shift) | (b1 >> rs);
                u0 = a0 << shift;
                u1 = (a1 << shift) | (a0 >> rs);
                u2 = (a2 << shift) | (a1 >> rs);
                u3 = (a3 << shift) | (a2 >> rs);
                u4 = a3 >> rs;
            }

            // Reciprocal only used when the hardware DIV intrinsic is absent.
#if NET8_0_OR_GREATER
            bool hasHwDiv = X86Base.X64.IsSupported;
#else
            const bool hasHwDiv = false;
#endif
            ulong recip = hasHwDiv ? 0UL : Reciprocal2By1(v2n);

            ulong qd0 = 0UL, qd1 = 0UL;

            // Outer iteration j = 1 (only when m == 4): operates on limbs
            // (u1, u2, u3, u4). After the step, u4 is part of the remainder
            // tail and will be 0.
            if (m == 4)
            {
                qd1 = KnuthN3Step(ref u1, ref u2, ref u3, ref u4, v0, v1, v2n, recip, hasHwDiv);
            }

            // Outer iteration j = 0: operates on limbs (u0, u1, u2, u3).
            qd0 = KnuthN3Step(ref u0, ref u1, ref u2, ref u3, v0, v1, v2n, recip, hasHwDiv);

            // D8: Unnormalize the remainder (3 low limbs u0, u1, u2).
            if (shift != 0)
            {
                u0 = (u0 >> shift) | (u1 << rs);
                u1 = (u1 >> shift) | (u2 << rs);
                u2 >>= shift;
            }

            remainder = FromLimbs(u0, u1, u2, 0UL);
            return FromLimbs(qd0, qd1, 0UL, 0UL);
        }

        /// <summary>
        /// One Knuth-D step for a 3-limb divisor: divides the 4-limb window
        /// (<paramref name="u0"/>..<paramref name="u3"/>) by
        /// (<paramref name="v0"/>, <paramref name="v1"/>, <paramref name="v2n"/>),
        /// updating the window in place (top limb becomes the high part of
        /// the remainder) and returning the quotient digit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong KnuthN3Step(ref ulong u0, ref ulong u1, ref ulong u2, ref ulong u3,
                                         ulong v0, ulong v1, ulong v2n, ulong recip, bool hasHwDiv)
        {
            // D3: Estimate qhat.
            ulong qh;
            ulong rhat;
            bool rhatFitsInUlong;

            if (u3 < v2n)
            {
                // Common case: qhat fits in 64 bits.
#if NET8_0_OR_GREATER
                if (hasHwDiv)
                {
                    (qh, rhat) = X86Base.X64.DivRem(u2, u3, v2n);
                }
                else
#endif
                {
                    qh = Div2By1Recip(u3, u2, v2n, recip, out rhat);
                }
                rhatFitsInUlong = true;
            }
            else
            {
                // u3 == v2n (u3 > v2n is impossible after normalization).
                // True qhat would be 2^64 or 2^64 + a small value. Clamp to
                // ulong.MaxValue and compute rhat exactly as a 128-bit value.
                qh = ulong.MaxValue;
                // rhat = (u3:u2) - qh * v2n = u2 + v2n, possibly carrying.
                ulong sum = unchecked(u2 + v2n);
                if (sum < u2)
                {
                    // rhat overflowed 64 bits - the while-loop below is skipped.
                    rhat = sum;
                    rhatFitsInUlong = false;
                }
                else
                {
                    rhat = sum;
                    rhatFitsInUlong = true;
                }
            }

            // D3 refinement loop: at most 2 decrements per Knuth invariant.
            // While (qh * v1, rhat, u1) forms an over-estimate, decrement qh
            // and add v2n to rhat until rhat exceeds 2^64 (terminal condition)
            // or the inequality no longer holds.
            while (rhatFitsInUlong)
            {
                ulong pHi = Multiply64(qh, v1, out ulong pLo);
                if (pHi < rhat || (pHi == rhat && pLo <= u1)) break;
                qh--;
                ulong newRhat = unchecked(rhat + v2n);
                if (newRhat < rhat) { rhat = newRhat; break; }
                rhat = newRhat;
            }

            // D4: (u3:u2:u1:u0) -= qh * (v2n:v1:v0)
            // Running 64-bit borrow propagates up through three limbs.
            ulong borrow;
            {
                ulong pHi = Multiply64(qh, v0, out ulong pLo);
                ulong newU0 = unchecked(u0 - pLo);
                if (newU0 > u0) pHi++;
                u0 = newU0;
                borrow = pHi;
            }
            {
                ulong pHi = Multiply64(qh, v1, out ulong pLo);
                ulong pLoPlusB = unchecked(pLo + borrow);
                if (pLoPlusB < borrow) pHi++;
                ulong newU1 = unchecked(u1 - pLoPlusB);
                if (newU1 > u1) pHi++;
                u1 = newU1;
                borrow = pHi;
            }
            {
                ulong pHi = Multiply64(qh, v2n, out ulong pLo);
                ulong pLoPlusB = unchecked(pLo + borrow);
                if (pLoPlusB < borrow) pHi++;
                ulong newU2 = unchecked(u2 - pLoPlusB);
                if (newU2 > u2) pHi++;
                u2 = newU2;
                borrow = pHi;
            }
            // Final limb: subtract running borrow; underflow triggers D6.
            ulong oldU3 = u3;
            ulong newU3 = unchecked(oldU3 - borrow);
            u3 = newU3;

            // D6: Add-back if we over-subtracted (probability ~2/2^64, so rare
            // but must be handled for correctness on adversarial inputs).
            if (newU3 > oldU3)
            {
                qh--;
                ulong addCarry;
                {
                    ulong sum = unchecked(u0 + v0);
                    addCarry = sum < u0 ? 1UL : 0UL;
                    u0 = sum;
                }
                {
                    ulong sum = unchecked(u1 + v1);
                    ulong c1 = sum < u1 ? 1UL : 0UL;
                    ulong sum2 = unchecked(sum + addCarry);
                    ulong c2 = sum2 < sum ? 1UL : 0UL;
                    u1 = sum2;
                    addCarry = c1 + c2;
                }
                {
                    ulong sum = unchecked(u2 + v2n);
                    ulong c1 = sum < u2 ? 1UL : 0UL;
                    ulong sum2 = unchecked(sum + addCarry);
                    ulong c2 = sum2 < sum ? 1UL : 0UL;
                    u2 = sum2;
                    addCarry = c1 + c2;
                }
                u3 = unchecked(u3 + addCarry);
            }

            return qh;
        }

        /// <summary>
        /// Formats the magnitude of a <see cref="UInt256"/> as a base-10 string.
        /// If <paramref name="negative"/> is true, a leading '-' is prepended.
        /// </summary>
        internal static string FormatDecimal(in UInt256 magnitude, bool negative)
        {
            if (magnitude._hi == UInt128.Zero && magnitude._lo == UInt128.Zero)
                return "0";

            Span<char> buf = stackalloc char[MAX_DECIMAL_DIGITS + 1];
            // Pre-compute byte-level bases so the inner loop does one 32-bit
            // read+write per digit pair without per-iteration Unsafe.As casts.
            ref byte bufBase = ref System.Runtime.CompilerServices.Unsafe.As<char, byte>(
                ref System.Runtime.InteropServices.MemoryMarshal.GetReference(buf));
            ref byte tdBase = ref System.Runtime.CompilerServices.Unsafe.As<char, byte>(
                ref System.Runtime.InteropServices.MemoryMarshal.GetReference(TWO_DIGITS));
            ref char bufRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(buf);
            int pos = buf.Length;

            // Work directly on four 64-bit limbs to avoid repeated UInt256 struct
            // copies and UInt128 pack/unpack on every chunk. Each outer iteration
            // extracts the next 19 decimal digits in O(1) long-divisions.
            ulong a0 = (ulong)magnitude._lo;
            ulong a1 = (ulong)(magnitude._lo >> 64);
            ulong a2 = (ulong)magnitude._hi;
            ulong a3 = (ulong)(magnitude._hi >> 64);

            while ((a0 | a1 | a2 | a3) != 0UL)
            {
                // 4-limb / 1-limb division by the constant DECIMAL_CHUNK.
                // On x64, X86Base.X64.DivRem emits a single hardware DIV per
                // limb (~20 cycles), avoiding the managed UInt128.op_Division
                // software path (which is itself a schoolbook division loop).
                ulong r = 0;
                ulong chunk;
#if NET8_0_OR_GREATER
                if (X86Base.X64.IsSupported)
                {
                    (a3, r) = X86Base.X64.DivRem(a3, r, DECIMAL_CHUNK);
                    (a2, r) = X86Base.X64.DivRem(a2, r, DECIMAL_CHUNK);
                    (a1, r) = X86Base.X64.DivRem(a1, r, DECIMAL_CHUNK);
                    (a0, r) = X86Base.X64.DivRem(a0, r, DECIMAL_CHUNK);
                    chunk = r;
                }
                else
#endif
                {
                    // Portable fallback: UInt128 division by a 64-bit constant.
                    UInt128 d;
                    UInt128 r128 = 0;
                    d = (r128 << 64) | a3; a3 = (ulong)(d / DECIMAL_CHUNK); r128 = d % DECIMAL_CHUNK;
                    d = (r128 << 64) | a2; a2 = (ulong)(d / DECIMAL_CHUNK); r128 = d % DECIMAL_CHUNK;
                    d = (r128 << 64) | a1; a1 = (ulong)(d / DECIMAL_CHUNK); r128 = d % DECIMAL_CHUNK;
                    d = (r128 << 64) | a0; a0 = (ulong)(d / DECIMAL_CHUNK); r128 = d % DECIMAL_CHUNK;
                    chunk = (ulong)r128;
                }

                bool last = (a0 | a1 | a2 | a3) == 0UL;
                if (!last)
                {
                    // Emit all 19 digits with leading zeros, two at a time via
                    // the TWO_DIGITS lookup (table of 100 two-character pairs).
                    // 19 is odd: emit the final (highest) digit singly at end.
                    for (int k = 0; k < 9; k++)
                    {
                        ulong q = chunk / 100UL;
                        uint rem = (uint)(chunk - q * 100UL);
                        chunk = q;
                        pos -= 2;
                        // Read 4 bytes (two UTF-16 chars) and store them at
                        // buf[pos..pos+2] in one 32-bit move.
                        uint pair = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tdBase, (nuint)(rem * 4)));
                        System.Runtime.CompilerServices.Unsafe.WriteUnaligned(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref bufBase, (nuint)((uint)pos * 2)),
                            pair);
                    }
                    pos--;
                    System.Runtime.CompilerServices.Unsafe.Add(ref bufRef, pos) = (char)('0' + (uint)chunk);
                }
                else
                {
                    // Final (most-significant) chunk: emit only significant digits.
                    while (chunk >= 100UL)
                    {
                        ulong q = chunk / 100UL;
                        uint rem = (uint)(chunk - q * 100UL);
                        chunk = q;
                        pos -= 2;
                        uint pair = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tdBase, (nuint)(rem * 4)));
                        System.Runtime.CompilerServices.Unsafe.WriteUnaligned(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref bufBase, (nuint)((uint)pos * 2)),
                            pair);
                    }
                    if (chunk >= 10UL)
                    {
                        uint rem = (uint)chunk;
                        pos -= 2;
                        uint pair = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tdBase, (nuint)(rem * 4)));
                        System.Runtime.CompilerServices.Unsafe.WriteUnaligned(
                            ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref bufBase, (nuint)((uint)pos * 2)),
                            pair);
                    }
                    else
                    {
                        pos--;
                        System.Runtime.CompilerServices.Unsafe.Add(ref bufRef, pos) = (char)('0' + (uint)chunk);
                    }
                }
            }

            if (negative)
            {
                pos--;
                System.Runtime.CompilerServices.Unsafe.Add(ref bufRef, pos) = '-';
            }
            return new string(buf[pos..]);
        }

        /// <summary>
        /// Parses a base-10 string into an unsigned <see cref="UInt256"/> magnitude.
        /// Accepts optional leading '+' or '-' and sets <paramref name="isNegative"/>
        /// accordingly. On any malformed input returns false; when the input is
        /// syntactically valid but exceeds 256 bits, returns false with
        /// <paramref name="overflow"/> set to true so the caller can translate it
        /// into <see cref="OverflowException"/> rather than <see cref="FormatException"/>.
        /// </summary>
        internal static bool TryParseDecimal(ReadOnlySpan<char> s, out UInt256 magnitude, out bool isNegative, out bool overflow)
        {
            magnitude = UInt256.Zero;
            isNegative = false;
            overflow = false;

            s = s.Trim();
            if (s.IsEmpty) return false;

            int i = 0;
            if (s[0] == '+') i = 1;
            else if (s[0] == '-') { isNegative = true; i = 1; }
            if (i >= s.Length) return false;

            // Strip leading zeros but keep at least one digit to parse.
            while (i < s.Length - 1 && s[i] == '0') i++;

            // Chunked parse: pack up to 9 decimal digits at a time into a single
            // ulong, then fold the chunk into the 4-limb state with one
            // multiply-add per chunk instead of per digit. For a 77-digit input
            // this reduces the 4-limb multiply count from 77 to ~9.
            ulong a0 = 0, a1 = 0, a2 = 0, a3 = 0;
            int len = s.Length;

            // First (possibly short) chunk aligns the remaining length to a
            // multiple of PARSE_CHUNK_DIGITS.
            int firstLen = (len - i) % PARSE_CHUNK_DIGITS;
            if (firstLen > 0)
            {
                ulong chunk = 0;
                int end = i + firstLen;
                for (; i < end; i++)
                {
                    uint d = (uint)(s[i] - '0');
                    if (d > 9) return false;
                    chunk = chunk * 10UL + d;
                }
                // Multiplier for the first chunk is 10^firstLen, which fits in ulong.
                ulong mul = Pow10(firstLen);
                MulAddInPlace(ref a0, ref a1, ref a2, ref a3, mul, chunk, out bool ov);
                if (ov) { overflow = true; return false; }
            }

            // Full 9-digit chunks.
            while (i < len)
            {
                ulong chunk = 0;
                int end = i + PARSE_CHUNK_DIGITS;
                for (; i < end; i++)
                {
                    uint d = (uint)(s[i] - '0');
                    if (d > 9) return false;
                    chunk = chunk * 10UL + d;
                }
                MulAddInPlace(ref a0, ref a1, ref a2, ref a3, PARSE_CHUNK, chunk, out bool ov);
                if (ov) { overflow = true; return false; }
            }

            magnitude = FromLimbs(a0, a1, a2, a3);
            return true;
        }

        /// <summary>Returns 10^n for 0 &lt;= n &lt;= 9 (fits in a <see cref="ulong"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Pow10(int n) => n switch
        {
            0 => 1UL,
            1 => 10UL,
            2 => 100UL,
            3 => 1_000UL,
            4 => 10_000UL,
            5 => 100_000UL,
            6 => 1_000_000UL,
            7 => 10_000_000UL,
            8 => 100_000_000UL,
            _ => 1_000_000_000UL,
        };

        /// <summary>
        /// In-place four-limb multiply-add: (a3:a2:a1:a0) = (a3:a2:a1:a0) * mul + add.
        /// Sets <paramref name="overflow"/> if the result exceeds 256 bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MulAddInPlace(ref ulong a0, ref ulong a1, ref ulong a2, ref ulong a3,
                                          ulong mul, ulong add, out bool overflow)
        {
            UInt128 p = (UInt128)a0 * mul + add;
            a0 = (ulong)p;
            ulong carry = (ulong)(p >> 64);
            p = (UInt128)a1 * mul + carry;
            a1 = (ulong)p;
            carry = (ulong)(p >> 64);
            p = (UInt128)a2 * mul + carry;
            a2 = (ulong)p;
            carry = (ulong)(p >> 64);
            p = (UInt128)a3 * mul + carry;
            a3 = (ulong)p;
            overflow = (ulong)(p >> 64) != 0UL;
        }
    }
}
