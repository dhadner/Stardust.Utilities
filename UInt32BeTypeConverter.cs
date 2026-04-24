using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of <see cref="UInt32Be"/>.
    /// Accepts decimal (culture-aware), hex "0x…", and binary "0b…" input with optional
    /// '_' digit separators and surrounding whitespace. Emits values as zero-padded
    /// "0x" + hex for strings. Supports the designer via <see cref="InstanceDescriptor"/>.
    /// </summary>
    public class UInt32BeTypeConverter : TypeConverter
    {
        private static readonly StandardValuesCollection STANDARD_VALUES =
            new(new UInt32Be[] { new(0U), new(uint.MaxValue) });

        /// <summary>
        /// Returns whether this converter can convert from the specified source type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/> or <see cref="uint"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            if (sourceType == typeof(uint)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns whether this converter can convert to the specified destination type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/>, <see cref="uint"/>, or <see cref="InstanceDescriptor"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string)) return true;
            if (destinationType == typeof(uint)) return true;
            if (destinationType == typeof(InstanceDescriptor)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value to a <see cref="UInt32Be"/> instance.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information (used for decimal parsing).</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted <see cref="UInt32Be"/>.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                return ConverterParsing.Parse<UInt32Be>(s, culture,
                    (d, p) => UInt32Be.Parse(d, p),
                    h => UInt32Be.Parse(h, NumberStyles.HexNumber));
            }
            if (value is uint u) return new UInt32Be(u);
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
                if (value is UInt32Be v) return $"0x{(uint)v:x8}";
            }
            if (destinationType == typeof(uint) && value is UInt32Be v2) return (uint)v2;
            if (destinationType == typeof(InstanceDescriptor) && value is UInt32Be v3)
            {
                ConstructorInfo ctor = typeof(UInt32Be).GetConstructor(new[] { typeof(uint) })!;
                return new InstanceDescriptor(ctor, new object[] { (uint)v3 });
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
            if (value is UInt32Be) return true;
            if (value is uint) return true;
            if (value is string s)
            {
                return ConverterParsing.TryParse<UInt32Be>(s, null,
                    (d, p) => UInt32Be.Parse(d, p),
                    h => UInt32Be.Parse(h, NumberStyles.HexNumber),
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

        /// <summary>Returns the common values 0 and <see cref="uint.MaxValue"/> for PropertyGrid drop-down hints.</summary>
        /// <param name="context">Context information.</param>
        /// <returns>The standard-values collection.</returns>
        public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context) => STANDARD_VALUES;
    }
}
