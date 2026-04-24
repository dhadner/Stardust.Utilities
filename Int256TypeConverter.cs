using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of <see cref="Int256"/>.
    /// Accepts decimal (culture-aware, signed), hex "0x…", and binary "0b…" input with
    /// optional '_' digit separators and surrounding whitespace. Emits values as
    /// culture-aware decimal strings for output (matching
    /// <see cref="System.ComponentModel.Int32Converter"/> and the other BCL numeric
    /// converters). Supports the designer via <see cref="InstanceDescriptor"/>.
    /// </summary>
    public class Int256TypeConverter : TypeConverter
    {
        private static readonly StandardValuesCollection STANDARD_VALUES =
            new(new Int256[] { Int256.MinValue, Int256.Zero, Int256.MaxValue });

        /// <summary>
        /// Returns whether this converter can convert from the specified source type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns whether this converter can convert to the specified destination type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/> or <see cref="InstanceDescriptor"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string)) return true;
            if (destinationType == typeof(InstanceDescriptor)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value to an <see cref="Int256"/> instance.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information (used for decimal parsing).</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted <see cref="Int256"/>.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                return ConverterParsing.Parse<Int256>(s, culture,
                    (d, p) => Int256.Parse(d, p),
                    h => Int256.Parse(h, NumberStyles.HexNumber));
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value to the specified destination type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The converted value; <see langword="null"/> if <paramref name="value"/> is null and the target is <see cref="string"/>.</returns>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture,
                                          object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is null) return null;
                if (value is Int256 v) return v.ToString("G", culture ?? CultureInfo.CurrentCulture);
            }
            if (destinationType == typeof(InstanceDescriptor) && value is Int256 v3)
            {
                ConstructorInfo ctor = typeof(Int256).GetConstructor(
                    new[] { typeof(ulong), typeof(ulong), typeof(ulong), typeof(ulong) })!;
                return new InstanceDescriptor(ctor, new object[] { v3._p3, v3._p2, v3._p1, v3._p0 });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Validates that <paramref name="value"/> is acceptable as input for this converter
        /// without throwing. Strings are probed via TryParse.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="value">The candidate value.</param>
        /// <returns>True when the value is acceptable.</returns>
        public override bool IsValid(ITypeDescriptorContext? context, object? value)
        {
            if (value is Int256) return true;
            if (value is string s)
            {
                return ConverterParsing.TryParse<Int256>(s, null,
                    (d, p) => Int256.Parse(d, p),
                    h => Int256.Parse(h, NumberStyles.HexNumber),
                    out _);
            }
            return base.IsValid(context, value);
        }

        /// <summary>Indicates that this converter supplies a short list of standard values.</summary>
        /// <param name="context">Context information.</param>
        /// <returns>Always true.</returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        /// <summary>Standard values are suggestions, not an exclusive set — the user may type any value.</summary>
        /// <param name="context">Context information.</param>
        /// <returns>Always false.</returns>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        /// <summary>Returns <see cref="Int256.MinValue"/>, <see cref="Int256.Zero"/>, and <see cref="Int256.MaxValue"/> for PropertyGrid drop-down hints.</summary>
        /// <param name="context">Context information.</param>
        /// <returns>The standard-values collection.</returns>
        public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context) => STANDARD_VALUES;
    }
}
