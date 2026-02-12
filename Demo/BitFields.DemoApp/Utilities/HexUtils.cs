using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BitFields.DemoApp;

public static class HexUtils
{
    public static string ToHex(byte[] data, int maxBytes = 256)
    {
        int count = Math.Min(data.Length, maxBytes);
        var sb = new StringBuilder(count * 3);
        for (int i = 0; i < count; i++)
        {
            sb.Append(data[i].ToString("X2"));
            if (i < count - 1)
                sb.Append(' ');
        }
        if (data.Length > maxBytes)
            sb.Append(" ...");
        return sb.ToString();
    }

    public static bool TryParseHex(string input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var cleaned = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (cleaned.Length % 2 != 0)
            return false;

        int count = cleaned.Length / 2;
        var buffer = new byte[count];
        for (int i = 0; i < count; i++)
        {
            if (!byte.TryParse(cleaned.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out buffer[i]))
                return false;
        }

        bytes = buffer;
        return true;
    }

    public static bool TryParseUShort(string input, out ushort value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];

        return ushort.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    public static string ToBinaryString(ulong value, int width)
    {
        var sb = new StringBuilder(width + (width / 4));
        for (int i = width - 1; i >= 0; i--)
        {
            sb.Append(((value >> i) & 1UL) == 1UL ? '1' : '0');
            if (i % 4 == 0 && i != 0)
                sb.Append(' ');
        }
        return sb.ToString();
    }

    public static string ToIPv4(uint value)
    {
        byte b1 = (byte)((value >> 24) & 0xFF);
        byte b2 = (byte)((value >> 16) & 0xFF);
        byte b3 = (byte)((value >> 8) & 0xFF);
        byte b4 = (byte)(value & 0xFF);
        return $"{b1}.{b2}.{b3}.{b4}";
    }

    public static string ProtocolName(byte protocol)
    {
        return protocol switch
        {
            6 => "TCP",
            17 => "UDP",
            1 => "ICMP",
            _ => protocol.ToString()
        };
    }
}
