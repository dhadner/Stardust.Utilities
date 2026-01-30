using System;
using System.IO;

namespace Stardust.Utilities.Bits
{
    /// <summary>
    /// BitStream class. Allows reading and writing individual bits as well as
    /// byte, ushort, and uint.
    /// </summary>
    public class BitStream : Stream
    {
        private byte[] _bits;
        private long _position = -1;
        private long _capacity;
        private long _length = 0;

        /// <summary>
        /// Create instance of <see cref="BitStream"/> class with
        /// default capacity of 2k bits.
        /// </summary>
        public BitStream()
        {
            _bits = new byte[256];
            _capacity = _bits.Length * 8;
        }

        /// <summary>
        /// Create instance of <see cref="BitStream"/> class with
        /// capacity of <paramref name="capacity"/> bits.
        /// </summary>
        /// <param name="capacity"></param>
        public BitStream(long capacity)
        {
            _bits = new byte[capacity / 8 + (capacity % 8 == 0 ? 0 : 1)];
            _capacity = capacity;
        }

        /// <summary>
        /// Capacity in bits.
        /// </summary>
        public long Capacity
        {
            get => _capacity;
            set
            {
                SetCapacity(value);
            }
        }

        /// <summary>
        /// Set capacity in bits.
        /// </summary>
        /// <param name="capacity">must be > 0</param>
        private void SetCapacity(long capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }
            if (capacity == Capacity)
            {
                return;
            }

            // Copy bytes. There are some corner cases where
            // the new capacity could result in the same number
            // of bytes, but those are not worth the effort to
            // save a copy operation since the default behavior
            // when writing to the stream is to double capacity
            // when increasing it. See EnsureCapacity.
            byte[] newBits = new byte[capacity / 8 + (capacity % 8 == 0 ? 0 : 1)];
            int byteCount = Math.Min(_bits.Length, newBits.Length);
            Span<byte> fromSpan = new Span<byte>(_bits, 0, byteCount);
            Span<byte> toSpan = new Span<byte>(newBits, 0, byteCount);
            fromSpan.CopyTo(toSpan);
            _bits = newBits;

            _capacity = capacity;
            if (_length > capacity)
            {
                _length = capacity;
            }
            if (_position > _length - 1)
            {
                // _position can be -1 if no bits.
                _position = _length - 1;
            }
        }

        /// <summary>
        /// True if can read from the stream.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// True if can seek in the stream.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// True if can write to the stream.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Length of the stream in bits (used).
        /// </summary>
        public override long Length => _length;

