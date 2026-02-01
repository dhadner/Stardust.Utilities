using Stardust.Utilities;
using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for the BitStream class.
/// </summary>
public class BitStreamTests
{
    [Fact]
    public void CreateBitStreamWorks()
    {
        BitStream stream = new BitStream();
        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(-1);
        stream.Capacity.Should().Be(2048);

        long capacity = 1000000;
        stream = new(capacity);
        stream.Should().NotBeNull();
        stream.Length.Should().Be(0);
        stream.Position.Should().Be(-1);
        stream.Capacity.Should().Be(capacity);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(true, 500)]
    [InlineData(true, 1024)]
    public void ReadWriteBitWorks(bool bit, int count)
    {
        // Every other bit is 0
        var stream = new BitStream();
        for (int i = 0; i < count * 2; i++)
        {
            stream.Write((i & 1) == 0 ? bit : false);
        }
        stream.Length.Should().Be(count * 2);
        stream.Position = 0;
        for (int i = 0; i < count * 2; i++)
        {
            bool value = stream.Read();
            value.Should().Be((i & 1) == 0 ? bit : false);
        }
    }

    [Theory]
    [InlineData(new byte[] { 0x1 })]
    [InlineData(new byte[] { 0x1e, 0x2 })]
    [InlineData(new byte[] { 0x1c, 0xff, 0x3 })]
    [InlineData(new byte[] { 0x1d, 0x2, 0x3, 0x74 })]
    [InlineData(new byte[] { 0x1, 0x22, 0x3, 0x4, 0x65 })]
    [InlineData(new byte[] { 0x1f, 0xef, 0x3, 0x04, 0x5, 0xe6 })]
    public void SeekWorks(byte[] bits)
    {
        var stream = new BitStream();
        for (int i = 0; i < bits.Length; i++)
        {
            stream.WriteByte(bits[i]);
        }
        stream.Length.Should().Be(bits.Length * 8);
        stream.Position = 0;

        stream.Seek(0, SeekOrigin.Begin);
        stream.Position.Should().Be(0);

        stream.Seek(0, SeekOrigin.End);
        stream.Position.Should().Be(stream.Length - 1);

        stream.Position = stream.Length / 2;
        long pos = stream.Position;
        stream.Seek(0, SeekOrigin.Current);
        stream.Position.Should().Be(pos);

        stream.Seek(-1, SeekOrigin.Current);
        stream.Position.Should().Be(pos - 1);

        stream.Seek(1, SeekOrigin.Current);
        stream.Position.Should().Be(pos);
    }

    [Theory]
    [InlineData(new byte[] { 0x1 })]
    [InlineData(new byte[] { 0x1e, 0x2 })]
    [InlineData(new byte[] { 0x1c, 0xff, 0x3 })]
    [InlineData(new byte[] { 0x1d, 0x2, 0x3, 0x74 })]
    [InlineData(new byte[] { 0x1, 0x22, 0x3, 0x4, 0x65 })]
    [InlineData(new byte[] { 0x1f, 0xef, 0x3, 0x04, 0x5, 0xe6 })]
    public void WriteWorks(byte[] bits)
    {
        var stream = new BitStream();
        stream.Write(bits, 0, bits.Length);

        stream.Length.Should().Be(bits.Length * 8);
        stream.Position = 0;

        stream.Seek(0, SeekOrigin.Begin);
        stream.Position.Should().Be(0);

        stream.Seek(0, SeekOrigin.End);
        stream.Position.Should().Be(stream.Length - 1);

        stream.Position = stream.Length / 2;
        long pos = stream.Position;
        stream.Seek(0, SeekOrigin.Current);
        stream.Position.Should().Be(pos);

        stream.Seek(-1, SeekOrigin.Current);
        stream.Position.Should().Be(pos - 1);

        stream.Seek(1, SeekOrigin.Current);
        stream.Position.Should().Be(pos);
    }

    [Fact]
    public void SetLengthWorks()
    {
        var stream = new BitStream();
        stream.Capacity.Should().Be(2048);
        stream.Length.Should().Be(0);
        stream.Write(true);
        stream.Position.Should().Be(0);
        stream.Length.Should().Be(1);
        stream.SetLength(10);
        stream.Length.Should().Be(10);

        stream.SetLength(4000);
        stream.Length.Should().Be(4000);
        stream.Capacity.Should().BeGreaterThanOrEqualTo(4000);
    }

