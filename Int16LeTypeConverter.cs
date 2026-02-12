using System;
using System.ComponentModel;
using System.Globalization;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of Int16Le.
    /// </summary>
    public class Int16LeTypeConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this converter can convert from the specified source type.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns><see langword="true"/> if conversion from <paramref name="sourceType"/> is supported; otherwise, <see langword="false"/>.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context,
                                            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Converts the given value to an Int16Le instance.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="culture">Culture information.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context,
                                            CultureInfo? culture, object value)
        {
            if (value is string)
            {
                string s = (string)value;
                NumberStyles style = NumberStyles.Integer;
                if (s.StartsWith("0x") || s.StartsWith("0X"))
                {
                    s = s.Substring(2);
                    style = NumberStyles.HexNumber;
                }
                var val = Int16Le.Parse(s, style);
                return val;
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
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture,
                                          object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value != null ? $"{value:x4}" : null;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
