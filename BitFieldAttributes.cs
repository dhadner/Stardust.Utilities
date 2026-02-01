using System;

namespace Stardust.Utilities
{
    /// <summary>
    /// Marks a struct as a bit register, enabling source generation for bitfield properties.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// [BitFields(typeof(byte))]
    /// public partial struct MyRegister
    /// {
    ///     [BitField(0, 4)] public partial byte LowNibble { get; set; }
    ///     [BitFlag(7)] public partial bool Flag { get; set; }
    /// }
    /// 
    /// // Usage with implicit conversions
    /// MyRegister reg = 0xFF;       // From byte
    /// byte raw = reg;              // To byte
    /// var reg2 = new MyRegister(0x55);  // Constructor
    /// </code>
    /// The generator creates:
    /// <list type="bullet">
    /// <item>A private Value field of the specified storage type</item>
    /// <item>A constructor taking the storage type</item>
    /// <item>Property implementations with inline bit manipulation</item>
    /// <item>Implicit conversion operators to/from the storage type</item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class BitFieldsAttribute : Attribute
    {
        /// <summary>
        /// The storage type (byte, ushort, uint, ulong, sbyte, short, int, or long).
        /// </summary>
        public Type StorageType { get; }

        /// <summary>
        /// Creates a BitFields attribute with the specified storage type.
        /// </summary>
        /// <param name="storageType">The storage type (byte, ushort, uint, ulong, sbyte, short, int, or long).</param>
        public BitFieldsAttribute(Type storageType)
        {
            StorageType = storageType;
        }
    }

    /// <summary>
    /// Marks a property as a single-bit flag at the specified bit position.
    /// </summary>
    /// <remarks>
    /// The property must be declared as <c>public partial bool</c>.
    /// The generator will implement the getter/setter using the non-generic BitFlag types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BitFlagAttribute : Attribute
    {
        /// <summary>
        /// The bit position (0-based).
        /// </summary>
        public int Bit { get; }

        /// <summary>
        /// Creates a new bit flag attribute.
        /// </summary>
        /// <param name="bit">The bit position (0-based).</param>
        public BitFlagAttribute(int bit)
        {
            Bit = bit;
        }
    }

    /// <summary>
    /// Marks a property as a multi-bit field at the specified position and width.
    /// </summary>
    /// <remarks>
    /// The property must be declared as <c>public partial {type}</c> where type is
    /// byte, ushort, uint, or ulong. The generator will implement the getter/setter
    /// using the non-generic BitField types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BitFieldAttribute : Attribute
    {
        /// <summary>
        /// The starting bit position (0-based).
        /// </summary>
        public int Shift { get; }

        /// <summary>
        /// The width of the field in bits.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Creates a new bit field attribute.
        /// </summary>
        /// <param name="shift">The starting bit position (0-based).</param>
        /// <param name="width">The width of the field in bits.</param>
        public BitFieldAttribute(int shift, int width)
        {
            Shift = shift;
            Width = width;
        }
    }
}
