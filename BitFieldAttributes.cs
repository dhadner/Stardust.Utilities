using System;

namespace Stardust.Utilities
{
    /// <summary>
    /// Marks a struct as a bit register, enabling source generation for bitfield properties.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// [BitFields]
    /// public partial struct MyRegister
    /// {
    ///     public ulong Value;
    ///     
    ///     [BitField(0, 8)] public partial byte LowByte { get; set; }
    ///     [BitFlag(15)] public partial bool Flag { get; set; }
    /// }
    /// </code>
    /// The generator creates property implementations and implicit conversion operators.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class BitFieldsAttribute : Attribute
    {
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
