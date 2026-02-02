using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 16-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct Int16Be : IComparable, IComparable<Int16Be>, IEquatable<Int16Be>,
                             IFormattable, ISpanFormattable, IParsable<Int16Be>, ISpanParsable<Int16Be>
    {
        [FieldOffset(0)] internal byte hi;
        [FieldOffset(1)] internal byte lo;

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native short.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Be(short num)
        {
            hi = (byte)(num >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 2 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int16Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 2)
            {
                throw new ArgumentException("List is too short");
            }
            hi = bytes[offset + 0];
            lo = bytes[offset + 1];
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 2 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int16Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 2)
            {
                throw new ArgumentException("Span must have at least 2 bytes", nameof(bytes));
            }
            hi = bytes[0];
            lo = bytes[1];
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native uint.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value exceeds short.MaxValue.</exception>
        public Int16Be(uint num)
        {
            if (num > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
        }

        /// <summary>
        /// Creates a big-endian 16-bit signed integer from a native int.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value exceeds short.MaxValue.</exception>
        public Int16Be(int num)
        {
            if (num > short.MaxValue || num < short.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            hi = (byte)((num & 0xffff) >> 8);
            lo = (byte)(num & 0xff);
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
        /// Writes a native short as big-endian bytes to a byte array.
        /// </summary>
        public static void ToBytes(short num, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(num >> 8);
            bytes[offset + 1] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native short as big-endian bytes to a Span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(short num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt16BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian short from a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int16Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int16Be(source);
        }

        #region Operators

        public static Int16Be operator +(Int16Be a) => a;

        public static Int16Be operator -(Int16Be a) => new((short)-(short)a);

        public static Int16Be operator +(Int16Be a, Int16Be b)
            => new((short)((short)a + (short)b));

        public static Int16Be operator -(Int16Be a, Int16Be b)
            => a + (-b);

        public static bool operator >(Int16Be a, Int16Be b)
           => (short)a > (short)b;

        public static bool operator <(Int16Be a, Int16Be b)
           => (short)a < (short)b;

        public static bool operator >=(Int16Be a, Int16Be b)
           => (short)a >= (short)b;

        public static bool operator <=(Int16Be a, Int16Be b)
           => (short)a <= (short)b;

        public static bool operator ==(Int16Be a, Int16Be b)
           => (short)a == (short)b;

        public static bool operator !=(Int16Be a, Int16Be b)
           => (short)a != (short)b;

        public static Int16Be operator *(Int16Be a, Int16Be b)
            => new((short)((short)a * (short)b));

        public static Int16Be operator /(Int16Be a, Int16Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new((short)((short)a / (short)b));
        }

        public static Int16Be operator &(Int16Be a, Int16Be b)
            => new((short)((short)a & (short)b));

        public static Int16Be operator |(Int16Be a, Int16Be b)
            => new((short)((short)a | (short)b));

        public static Int16Be operator >>(Int16Be a, int b)
            => new((short)((short)a >> (byte)b));

        /// <summary>
        /// Shift left loses bits on the left side.
        /// Must cast the argument to Int32Be prior to shifting
        /// (i.e., use the Int32Be version of this operator)
        /// if the intent is to widen the result.
        /// </summary>
        public static Int16Be operator <<(Int16Be a, int b)
            => new((short)((short)a << (byte)b));

        public static Int16Be operator %(Int16Be a, int b)
            => new((short)((short)a % b));

        public static Int16Be operator ^(Int16Be a, int b)
            => new((short)((short)a ^ b));

        public static Int16Be operator ~(Int16Be a)
            => new((short)~(short)a);

        public static Int16Be operator ++(Int16Be a)
            => new((short)((short)a + 1));

        public static Int16Be operator --(Int16Be a)
            => new((short)((short)a - 1));

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator short(Int16Be a) => (short)((a.hi << 8) | a.lo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int16Be a) => (short)a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int16Be(short a) => new(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt32Be(Int16Be a) => new((short)a);

        public static explicit operator byte(Int16Be a) => a.hi;

        public static explicit operator Int16Be(uint a)
        {
            if (a > short.MaxValue || (int)a < short.MinValue)
            {
                throw new ArgumentOutOfRangeException(nameof(a));
            }
            return new Int16Be((short)a);
        }

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int16Be.
        /// </summary>
        public static Int16Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int16Be(short.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int16Be using the specified format provider.
        /// </summary>
        public static Int16Be Parse(string s, IFormatProvider? provider)
        {
            return new Int16Be(short.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int16Be.
        /// </summary>
        public static Int16Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int16Be(short.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int16Be.
        /// </summary>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int16Be result)
        {
            if (short.TryParse(s, provider, out short value))
            {
                result = new Int16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int16Be.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int16Be result)
        {
            if (short.TryParse(s, provider, out short value))
            {
                result = new Int16Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{(short)this:x4}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((short)this).ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((short)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((Int16Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(Int16Be other)
        {
            short a = (short)other;
            short b = (short)this;
            return b.CompareTo(a);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int16Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Int16Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((short)this).GetHashCode();
        }

        #endregion
    }
}
