using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 16-bit unsigned integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct UInt16Be : IComparable, IComparable<UInt16Be>, IEquatable<UInt16Be>,
                              IFormattable, ISpanFormattable, IParsable<UInt16Be>, ISpanParsable<UInt16Be>
    {
        [FieldOffset(0)] internal byte hi;
        [FieldOffset(1)] internal byte lo;

        /// <summary>
        /// Creates a big-endian 16-bit unsigned integer from a native ushort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Be(ushort num)
        {
            hi = (byte)(num >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit unsigned integer from a native int.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value is outside ushort range.</exception>
        public UInt16Be(int num)
        {
            if (num > ushort.MaxValue || num < ushort.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit unsigned integer from a native uint.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value exceeds ushort.MaxValue.</exception>
        public UInt16Be(uint num)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(num, ushort.MaxValue);
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit unsigned integer from a byte array.
        /// </summary>
        /// <param name="bytes">Source byte array (must have at least 2 bytes from offset).</param>
        /// <param name="offset">Starting offset in the array.</param>
        /// <exception cref="ArgumentException">If array is too short.</exception>
        public UInt16Be(byte[] bytes, int offset = 0)
        {
            if (bytes.Length - offset < 2)
            {
                throw new ArgumentException("offset too large");
            }
            hi = bytes[offset + 0];
            lo = bytes[offset + 1];
        }

        /// <summary>
        /// Creates a big-endian 16-bit unsigned integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 2 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt16Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
            {
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            }
            hi = bytes[0];
            lo = bytes[1];
        }

        /// <summary>
        /// Writes the big-endian bytes to a byte array.
        /// </summary>
        public readonly void ToBytes(byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = hi;
            bytes[offset + 1] = lo;
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 2 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
            {
                throw new ArgumentException("Destination span must have at least 2 bytes", nameof(destination));
            }
            destination[0] = hi;
            destination[1] = lo;
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 2)
                return false;
            destination[0] = hi;
            destination[1] = lo;
            return true;
        }

        /// <summary>
        /// Writes a native ushort as big-endian bytes to a byte array.
        /// </summary>
        public static void ToBytes(ushort num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 8);
            bytes[offset + 1] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native ushort as big-endian bytes to a Span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(ushort num, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian ushort from a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new UInt16Be(source);
        }

        /// <summary>
        /// Parses a string to a UInt16Be.
        /// </summary>
        public static UInt16Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt16Be(ushort.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to a UInt16Be using the specified format provider.
        /// </summary>
        public static UInt16Be Parse(string s, IFormatProvider? provider)
        {
            return new UInt16Be(ushort.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to a UInt16Be.
        /// </summary>
        public static UInt16Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new UInt16Be(ushort.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to a UInt16Be.
        /// </summary>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt16Be result)
        {
            if (ushort.TryParse(s, provider, out ushort value))
            {
                result = new UInt16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to a UInt16Be.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt16Be result)
        {
            if (ushort.TryParse(s, provider, out ushort value))
            {
                result = new UInt16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(ushort)this:x4}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((ushort)this).ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((ushort)this).TryFormat(destination, out charsWritten, format, provider);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("obj is null");
            }
            return CompareTo((UInt16Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(UInt16Be other)
        {
            ushort a = (ushort)other;
            ushort b = (ushort)this;
            return b.CompareTo(a);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt16Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(UInt16Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((ushort)this).GetHashCode();
        }

        #region Operators

        public static UInt16Be operator +(UInt16Be a) => a;

        public static UInt16Be operator -(UInt16Be a) => new((ushort)-(ushort)a);

        public static UInt16Be operator +(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a + (ushort)b));

        public static UInt16Be operator -(UInt16Be a, UInt16Be b)
            => a + (-b);

        public static bool operator >(UInt16Be a, UInt16Be b)
           => (ushort)a > (ushort)b;

        public static bool operator <(UInt16Be a, UInt16Be b)
           => (ushort)a < (ushort)b;

        public static bool operator >=(UInt16Be a, UInt16Be b)
           => (ushort)a >= (ushort)b;

        public static bool operator <=(UInt16Be a, UInt16Be b)
           => (ushort)a <= (ushort)b;

        public static bool operator ==(UInt16Be a, UInt16Be b)
           => (ushort)a == (ushort)b;

        public static bool operator !=(UInt16Be a, UInt16Be b)
           => (ushort)a != (ushort)b;

        public static UInt16Be operator *(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a * (ushort)b));

        public static UInt16Be operator /(UInt16Be a, UInt16Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt16Be((ushort)((ushort)a / (ushort)b));
        }

        public static UInt16Be operator &(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a & (ushort)b));

        public static UInt16Be operator |(UInt16Be a, UInt16Be b)
            => new((ushort)((ushort)a | (ushort)b));

        public static UInt16Be operator >>(UInt16Be a, uint b)
            => new((ushort)((ushort)a >> (byte)b));

        /// <summary>
        /// Shift left loses bits on the left side.
        /// Must cast the argument to UInt32Be prior to shifting
        /// (i.e., use the UInt32Be version of this operator)
        /// if the intent is to widen the result.
        /// </summary>
        public static UInt16Be operator <<(UInt16Be a, uint b)
            => new((ushort)((ushort)a << (byte)b));

        public static UInt16Be operator %(UInt16Be a, uint b)
            => new((ushort)((ushort)a % b));

        public static UInt16Be operator ^(UInt16Be a, uint b)
            => new((ushort)((ushort)a ^ b));

        public static UInt16Be operator ~(UInt16Be a)
            => new((ushort)~(ushort)a);

        public static UInt16Be operator ++(UInt16Be a)
            => new((ushort)((ushort)a + 1));

        public static UInt16Be operator --(UInt16Be a)
            => new((ushort)((ushort)a - 1));

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(UInt16Be a) => (ushort)((ushort)(a.hi << 8) | a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UInt16Be a) => (ushort)a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt16Be(ushort a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Be(UInt16Be a) => new((ushort)a);

        public static explicit operator byte(UInt16Be a) => a.hi;

        public static explicit operator UInt16Be(uint a)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(a, ushort.MaxValue);
            return new UInt16Be((ushort)a);
        }

        public static explicit operator UInt16Be(int a) => (UInt16Be)(uint)a;

        #endregion
    }
}
