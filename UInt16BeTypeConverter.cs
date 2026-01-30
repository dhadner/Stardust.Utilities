using System;
using System.ComponentModel;
using System.Globalization;

namespace Stardust.Utilities
{
    /// <summary>
    /// Used to support PropertyGrid editing of UInt16Be.
    /// </summary>
    public class UInt16BeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context,
                                            Type sourceType)
        {
            return sourceType == typeof(string);
        }

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
                var val = UInt16Be.Parse(s, style);
                return val;
            }

            return base.ConvertFrom(context, culture, value);
        }

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
