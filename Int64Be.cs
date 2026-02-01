using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 64-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Int64Be : IComparable, IComparable<Int64Be>, IEquatable<Int64Be>,
                             IFormattable, ISpanFormattable, IParsable<Int64Be>, ISpanParsable<Int64Be>
    {
        [FieldOffset(0)] internal UInt32Be hi;
        [FieldOffset(4)] internal UInt32Be lo;

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from two UInt32Be values.
        /// </summary>
        public Int64Be(UInt32Be hi, UInt32Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 8 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int64Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 8)
            {
                throw new ArgumentException("List is too short");
            }
            hi = new UInt32Be(bytes, offset);
            lo = new UInt32Be(bytes, offset + 4);
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 8 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
            {
                throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            }
            hi = new UInt32Be(bytes);
            lo = new UInt32Be(bytes.Slice(4));
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a native long.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int64Be(long num)
        {
            hi = new UInt32Be((uint)((ulong)num >> 32));
            lo = new UInt32Be((uint)(num & 0xFFFFFFFF));
        }

        /// <summary>
        /// Creates a big-endian 64-bit signed integer from a native ulong.
        /// </summary>
        public Int64Be(ulong num)
        {
            hi = new UInt32Be((uint)(num >> 32));
            lo = new UInt32Be((uint)(num & 0xFFFFFFFF));
        }

        /// <summary>
        /// Writes the big-endian bytes to an IList.
        /// </summary>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            hi.ToBytes(bytes, offset);
            lo.ToBytes(bytes, offset + 4);
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 8 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
            {
                throw new ArgumentException("Destination span must have at least 8 bytes", nameof(destination));
            }
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(4));
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 8)
                return false;
            hi.WriteTo(destination);
            lo.WriteTo(destination.Slice(4));
            return true;
        }

        /// <summary>
        /// Writes a native long as big-endian bytes to an IList.
        /// </summary>
        public static void ToBytes(long num, IList<byte> bytes, int offset = 0)
        {
            ulong unum = (ulong)num;
            bytes[offset + 0] = (byte)(unum >> 56);
            bytes[offset + 1] = (byte)((unum >> 48) & 0xff);
            bytes[offset + 2] = (byte)((unum >> 40) & 0xff);
            bytes[offset + 3] = (byte)((unum >> 32) & 0xff);
            bytes[offset + 4] = (byte)((unum >> 24) & 0xff);
            bytes[offset + 5] = (byte)((unum >> 16) & 0xff);
            bytes[offset + 6] = (byte)((unum >> 8) & 0xff);
            bytes[offset + 7] = (byte)(unum & 0xff);
        }

        /// <summary>
        /// Writes a native long as big-endian bytes to a Span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(long num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt64BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian long from a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int64Be(source);
        }

        #region Operators

        public static Int64Be operator +(Int64Be a) => a;

        public static Int64Be operator -(Int64Be a) => new(-(long)a);

        public static Int64Be operator +(Int64Be a, Int64Be b)
            => new((long)a + (long)b);

        public static bool operator >(Int64Be a, Int64Be b)
           => (long)a > (long)b;

        public static bool operator <(Int64Be a, Int64Be b)
           => (long)a < (long)b;

        public static bool operator >=(Int64Be a, Int64Be b)
           => (long)a >= (long)b;

        public static bool operator <=(Int64Be a, Int64Be b)
           => (long)a <= (long)b;

        public static bool operator ==(Int64Be a, Int64Be b)
           => (long)a == (long)b;

        public static bool operator !=(Int64Be a, Int64Be b)
           => (long)a != (long)b;

        public static Int64Be operator -(Int64Be a, Int64Be b)
            => new((long)a - (long)b);

        public static Int64Be operator *(Int64Be a, Int64Be b)
            => new((long)a * (long)b);

        public static Int64Be operator &(Int64Be a, Int64Be b)
            => new((long)a & (long)b);

        public static Int64Be operator |(Int64Be a, Int64Be b)
            => new((long)a | (long)b);

        public static Int64Be operator ^(Int64Be a, Int64Be b)
            => new((long)a ^ (long)b);

        public static Int64Be operator ~(Int64Be a)
            => new(~(long)a);

        public static Int64Be operator >>(Int64Be a, int b)
            => new((long)a >> b);

        public static Int64Be operator <<(Int64Be a, int b)
            => new((long)a << b);

        public static Int64Be operator /(Int64Be a, Int64Be b)
        {
            long divisor = (long)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int64Be((long)a / divisor);
        }

        public static Int64Be operator %(Int64Be a, Int64Be b)
        {
            long divisor = (long)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int64Be((long)a % divisor);
        }

        public static Int64Be operator ++(Int64Be a)
            => new((long)a + 1);

        public static Int64Be operator --(Int64Be a)
            => new((long)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Int64Be a)
        {
            return (long)(((ulong)(uint)a.hi << 32) | (uint)a.lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int64Be(long a) => new(a);

        public static explicit operator int(Int64Be a) => (int)(long)a;
        public static explicit operator short(Int64Be a) => (short)(long)a;
        public static explicit operator sbyte(Int64Be a) => (sbyte)(long)a;
        public static explicit operator Int32Be(Int64Be a) => new((int)(long)a);
        public static explicit operator Int16Be(Int64Be a) => new((short)(long)a);

        // Sign-extending conversions from smaller types
        public static implicit operator Int64Be(Int32Be a) => new((int)a);
        public static implicit operator Int64Be(Int16Be a) => new((short)a);

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int64Be.
        /// </summary>
        public static Int64Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int64Be(long.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int64Be using the specified format provider.
        /// </summary>
        public static Int64Be Parse(string s, IFormatProvider? provider)
        {
            return new Int64Be(long.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int64Be.
        /// </summary>
        public static Int64Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int64Be(long.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int64Be.
        /// </summary>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int64Be result)
        {
            if (long.TryParse(s, provider, out long value))
            {
                result = new Int64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int64Be.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int64Be result)
        {
            if (long.TryParse(s, provider, out long value))
            {
                result = new Int64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(ulong)(long)this:x16}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((long)this).ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((long)this).TryFormat(destination, out charsWritten, format, provider);
        }

        #endregion

        #region Comparison and Equality

        /// <inheritdoc/>
        public readonly int CompareTo(object? obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Object is null");
            }
            return CompareTo((Int64Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(Int64Be other)
        {
            long a = (long)other;
            long b = (long)this;
            return b.CompareTo(a);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int64Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Int64Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((long)this).GetHashCode();
        }

        #endregion
    }
}
