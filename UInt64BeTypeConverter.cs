using System;
using System.ComponentModel;
using System.Globalization;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of UInt64Be.
    /// </summary>
    public class UInt64BeTypeConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this converter can convert from the specified source type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns><see langword="true"/> if conversion from <paramref name="sourceType"/> is supported; otherwise, <see langword="false"/>.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Converts the given value to a UInt64Be instance.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
            {
                NumberStyles style = NumberStyles.Integer;
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    s = s[2..];
                    style = NumberStyles.HexNumber;
                }
                return UInt64Be.Parse(s, style);
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
        /// <returns>The converted value.</returns>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is UInt64Be v)
            {
                return $"0x{(ulong)v:x16}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
