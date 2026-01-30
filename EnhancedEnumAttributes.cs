using System;

namespace Stardust.Utilities
{
    /// <summary>
    /// Marks a partial record as an enhanced enum (discriminated union).
    /// The generator will create nested record types for each variant defined in the [EnumKind] enum.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage:
    /// <code>
    /// [EnhancedEnum]
    /// public partial record MyEnum
    /// {
    ///     [EnumKind]
    ///     private enum Kind
    ///     {
    ///         [EnumValue(typeof((uint, int)))]
    ///         Option1,
    ///         
    ///         [EnumValue(typeof(string))]
    ///         Option2,
    ///         
    ///         [EnumValue]  // No payload
    ///         None,
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The generator creates:
    /// <list type="bullet">
    ///   <item>Nested sealed record types: <c>MyEnum.Option1</c>, <c>MyEnum.Option2</c>, <c>MyEnum.None</c></item>
    ///   <item>Static factory methods: <c>MyEnum.Option1((1u, 2))</c></item>
    ///   <item>Deconstruct methods for pattern matching in switch expressions</item>
    /// </list>
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var value = MyEnum.Option1((42u, -1));
    /// 
    /// string result = value switch
    /// {
    ///     MyEnum.Option1(var tuple) => $"Got tuple: {tuple}",
    ///     MyEnum.Option2(var str) => $"Got string: {str}",
    ///     MyEnum.None => "Got none",
    ///     _ => "Unknown"
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EnhancedEnumAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks an enum nested within an [EnhancedEnum] record as the kind discriminator.
    /// Each enum value should be decorated with [EnumValue] to specify its payload type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class EnumKindAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies the payload type for an enum variant within an [EnumKind] enum.
    /// Use the parameterless constructor for variants with no payload.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// [EnumKind]
    /// private enum Kind
    /// {
    ///     [EnumValue(typeof(int))]           // Payload is int
    ///     Number,
    ///     
    ///     [EnumValue(typeof((uint, string)))] // Payload is a tuple
    ///     AddressWithName,
    ///     
    ///     [EnumValue(typeof(Exception))]     // Payload is a reference type
    ///     Error,
    ///     
    ///     [EnumValue]                        // No payload (unit variant)
    ///     None,
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumValueAttribute : Attribute
    {
        /// <summary>
        /// The CLR type of the payload for this variant, or null for unit variants.
        /// </summary>
        public Type? PayloadType { get; }

        /// <summary>
        /// Creates an enum value attribute for a unit variant (no payload).
        /// </summary>
        public EnumValueAttribute()
        {
            PayloadType = null;
        }

        /// <summary>
        /// Creates an enum value attribute with the specified payload type.
        /// </summary>
        /// <param name="payloadType">The CLR type of the payload.</param>
        public EnumValueAttribute(Type payloadType)
        {
            PayloadType = payloadType;
        }
    }
}