        /// <summary>
        /// Position in the stream in bits.
        /// </summary>
        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                {
                    // Set to -1 if no bits.
                    if (_length == 0)
                    {
                        _position = -1;
                    }
                    else
                    {
                        // Limit to 0 if there are bits
                        _position = 0;
                    }
                }
                else
                {
                    if (value > _length)
                    {
                        _position = _length;
                    }
                    else
                    {
                        _position = value;
                    }
                }
            }
        }

        /// <summary>
        /// Ensure enough capacity.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        private void EnsureCapacity(long value)
        {
            if (value <= _capacity)
            {
                return;
            }
            if (value > (long)Array.MaxLength * 8)
            {
                throw new ArgumentException("Capacity overflow - too large", nameof(value));
            }
            if (value < _capacity * 2)
            {
                // Grow by factor of 2 each time
                value = _capacity * 2;
            }

            // Limit to maximum array size when doubling.
            // value is guaranteed to be <= Array.MaxLength * 8.
            Capacity = Math.Min(value, (long)Array.MaxLength * 8);
        }

        /// <summary>
        /// Has no effect since the stream is in memory without 
        /// backing store.
        /// </summary>
        public override void Close()
        {
            // Empty method to prevent calling Dispose, etc.
        }

        /// <summary>
        /// Has no effect since the stream is in memory without
        /// backing store.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Read a bit at the current <see cref="Position"/> and increment the
        /// Position.
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            if (Position <= _capacity - 1)
            {
                bool bit = ReadBit(_bits, (int)Position);
                _position++;
                return bit;
            }
            throw new ArgumentException($"Position out of range: {Position}");
        }

        /// <summary>
        /// Read a bit, return 1 if bit read, -1 if not.
        /// Safe even at end of stream.
        /// </summary>
        /// <param name="bit"></param>
        /// <returns>1 if bit returned, else 0</returns>
        public int Read(out bool bit)
        {
            if (Position <= _capacity - 1)
            {
                bit = ReadBit(_bits, (int)Position);
                _position++;
                return 1;
            }
            bit = false;
            return -1;
        }

        /// <summary>
        /// Read bits from the stream into the passed-in buffer starting
        /// from <see cref="Position"/>. Advances Position by the number of
        /// bits read (i.e., count * 8 bits).
        /// </summary>
        /// <param name="buffer">output buffer</param>
        /// <param name="offset">starting offset in the output buffer</param>
        /// <param name="count">number of bytes to read</param>
        /// <returns>number of bytes or -1 if end of stream reached</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position > _length - 1)
            {
                return -1;
            }
            int outPos = offset * 8;
            int bitCount = count * 8;
            int byteCount = 0;
            for (int i = 0; i < bitCount; i++)
            {
                if (_position > _length - 1)
                {
                    break;
                }
                bool bit = ReadBit(_bits, _position++);
                WriteBit(buffer, outPos++, bit);
                if (i % 8 == 0)
                {
                    byteCount++;
                }
            }
            return byteCount;
        }

        /// <summary>
        /// Return the byte starting at the current position and
        /// increment position by 8.
        /// </summary>
        /// <returns>-1 if not enough bits to fill a byte, otherwise byte value</returns>
        public override int ReadByte()
        {
            int result = -1;
            if (Position >= 0 && Length >= Position + 8)
            {
                byte value = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if (Read())
                    {
                        value |= (byte)(1 << bit);
                    }
                }
                result = value;
            }
            return result;
        }

        /// <summary>
        /// Copy bytes from one buffer to another buffer.
        /// </summary>
        /// <param name="fromBuffer"></param>
        /// <param name="fromOffset"></param>
        /// <param name="toBuffer"></param>
        /// <param name="toOffset"></param>
        /// <param name="byteCount"></param>
        /// <exception cref="ArgumentException"></exception>
        private void CopyBytes(byte[] fromBuffer, int fromOffset,
                               byte[] toBuffer, int toOffset, int byteCount)
        {
            // Check bounds very carefully first.
            if (fromBuffer.Length < fromOffset + byteCount ||
                fromOffset < 0 ||
                toBuffer.Length < toOffset + byteCount ||
                toOffset < 0 ||
                byteCount < 0)
            {
                throw new ArgumentException("CopyBytes: Arguments out of range");
            }

            // Copy bytes
            Span<byte> fromSpan = new Span<byte>(fromBuffer, fromOffset, byteCount);
            Span<byte> toSpan = new Span<byte>(toBuffer, toOffset, byteCount);
            fromSpan.CopyTo(toSpan);
        }

        /// <summary>
        /// Copy bits.
        /// </summary>
        /// <param name="fromBuffer"></param>
        /// <param name="fromBitPosition"></param>
        /// <param name="toBuffer"></param>
        /// <param name="toBitPosition"></param>
        /// <param name="bitCount"></param>
        private void CopyBits(byte[] fromBuffer, int fromBitPosition,
                              byte[] toBuffer, int toBitPosition, int bitCount)
        {
            if (bitCount < 0)
            {
                throw new ArgumentException($"{nameof(bitCount)} < 0");
            }
            if (fromBitPosition < 0 || toBitPosition < 0)
            {
                throw new ArgumentException($"{nameof(fromBitPosition)} and/or {nameof(toBitPosition)} out of range");
            }

            if (fromBitPosition % 8 == 0 && toBitPosition % 8 == 0)
            {
                int byteCount = bitCount / 8;
                int fromOffset = fromBitPosition / 8;
                int toOffset = toBitPosition / 8;

                // Just copy the bytes
                CopyBytes(fromBuffer, fromOffset, toBuffer, toOffset, byteCount);

                int remainderBits = bitCount % 8;
                if (remainderBits > 0)
                {
                    // Finish up the last partial byte.
                    for (int i = 0; i < remainderBits; i++)
                    {
                        bool bit = ReadBit(fromBuffer, fromOffset * 8 + i);
                        WriteBit(toBuffer, toOffset * 8 + i, bit);
                    }
                }
            }
            else
            {
                int fromByteCount = (fromBitPosition + bitCount) / 8 + ((fromBitPosition + bitCount) % 8) != 0 ? 1 : 0;
                int toByteCount = (toBitPosition + bitCount) / 8 + ((toBitPosition + bitCount) % 8) != 0 ? 1 : 0;

                if (fromBuffer.Length < fromByteCount ||
                    toBuffer.Length < toByteCount)
                {
                    throw new ArgumentException($"CopyBits: Too many bits for the size of the array(s)");
                }

                // Do it the slow way
                for (int i = 0; i < bitCount; i++)
                {
                    bool bit = ReadBit(fromBuffer, fromBitPosition + i);
                    WriteBit(toBuffer, toBitPosition + i, bit);
                }
            }
        }

        /// <summary>
        /// Allow public access to the bit buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer()
        {
            return _bits;
        }

        /// <summary>
        /// Truncate the stream by <paramref name="bitCount"/> bits starting at
        /// the <paramref name="origin"/>. Does not change the Capacity.
        /// Position is set to the same bit as it started out unless
        /// that would push it off the end, in which case it is set to the 
        /// last bit. Length is reduced by <paramref name="bitCount"/>.
        /// </summary>
        /// <param name="bitCount"></param>
        /// <param name="origin">Begin and End are supported, but Current is not supported and
        /// throws an exception.</param>
        public void Truncate(int bitCount, SeekOrigin origin = SeekOrigin.Begin)
        {
            if (_position < 0)
            {
                return;
            }
            int length = (int)Length - bitCount;
            BitStream temp = new BitStream(_capacity);
            byte[] toBuffer = temp.GetBuffer();
            switch (origin)
            {
                case SeekOrigin.Begin:
                    CopyBits(_bits, bitCount, toBuffer, 0, length);
                    _bits = toBuffer;
                    _position = Math.Max(0, _position - bitCount);
                    break;
                case SeekOrigin.Current:
                    throw new NotSupportedException("SeekOrigin.Current not supported");
                case SeekOrigin.End:
                    // No action required - _length and _position handled below
                    break;
                default:
                    break;
            }
            _length = length;
            _position = Math.Min(_position, _length - 1);
        }

        /// <summary>
        /// Move the Position to the specified location.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - 1 + offset;
                    break;
                default:
                    break;
            };
            return Position;
        }

        /// <summary>
        /// Set the length in bits.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            EnsureCapacity(value);
            _length = value;
            if (_position > _length - 1)
            {
                _position = Math.Max(0, _length - 1);
            }
        }

        /// <summary>
        /// Write one bit if there is room and return 1, else return 0
        /// if at the maximum length of the stream.
        /// </summary>
        /// <param name="bit"></param>
        /// <returns>Number of bits written (0 or 1)</returns>
        public int Write(bool bit)
        {
            long nextPos = Position + 1;
            EnsureCapacity(nextPos + 1);

            WriteBit(_bits, (int)nextPos, bit);
            _length++;
            Position = nextPos;
            return 1;
        }

        /// <summary>
        /// Write (offset in bytes, count in bytes)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count * 8; i++)
            {
                int bitPosition = offset * 8 + i;
                bool bit = ReadBit(buffer, bitPosition);
                Write(bit);
            }
        }

        /// <summary>
        /// Read a bit.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bitPosition"></param>
        /// <returns></returns>
        private bool ReadBit(byte[] buffer, long bitPosition)
        {
            byte value = buffer[bitPosition / 8];
            return (value & (1 << (int)(bitPosition % 8))) != 0 ? true : false;
        }

        /// <summary>
        /// Write a bit.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bitPosition"></param>
        /// <param name="bit"></param>
        private void WriteBit(byte[] buffer, int bitPosition, bool bit)
        {
            int byteOffset = bitPosition / 8;
            int bitNumber = bitPosition % 8;
            byte value = buffer[byteOffset];
            if (bit)
            {
                value = (byte)(value | (1 << bitNumber));
            }
            else
            {
                value = (byte)(value & ~(1 << bitNumber));
            }
            buffer[byteOffset] = value;
        }
    }
}
