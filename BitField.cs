using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stardust.Utilities
{
    #region Generic Types

    /// <summary>
    /// Defines a single-bit boolean flag within a <typeparamref name="TStorage"/> value.
    /// Pre-computes mask at construction time for zero-overhead runtime operations.
    /// </summary>
    /// <typeparam name="TStorage">The backing storage type (byte, ushort, uint, ulong).</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitFlag<TStorage>
        where TStorage : unmanaged, IBinaryInteger<TStorage>
    {
        private static int BitSize => Unsafe.SizeOf<TStorage>() * 8;
        private readonly TStorage _mask;

        /// <summary>The bit position (0-based).</summary>
        public int Shift
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => int.CreateTruncating(TStorage.Log2(_mask));
        }

        /// <summary>Creates a new bit flag definition.</summary>
        /// <param name="shift">The bit position (0-based).</param>
        public BitFlag(int shift)
        {
            if (shift < 0 || shift >= BitSize)
                throw new ArgumentOutOfRangeException(nameof(shift),
                    $"Shift must be 0-{BitSize - 1} for {typeof(TStorage).Name}.");
            _mask = TStorage.One << shift;
        }

        /// <summary>Gets the flag value using indexer syntax.</summary>
        public bool this[TStorage value]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (value & _mask) != TStorage.Zero;
        }

        /// <summary>Sets or clears the flag.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TStorage Set(TStorage value, bool flag) =>
            flag ? (value | _mask) : (value & ~_mask);

        /// <summary>Toggles the flag.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TStorage Toggle(TStorage value) => value ^ _mask;
    }

    /// <summary>
    /// Defines a multi-bit field within a <typeparamref name="TStorage"/> value.
    /// Pre-computes masks at construction time for efficient runtime operations.
    /// </summary>
    /// <typeparam name="TStorage">The backing storage type (byte, ushort, uint, ulong).</typeparam>
    /// <typeparam name="TField">The field type for extraction (byte, ushort, uint).</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitField<TStorage, TField>
        where TStorage : unmanaged, IBinaryInteger<TStorage>
        where TField : unmanaged, IBinaryInteger<TField>
    {
        private static int BitSize => Unsafe.SizeOf<TStorage>() * 8;
        private readonly int _shift;
        private readonly TStorage _mask;
        private readonly TStorage _shiftedMask;

        /// <summary>The starting bit position (0-based).</summary>
        public int Shift
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _shift;
        }

        /// <summary>The field width in bits.</summary>
        public int Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => int.CreateTruncating(TStorage.PopCount(_mask));
        }

        /// <summary>Creates a new bitfield definition.</summary>
        /// <param name="shift">The starting bit position (0-based).</param>
        /// <param name="width">The field width in bits.</param>
        public BitField(int shift, int width)
        {
            if (shift < 0)
                throw new ArgumentOutOfRangeException(nameof(shift), "Shift cannot be negative.");
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (shift + width > BitSize)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"Shift ({shift}) + Width ({width}) exceeds {BitSize} bits.");

            _shift = shift;
            // Handle full-width mask (avoid overflow when width == BitSize)
            _mask = width >= BitSize ? TStorage.AllBitsSet : (TStorage.One << width) - TStorage.One;
            _shiftedMask = _mask << shift;
        }

        /// <summary>Extracts the field value using indexer syntax.</summary>
        public TField this[TStorage value]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TField.CreateTruncating((value >> _shift) & _mask);
        }

        /// <summary>Sets the field value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TStorage Set(TStorage value, TField field)
        {
            TStorage shifted = TStorage.CreateTruncating(field) << _shift;
            return (value & ~_shiftedMask) | (shifted & _shiftedMask);
        }
    }

    #endregion

    #region Non-Generic Specialized Types (Hot Path Performance)

    /// <summary>
    /// Non-generic bitfield for ulong storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitField64
    {
        private readonly int _shift;
        private readonly ulong _mask;
        private readonly ulong _shiftedMask;

        public int Shift => _shift;
        public int Width => BitOperations.PopCount(_mask);

        public BitField64(int shift, int width)
        {
            if (shift < 0 || shift >= 64)
                throw new ArgumentOutOfRangeException(nameof(shift));
            if (width <= 0 || shift + width > 64)
                throw new ArgumentOutOfRangeException(nameof(width));

            _shift = shift;
            _mask = (1UL << width) - 1;
            _shiftedMask = _mask << shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(ulong value) => (byte)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetUShort(ulong value) => (ushort)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetUInt(ulong value) => (uint)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetULong(ulong value) => (value >> _shift) & _mask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Set(ulong value, byte field) =>
            (value & ~_shiftedMask) | (((ulong)field << _shift) & _shiftedMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Set(ulong value, ushort field) =>
            (value & ~_shiftedMask) | (((ulong)field << _shift) & _shiftedMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Set(ulong value, uint field) =>
            (value & ~_shiftedMask) | (((ulong)field << _shift) & _shiftedMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Set(ulong value, ulong field) =>
            (value & ~_shiftedMask) | ((field << _shift) & _shiftedMask);
    }

    /// <summary>
    /// Non-generic bit flag for ulong storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitFlag64
    {
        private readonly ulong _mask;

        public int Shift => BitOperations.TrailingZeroCount(_mask);

        public BitFlag64(int shift)
        {
            if (shift < 0 || shift >= 64)
                throw new ArgumentOutOfRangeException(nameof(shift));
            _mask = 1UL << shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(ulong value) => (value & _mask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Set(ulong value, bool flag) =>
            flag ? (value | _mask) : (value & ~_mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Toggle(ulong value) => value ^ _mask;
    }

    /// <summary>
    /// Non-generic bitfield for uint storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitField32
    {
        private readonly int _shift;
        private readonly uint _mask;
        private readonly uint _shiftedMask;

        public int Shift => _shift;
        public int Width => BitOperations.PopCount(_mask);

        public BitField32(int shift, int width)
        {
            if (shift < 0 || shift >= 32)
                throw new ArgumentOutOfRangeException(nameof(shift));
            if (width <= 0 || shift + width > 32)
                throw new ArgumentOutOfRangeException(nameof(width));

            _shift = shift;
            _mask = (1U << width) - 1;
            _shiftedMask = _mask << shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(uint value) => (byte)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetUShort(uint value) => (ushort)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetUInt(uint value) => (value >> _shift) & _mask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Set(uint value, byte field) =>
            (value & ~_shiftedMask) | (((uint)field << _shift) & _shiftedMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Set(uint value, ushort field) =>
            (value & ~_shiftedMask) | (((uint)field << _shift) & _shiftedMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Set(uint value, uint field) =>
            (value & ~_shiftedMask) | ((field << _shift) & _shiftedMask);
    }

    /// <summary>
    /// Non-generic bit flag for uint storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitFlag32
    {
        private readonly uint _mask;

        public int Shift => BitOperations.TrailingZeroCount(_mask);

        public BitFlag32(int shift)
        {
            if (shift < 0 || shift >= 32)
                throw new ArgumentOutOfRangeException(nameof(shift));
            _mask = 1U << shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(uint value) => (value & _mask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Set(uint value, bool flag) =>
            flag ? (value | _mask) : (value & ~_mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Toggle(uint value) => value ^ _mask;
    }

    /// <summary>
    /// Non-generic bitfield for ushort storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitField16
    {
        private readonly int _shift;
        private readonly ushort _mask;
        private readonly ushort _shiftedMask;

        public int Shift => _shift;
        public int Width => BitOperations.PopCount(_mask);

        public BitField16(int shift, int width)
        {
            if (shift < 0 || shift >= 16)
                throw new ArgumentOutOfRangeException(nameof(shift));
            if (width <= 0 || shift + width > 16)
                throw new ArgumentOutOfRangeException(nameof(width));

            _shift = shift;
            _mask = (ushort)((1 << width) - 1);
            _shiftedMask = (ushort)(_mask << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(ushort value) => (byte)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetUShort(ushort value) => (ushort)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Set(ushort value, byte field) =>
            (ushort)((value & ~_shiftedMask) | ((field << _shift) & _shiftedMask));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Set(ushort value, ushort field) =>
            (ushort)((value & ~_shiftedMask) | ((field << _shift) & _shiftedMask));
    }

    /// <summary>
    /// Non-generic bit flag for ushort storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitFlag16
    {
        private readonly ushort _mask;

        public int Shift => BitOperations.TrailingZeroCount(_mask);

        public BitFlag16(int shift)
        {
            if (shift < 0 || shift >= 16)
                throw new ArgumentOutOfRangeException(nameof(shift));
            _mask = (ushort)(1 << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(ushort value) => (value & _mask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Set(ushort value, bool flag) =>
            (ushort)(flag ? (value | _mask) : (value & ~_mask));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Toggle(ushort value) => (ushort)(value ^ _mask);
    }

    /// <summary>
    /// Non-generic bitfield for byte storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitField8
    {
        private readonly int _shift;
        private readonly byte _mask;
        private readonly byte _shiftedMask;

        public int Shift => _shift;
        public int Width => BitOperations.PopCount(_mask);

        public BitField8(int shift, int width)
        {
            if (shift < 0 || shift >= 8)
                throw new ArgumentOutOfRangeException(nameof(shift));
            if (width <= 0 || shift + width > 8)
                throw new ArgumentOutOfRangeException(nameof(width));

            _shift = shift;
            _mask = (byte)((1 << width) - 1);
            _shiftedMask = (byte)(_mask << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(byte value) => (byte)((value >> _shift) & _mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Set(byte value, byte field) =>
            (byte)((value & ~_shiftedMask) | ((field << _shift) & _shiftedMask));
    }

    /// <summary>
    /// Non-generic bit flag for byte storage. Use for hot paths requiring maximum performance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitFlag8
    {
        private readonly byte _mask;

        public int Shift => BitOperations.TrailingZeroCount(_mask);

        public BitFlag8(int shift)
        {
            if (shift < 0 || shift >= 8)
                throw new ArgumentOutOfRangeException(nameof(shift));
            _mask = (byte)(1 << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(byte value) => (value & _mask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Set(byte value, bool flag) =>
            (byte)(flag ? (value | _mask) : (value & ~_mask));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Toggle(byte value) => (byte)(value ^ _mask);
    }

    #endregion
}
