using System;

namespace Stardust.Utilities
{
    /// <summary>
    /// Marks a partial struct as an enhanced enum (discriminated union).
    /// The generator creates a zero-allocation struct with inline payload storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage:
    /// <code>
    /// [EnhancedEnum]
    /// public partial struct MyCommand
    /// {
    ///     [EnumKind]
    ///     private enum Kind
    ///     {
    ///         [EnumValue(typeof((uint, int)))]
    ///         SetValue,
    ///         
    ///         [EnumValue(typeof(string))]  // Reference types are fine
    ///         Evaluate,
    ///         
    ///         [EnumValue]  // No payload
    ///         Step,
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The generator creates:
    /// <list type="bullet">
    ///   <item>Static factory methods: <c>MyCommand.SetValue((1u, 2))</c></item>
    ///   <item>Is properties: <c>cmd.IsSetValue</c></item>
    ///   <item>TryGet methods: <c>cmd.TryGetSetValue(out var value)</c></item>
    ///   <item>Match method for exhaustive pattern matching</item>
    /// </list>
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var cmd = MyCommand.SetValue((42u, -1));
    /// 
    /// // Using Match:
    /// string result = cmd.Match(
    ///     setValue: v => $"Set: {v}",
    ///     evaluate: s => $"Eval: {s}",
    ///     step: () => "Step"
    /// );
    /// 
    /// // Using TryGet:
    /// if (cmd.TryGetSetValue(out var value))
    ///     Console.WriteLine(value);
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class EnhancedEnumAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks an enum nested within an [EnhancedEnum] struct as the kind discriminator.
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