    [Theory]
    [InlineData(new byte[] { 0x1 })]
    [InlineData(new byte[] { 0x1e, 0x2 })]
    [InlineData(new byte[] { 0x1c, 0xff, 0x3 })]
    [InlineData(new byte[] { 0x1d, 0x2, 0x3, 0x74 })]
    [InlineData(new byte[] { 0x1, 0x22, 0x3, 0x4, 0x65 })]
    [InlineData(new byte[] { 0x1f, 0xef, 0x3, 0x04, 0x5, 0xe6 })]
    public void ReadByteWorks(byte[] bits)
    {
        var stream = new BitStream();
        for (int i = 0; i < bits.Length; i++)
        {
            stream.WriteByte(bits[i]);
        }
        stream.Length.Should().Be(bits.Length * 8);
        stream.Position = 0;
        for (int i = 0; i < bits.Length; i++)
        {
            byte test = 0;
            for (int b = 0; b < 8; b++)
            {
                byte mask = (byte)(1 << b);
                bool bit = stream.Read();
                test = bit ? (byte)(test | mask) : (byte)(test & ~mask);
            }
            test.Should().Be(bits[i]);
        }
    }

    [Theory]
    [InlineData(new byte[] { 0x1 }, 3, SeekOrigin.Begin, 0)]
    [InlineData(new byte[] { 0x1e, 0x2 }, 12, SeekOrigin.End, 0)]
    [InlineData(new byte[] { 0x1c, 0xff, 0x3 }, 11, SeekOrigin.Begin, 15)]
    [InlineData(new byte[] { 0x1d, 0x2, 0x3, 0x74 }, 1, SeekOrigin.Begin, 0)]
    [InlineData(new byte[] { 0x1, 0x22, 0x3, 0x4, 0x65 }, 35, SeekOrigin.Begin, 0)]
    [InlineData(new byte[] { 0x1f, 0xef, 0x3, 0x04, 0x5, 0xe6 }, 42, SeekOrigin.End, 46)]
    public void TruncateWorks(byte[] bits, int bitsToCut, SeekOrigin origin, long initialPosition)
    {
        var stream = new BitStream();
        for (int i = 0; i < bits.Length; i++)
        {
            stream.WriteByte(bits[i]);
        }
        var stream2 = new BitStream();
        for (int i = 0; i < bits.Length; i++)
        {
            stream2.WriteByte(bits[i]);
        }
        long initialLength = stream.Length;

        stream.Length.Should().Be(bits.Length * 8);

        stream.Position = initialPosition;
        stream.Truncate(bitsToCut, origin);

        stream.Length.Should().Be(initialLength - bitsToCut);
        stream.Position.Should().BeGreaterThanOrEqualTo(0);
        stream.Position.Should().BeLessThan(stream.Length);

        stream.Position = 0;
        if (origin == SeekOrigin.Begin)
        {
            stream2.Position = bitsToCut;
        }
        else
        {
            stream2.Position = 0;
        }
        for (int i = 0; i < stream.Length; i++)
        {
            bool bit = stream.Read();
            bool bit2 = stream2.Read();
            bit.Should().Be(bit2);
        }
    }

    [Theory]
    [InlineData(2048, -1, 10000, -1)]
    [InlineData(4096, 4000, 2048, 2047)]
    [InlineData(2048, 2000, 10000, 2000)]
    [InlineData(5000, 10, 10000, 10)]
    [InlineData(3000, 250, 10000, 250)]
    public void SizingWorks(int originalCapacity, long initialPosition, int newCapacity, long expectedEndPosition)
    {
        BitStream stream = new BitStream(originalCapacity);
        stream.Capacity.Should().Be(originalCapacity);

        for (int i = 0; i <= initialPosition; i++)
        {
            stream.Write(false);
        }
        stream.Position.Should().Be(initialPosition);

        byte[] array = stream.GetBuffer();
        int originalBufferLength = array.Length;
        for (int i = 0; i < array.Length; i++)
        {
            if (i % 2 == 0)
            {
                array[i] = 0xc3;
            }
            else
            {
                array[i] = 0x96;
            }
        }

        stream.Capacity = newCapacity;
        stream.Capacity.Should().Be(newCapacity);
        stream.Position.Should().Be(expectedEndPosition);

        byte[] buffer = stream.GetBuffer();
        int newBufferLength = buffer.Length;
        int lengthToCheck = Math.Min(originalBufferLength, newBufferLength);
        for (int i = 0; i < lengthToCheck; i++)
        {
            if (i % 2 == 0)
            {
                buffer[i].Should().Be(0xc3);
            }
            else
            {
                buffer[i].Should().Be(0x96);
            }
        }
    }
}
