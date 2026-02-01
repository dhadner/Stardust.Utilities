using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 64-bit unsigned integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct UInt64Be : IComparable, IComparable<UInt64Be>, IEquatable<UInt64Be>,
                              IFormattable, ISpanFormattable, IParsable<UInt64Be>, ISpanParsable<UInt64Be>
    {
        [FieldOffset(0)] internal UInt32Be hi;
        [FieldOffset(4)] internal UInt32Be lo;

        /// <summary>
        /// Creates a big-endian 64-bit unsigned integer from two UInt32Be values.
        /// </summary>
        public UInt64Be(UInt32Be hi, UInt32Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 64-bit unsigned integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 8 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public UInt64Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 8)
            {
                throw new ArgumentException("List is too short");
            }
            hi = new UInt32Be(bytes, offset);
            lo = new UInt32Be(bytes, offset + 4);
        }

        /// <summary>
        /// Creates a big-endian 64-bit unsigned integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 8 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 8)
            {
                throw new ArgumentException("Span must have at least 8 bytes", nameof(bytes));
            }
            hi = new UInt32Be(bytes);
            lo = new UInt32Be(bytes.Slice(4));
        }

        /// <summary>
        /// Creates a big-endian 64-bit unsigned integer from a native ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt64Be(ulong num)
        {
            hi = new UInt32Be((uint)(num >> 32));
            lo = new UInt32Be((uint)(num & 0xFFFFFFFF));
        }

        /// <summary>
        /// Creates a big-endian 64-bit unsigned integer from a native long.
        /// </summary>
        public UInt64Be(long num)
        {
            hi = new UInt32Be((uint)((ulong)num >> 32));
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
        /// Writes a native ulong as big-endian bytes to an IList.
        /// </summary>
        public static void ToBytes(ulong num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 56);
            bytes[offset + 1] = (byte)((num >> 48) & 0xff);
            bytes[offset + 2] = (byte)((num >> 40) & 0xff);
            bytes[offset + 3] = (byte)((num >> 32) & 0xff);
            bytes[offset + 4] = (byte)((num >> 24) & 0xff);
            bytes[offset + 5] = (byte)((num >> 16) & 0xff);
            bytes[offset + 6] = (byte)((num >> 8) & 0xff);
            bytes[offset + 7] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native ulong as big-endian bytes to a Span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(ulong num, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian ulong from a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new UInt64Be(source);
        }

        #region Operators

        public static UInt64Be operator +(UInt64Be a) => a;

        public static UInt64Be operator -(UInt64Be a) => new((ulong)-(long)(ulong)a);

        public static UInt64Be operator +(UInt64Be a, UInt64Be b)
            => new((ulong)a + (ulong)b);

        public static bool operator >(UInt64Be a, UInt64Be b)
           => (ulong)a > (ulong)b;

        public static bool operator <(UInt64Be a, UInt64Be b)
           => (ulong)a < (ulong)b;

        public static bool operator >=(UInt64Be a, UInt64Be b)
           => (ulong)a >= (ulong)b;

        public static bool operator <=(UInt64Be a, UInt64Be b)
           => (ulong)a <= (ulong)b;

        public static bool operator ==(UInt64Be a, UInt64Be b)
           => (ulong)a == (ulong)b;

        public static bool operator !=(UInt64Be a, UInt64Be b)
           => (ulong)a != (ulong)b;

        public static UInt64Be operator -(UInt64Be a, UInt64Be b)
            => new((ulong)a - (ulong)b);

        public static UInt64Be operator *(UInt64Be a, UInt64Be b)
            => new((ulong)a * (ulong)b);

        public static UInt64Be operator &(UInt64Be a, UInt64Be b)
            => new((ulong)a & (ulong)b);

        public static UInt64Be operator |(UInt64Be a, UInt64Be b)
            => new((ulong)a | (ulong)b);

        public static UInt64Be operator ^(UInt64Be a, UInt64Be b)
            => new((ulong)a ^ (ulong)b);

        public static UInt64Be operator ~(UInt64Be a)
            => new(~(ulong)a);

        public static UInt64Be operator >>(UInt64Be a, int b)
            => new((ulong)a >> b);

        public static UInt64Be operator <<(UInt64Be a, int b)
            => new((ulong)a << b);

        public static UInt64Be operator /(UInt64Be a, UInt64Be b)
        {
            ulong divisor = (ulong)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt64Be((ulong)a / divisor);
        }

        public static UInt64Be operator %(UInt64Be a, UInt64Be b)
        {
            ulong divisor = (ulong)b;
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }
            return new UInt64Be((ulong)a % divisor);
        }

        public static UInt64Be operator ++(UInt64Be a)
            => new((ulong)a + 1);

        public static UInt64Be operator --(UInt64Be a)
            => new((ulong)a - 1);

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UInt64Be a)
        {
            return ((ulong)(uint)a.hi << 32) | (uint)a.lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt64Be(ulong a) => new(a);

        public static explicit operator uint(UInt64Be a) => (uint)a.lo;
        public static explicit operator ushort(UInt64Be a) => (ushort)(uint)a.lo;
        public static explicit operator byte(UInt64Be a) => (byte)(uint)a.lo;
        public static explicit operator UInt32Be(UInt64Be a) => a.lo;
        public static explicit operator UInt16Be(UInt64Be a) => (UInt16Be)a.lo;

        public static implicit operator UInt64Be(UInt32Be a) => new(0, a);
        public static implicit operator UInt64Be(UInt16Be a) => new(0, (UInt32Be)a);

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to a UInt64Be.
        /// </summary>
        public static UInt64Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new UInt64Be(ulong.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to a UInt64Be using the specified format provider.
        /// </summary>
        public static UInt64Be Parse(string s, IFormatProvider? provider)
        {
            return new UInt64Be(ulong.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to a UInt64Be.
        /// </summary>
        public static UInt64Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new UInt64Be(ulong.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to a UInt64Be.
        /// </summary>
        public static bool TryParse(string? s, IFormatProvider? provider, out UInt64Be result)
        {
            if (ulong.TryParse(s, provider, out ulong value))
            {
                result = new UInt64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to a UInt64Be.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt64Be result)
        {
            if (ulong.TryParse(s, provider, out ulong value))
            {
                result = new UInt64Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(ulong)this:x16}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((ulong)this).ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((ulong)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((UInt64Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(UInt64Be other)
        {
            ulong a = (ulong)other;
            ulong b = (ulong)this;
            return b.CompareTo(a);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((UInt64Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(UInt64Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((ulong)this).GetHashCode();
        }

        #endregion
    }
}
