using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    /// <summary>
    /// Big-endian 32-bit signed integer type.
    /// Stores bytes in network byte order (most significant byte first).
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Int32Be : IComparable, IComparable<Int32Be>, IEquatable<Int32Be>,
                             IFormattable, ISpanFormattable, IParsable<Int32Be>, ISpanParsable<Int32Be>
    {
        [FieldOffset(0)] internal UInt16Be hi;
        [FieldOffset(2)] internal UInt16Be lo;

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from two UInt16Be values.
        /// </summary>
        public Int32Be(UInt16Be hi, UInt16Be lo)
        {
            this.hi = hi;
            this.lo = lo;
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from an IList of bytes.
        /// </summary>
        /// <param name="bytes">Source bytes (must have at least 4 bytes from offset).</param>
        /// <param name="offset">Starting offset.</param>
        /// <exception cref="ArgumentException">If list is too short.</exception>
        public Int32Be(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count - offset < 4)
            {
                throw new ArgumentException("List is too short");
            }
            hi.hi = bytes[offset + 0];
            hi.lo = bytes[offset + 1];
            lo.hi = bytes[offset + 2];
            lo.lo = bytes[offset + 3];
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from a ReadOnlySpan.
        /// </summary>
        /// <param name="bytes">Source span (must have at least 4 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Be(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 4)
            {
                throw new ArgumentException("Span must have at least 4 bytes", nameof(bytes));
            }
            hi.hi = bytes[0];
            hi.lo = bytes[1];
            lo.hi = bytes[2];
            lo.lo = bytes[3];
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from a native uint.
        /// </summary>
        public Int32Be(uint num)
        {
            hi = (UInt16Be)(ushort)(num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        /// <summary>
        /// Creates a big-endian 32-bit signed integer from a native int.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int32Be(int num)
        {
            hi = (UInt16Be)(ushort)((uint)num >> 16);
            lo = (UInt16Be)(ushort)(num & 0xffff);
        }

        /// <summary>
        /// Writes the big-endian bytes to an IList.
        /// </summary>
        public readonly void ToBytes(IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = hi.hi;
            bytes[offset + 1] = hi.lo;
            bytes[offset + 2] = lo.hi;
            bytes[offset + 3] = lo.lo;
        }

        /// <summary>
        /// Writes the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span (must have at least 4 bytes).</param>
        /// <exception cref="ArgumentException">If span is too short.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
            {
                throw new ArgumentException("Destination span must have at least 4 bytes", nameof(destination));
            }
            destination[0] = hi.hi;
            destination[1] = hi.lo;
            destination[2] = lo.hi;
            destination[3] = lo.lo;
        }

        /// <summary>
        /// Tries to write the big-endian bytes to a Span.
        /// </summary>
        /// <param name="destination">Destination span.</param>
        /// <returns>True if successful, false if span is too short.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryWriteTo(Span<byte> destination)
        {
            if (destination.Length < 4)
                return false;
            destination[0] = hi.hi;
            destination[1] = hi.lo;
            destination[2] = lo.hi;
            destination[3] = lo.lo;
            return true;
        }

        /// <summary>
        /// Writes a native int as big-endian bytes to an IList.
        /// </summary>
        public static void ToBytes(int num, IList<byte> bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)((uint)num >> 24);
            bytes[offset + 1] = (byte)((num >> 16) & 0xff);
            bytes[offset + 2] = (byte)((num >> 8) & 0xff);
            bytes[offset + 3] = (byte)(num & 0xff);
        }

        /// <summary>
        /// Writes a native int as big-endian bytes to a Span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTo(int num, Span<byte> destination)
        {
            BinaryPrimitives.WriteInt32BigEndian(destination, num);
        }

        /// <summary>
        /// Reads a big-endian int from a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Be ReadFrom(ReadOnlySpan<byte> source)
        {
            return new Int32Be(source);
        }

        #region Operators

        public static Int32Be operator +(Int32Be a) => a;

        public static Int32Be operator -(Int32Be a) => new(-(int)a);

        public static Int32Be operator +(Int32Be a, Int32Be b)
            => new((int)a + (int)b);

        public static bool operator >(Int32Be a, Int32Be b)
           => (int)a > (int)b;

        public static bool operator <(Int32Be a, Int32Be b)
           => (int)a < (int)b;

        public static bool operator >=(Int32Be a, Int32Be b)
           => (int)a >= (int)b;

        public static bool operator <=(Int32Be a, Int32Be b)
           => (int)a <= (int)b;

        public static bool operator ==(Int32Be a, Int32Be b)
           => (int)a == (int)b;

        public static bool operator !=(Int32Be a, Int32Be b)
           => (int)a != (int)b;

        public static Int32Be operator -(Int32Be a, Int32Be b)
            => a + (-b);

        public static Int32Be operator *(Int32Be a, Int32Be b)
            => new((int)a * (int)b);

        public static Int32Be operator &(Int32Be a, Int32Be b)
            => new((int)a & (int)b);

        public static Int32Be operator |(Int32Be a, Int32Be b)
            => new((int)a | (int)b);

        public static Int32Be operator >>(Int32Be a, Int32Be b)
            => new((int)a >> b.lo.lo);

        public static Int32Be operator <<(Int32Be a, Int32Be b)
            => new((int)a << b.lo.lo);

        public static Int32Be operator /(Int32Be a, Int32Be b)
        {
            if (b.hi == 0 && b.lo == 0)
            {
                throw new DivideByZeroException();
            }
            return new Int32Be((int)a / (int)b);
        }

        #endregion

        #region Conversions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Int32Be a) => a.hi.hi << 24 | a.hi.lo << 16 | a.lo.hi << 8 | a.lo.lo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Int32Be(int a) => new(a);

        public static explicit operator ushort(Int32Be a) => (ushort)a.hi;
        public static explicit operator byte(Int32Be a) => a.hi.hi;

        public static explicit operator Int32Be(Int16Be v)
        {
            // Sign-extend Int16Be to Int32Be
            short value = (short)v;
            return new Int32Be(value);
        }

        #endregion

        #region Parsing and Formatting

        /// <summary>
        /// Parses a string to an Int32Be.
        /// </summary>
        public static Int32Be Parse(string s, NumberStyles style = NumberStyles.Integer)
        {
            if (style == NumberStyles.HexNumber)
            {
                s = s.ToLower().Replace("0x", "");
            }
            return new Int32Be(int.Parse(s, style));
        }

        /// <summary>
        /// Parses a string to an Int32Be using the specified format provider.
        /// </summary>
        public static Int32Be Parse(string s, IFormatProvider? provider)
        {
            return new Int32Be(int.Parse(s, provider));
        }

        /// <summary>
        /// Parses a span of characters to an Int32Be.
        /// </summary>
        public static Int32Be Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return new Int32Be(int.Parse(s, provider));
        }

        /// <summary>
        /// Tries to parse a string to an Int32Be.
        /// </summary>
        public static bool TryParse(string? s, IFormatProvider? provider, out Int32Be result)
        {
            if (int.TryParse(s, provider, out int value))
            {
                result = new Int32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Tries to parse a span of characters to an Int32Be.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int32Be result)
        {
            if (int.TryParse(s, provider, out int value))
            {
                result = new Int32Be(value);
                return true;
            }
            result = default;
            return false;
        }

        /// <inheritdoc/>
        public override readonly string ToString() => $"0x{hi.hi:x2}{hi.lo:x2}{lo.hi:x2}{lo.lo:x2}";

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ((int)this).ToString(format, formatProvider);
        }

        /// <inheritdoc/>
        public readonly bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return ((int)this).TryFormat(destination, out charsWritten, format, provider);
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
            return CompareTo((Int32Be)obj);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(Int32Be other)
        {
            int a = (int)other;
            int b = (int)this;
            return b.CompareTo(a);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Equals((Int32Be)obj);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Int32Be other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((int)this).GetHashCode();
        }

        #endregion
    }
}
