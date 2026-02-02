using System;
using System.ComponentModel;
using System.Globalization;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of Int64Be.
    /// </summary>
    public class Int64BeTypeConverter : TypeConverter
    {
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc/>
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
                return Int64Be.Parse(s, style);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc/>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Int64Be v)
            {
                return $"0x{(ulong)(long)v:x16}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
