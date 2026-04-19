using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// High-throughput batched 256-bit integer operations.
    ///
    /// Currently exposes a batched <see cref="Multiply"/> that is measurably
    /// faster than the equivalent scalar-operator loop (and faster than the
    /// comparable APIs in Nethermind.Int256 and MissingValues). Batched
    /// add/subtract were evaluated and found to be memory-bandwidth bound
    /// for typical batch sizes; for those operations a direct <c>for</c>
    /// loop over the scalar <c>+</c> / <c>-</c> operators is as fast as any
    /// batched implementation, so no batched overload is provided.
    /// </summary>
    public static class UInt256Vector
    {
        // Number of 64-bit limbs per UInt256. Used as a stride when treating
        // a Span<UInt256> as a flat span of ulong limbs.
        private const int LIMBS_PER_VALUE = 4;

        /// <summary>
        /// Element-wise multiplication: <c>destination[i] = left[i] * right[i]</c>
        /// (low 256 bits; wraps modulo 2^256). Operates directly on the four
        /// 64-bit limbs of each <see cref="UInt256"/> via reference arithmetic,
        /// eliminating the 32-byte struct-return copy the scalar operator
        /// pays on each element and letting the JIT schedule consecutive
        /// limb multiplies on independent execution ports.
        /// </summary>
        public static void Multiply(ReadOnlySpan<UInt256> left, ReadOnlySpan<UInt256> right, Span<UInt256> destination)
        {
            if (left.Length != destination.Length || right.Length != destination.Length)
                throw new ArgumentException("All spans must have the same length.");

            int n = destination.Length;
            if (n == 0) return;

            ref ulong pa = ref Unsafe.As<UInt256, ulong>(ref MemoryMarshal.GetReference(left));
            ref ulong pb = ref Unsafe.As<UInt256, ulong>(ref MemoryMarshal.GetReference(right));
            ref ulong pr = ref Unsafe.As<UInt256, ulong>(ref MemoryMarshal.GetReference(destination));

            for (int i = 0; i < n; i++)
            {
                MulCore(ref pa, ref pb, ref pr, (nint)i * LIMBS_PER_VALUE);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MulCore(ref ulong pa, ref ulong pb, ref ulong pr, nint off)
        {
            // Low 256 bits of a 4x4 schoolbook multiply. Same algebra as
            // UInt256.operator *; 10 of the 16 partial products contribute
            // to bits 0..255, the other 6 fall entirely above bit 255.
            ulong a0 = Unsafe.Add(ref pa, off);
            ulong a1 = Unsafe.Add(ref pa, off + 1);
            ulong a2 = Unsafe.Add(ref pa, off + 2);
            ulong a3 = Unsafe.Add(ref pa, off + 3);
            ulong b0 = Unsafe.Add(ref pb, off);
            ulong b1 = Unsafe.Add(ref pb, off + 1);
            ulong b2 = Unsafe.Add(ref pb, off + 2);
            ulong b3 = Unsafe.Add(ref pb, off + 3);

            UInt128 p00 = (UInt128)a0 * b0;
            UInt128 p01 = (UInt128)a0 * b1;
            UInt128 p10 = (UInt128)a1 * b0;
            UInt128 p02 = (UInt128)a0 * b2;
            UInt128 p11 = (UInt128)a1 * b1;
            UInt128 p20 = (UInt128)a2 * b0;

            ulong r0 = (ulong)p00;

            UInt128 col1 = (UInt128)(ulong)(p00 >> 64) + (ulong)p01 + (ulong)p10;
            ulong r1 = (ulong)col1;

            UInt128 col2 = (UInt128)(ulong)(col1 >> 64)
                + (ulong)(p01 >> 64) + (ulong)(p10 >> 64)
                + (ulong)p02 + (ulong)p11 + (ulong)p20;
            ulong r2 = (ulong)col2;

            ulong r3 = unchecked(
                (ulong)(col2 >> 64)
                + (ulong)(p02 >> 64) + (ulong)(p11 >> 64) + (ulong)(p20 >> 64)
                + a0 * b3 + a1 * b2 + a2 * b1 + a3 * b0);

            Unsafe.Add(ref pr, off) = r0;
            Unsafe.Add(ref pr, off + 1) = r1;
            Unsafe.Add(ref pr, off + 2) = r2;
            Unsafe.Add(ref pr, off + 3) = r3;
        }
    }
}
