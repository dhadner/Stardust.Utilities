using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of <see cref="UInt64Be"/>.
    /// Accepts decimal (culture-aware), hex "0x…", and binary "0b…" input with optional
    /// '_' digit separators and surrounding whitespace. Emits values as zero-padded
    /// "0x" + hex for strings. Supports the designer via <see cref="InstanceDescriptor"/>.
    /// </summary>
    public class UInt64BeTypeConverter : TypeConverter
    {
        private static readonly StandardValuesCollection STANDARD_VALUES =
            new(new UInt64Be[] { new(0UL), new(ulong.MaxValue) });

        /// <summary>
        /// Returns whether this converter can convert from the specified source type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/> or <see cref="ulong"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            if (sourceType == typeof(ulong)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns whether this converter can convert to the specified destination type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns><see langword="true"/> for <see cref="string"/>, <see cref="ulong"/>, or <see cref="InstanceDescriptor"/>; otherwise delegates to the base.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string)) return true;
            if (destinationType == typeof(ulong)) return true;
            if (destinationType == typeof(InstanceDescriptor)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value to a <see cref="UInt64Be"/> instance.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information (used for decimal parsing).</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted <see cref="UInt64Be"/>.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                return ConverterParsing.Parse<UInt64Be>(s, culture,
                    (d, p) => UInt64Be.Parse(d, p),
                    h => UInt64Be.Parse(h, NumberStyles.HexNumber));
            }
            if (value is ulong u) return new UInt64Be(u);
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
                if (value is UInt64Be v) return $"0x{(ulong)v:x16}";
            }
            if (destinationType == typeof(ulong) && value is UInt64Be v2) return (ulong)v2;
            if (destinationType == typeof(InstanceDescriptor) && value is UInt64Be v3)
            {
                ConstructorInfo ctor = typeof(UInt64Be).GetConstructor(new[] { typeof(ulong) })!;
                return new InstanceDescriptor(ctor, new object[] { (ulong)v3 });
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
            if (value is UInt64Be) return true;
            if (value is ulong) return true;
            if (value is string s)
            {
                return ConverterParsing.TryParse<UInt64Be>(s, null,
                    (d, p) => UInt64Be.Parse(d, p),
                    h => UInt64Be.Parse(h, NumberStyles.HexNumber),
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

        /// <summary>Returns the common values 0 and <see cref="ulong.MaxValue"/> for PropertyGrid drop-down hints.</summary>
        /// <param name="context">Context information.</param>
        /// <returns>The standard-values collection.</returns>
        public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context) => STANDARD_VALUES;
    }
}
