using Stardust.Utilities;

namespace BitFields.DemoApp;

// ═══════════════════════════════════════════════════════════════════
//  Telemetry Stream Views — single fixed frame layout at variable
//  stream offsets, demonstrating non-byte-aligned floating-point
//  access through both native types and IEEE 754 decompositions.
//
//  Frame layout (181 data bits):
//    SOF(1) | Temperature(64) | Sep1(1) | Pressure(64) | Sep2(1)
//    | SensorHealth(32) | Sep3(1) | Checksum(16) | EOF(1)
//
//  Each floating-point field is exposed as BOTH a native type
//  (double/float/Half) and as an IEEE 754 BitFields struct
//  (IEEE754Double/IEEE754Single/IEEE754Half) at the same bit range,
//  letting the user read and write the same bits two ways.
//
//  The leading stream offset (0, 1, 3, or 7 bits) shifts the
//  entire frame within the buffer, proving the library handles
//  arbitrary bit alignment.
// ═══════════════════════════════════════════════════════════════════

// ── Offset 0 ─────────────────────────────────────────────────────
/// <summary>
/// Telemetry frame at stream offset 0 (no leading pad).
/// <code>
/// Bit  0      : SOF          (1-bit start-of-frame)
/// Bits 1-64   : Temperature  (64-bit double / IEEE754Double)
/// Bit  65     : Sep1         (1-bit separator)
/// Bits 66-129 : Pressure     (64-bit double / IEEE754Double)
/// Bit  130    : Sep2         (1-bit separator)
/// Bits 131-162: SensorHealth (32-bit float / IEEE754Single)
/// Bit  163    : Sep3         (1-bit separator)
/// Bits 164-179: Checksum     (16-bit Half / IEEE754Half)
/// Bit  180    : EOF          (1-bit end-of-frame)
/// </code>
/// </summary>
[BitFields(Description = "Telemetry Frame — stream offset 0 (all fields non-byte-aligned)")]
public partial record struct TelemetryStreamAt0
{
    [BitFlag(0, Description = "Start-of-frame marker")]
    public partial bool SOF { get; set; }

    [BitField(1, End = 64, Description = "Temperature (°C) — native double")]
    public partial double Temperature { get; set; }
    [BitField(1, End = 64, Description = "Temperature (°C) — IEEE 754 double decomposition")]
    public partial IEEE754Double TemperatureIEEE { get; set; }

    [BitFlag(65, Description = "Field separator")]
    public partial bool Sep1 { get; set; }

    [BitField(66, End = 129, Description = "Pressure (hPa) — native double")]
    public partial double Pressure { get; set; }
    [BitField(66, End = 129, Description = "Pressure (hPa) — IEEE 754 double decomposition")]
    public partial IEEE754Double PressureIEEE { get; set; }

    [BitFlag(130, Description = "Field separator")]
    public partial bool Sep2 { get; set; }

    [BitField(131, End = 162, Description = "Sensor health — native float")]
    public partial float SensorHealth { get; set; }
    [BitField(131, End = 162, Description = "Sensor health — IEEE 754 single decomposition")]
    public partial IEEE754Single SensorHealthIEEE { get; set; }

    [BitFlag(163, Description = "Field separator")]
    public partial bool Sep3 { get; set; }

    [BitField(164, End = 179, Description = "Checksum — native Half")]
    public partial Half Checksum { get; set; }
    [BitField(164, End = 179, Description = "Checksum — IEEE 754 half decomposition")]
    public partial IEEE754Half ChecksumIEEE { get; set; }

    [BitFlag(180, Description = "End-of-frame marker")]
    public partial bool EOF { get; set; }
}

// ── Offset 1 ─────────────────────────────────────────────────────
/// <summary>
/// Telemetry frame at stream offset 1 (1-bit leading pad).
/// Same frame layout shifted by 1 bit within the stream buffer.
/// </summary>
[BitFields(Description = "Telemetry Frame — stream offset 1 (1-bit lead)")]
public partial record struct TelemetryStreamAt1
{
    [BitField(0, End = 0, Description = "Stream offset padding (1 bit)")]
    public partial byte Pad { get; set; }

    [BitFlag(1, Description = "Start-of-frame marker")]
    public partial bool SOF { get; set; }

    [BitField(2, End = 65, Description = "Temperature (°C) — native double")]
    public partial double Temperature { get; set; }
    [BitField(2, End = 65, Description = "Temperature (°C) — IEEE 754 double decomposition")]
    public partial IEEE754Double TemperatureIEEE { get; set; }

    [BitFlag(66, Description = "Field separator")]
    public partial bool Sep1 { get; set; }

    [BitField(67, End = 130, Description = "Pressure (hPa) — native double")]
    public partial double Pressure { get; set; }
    [BitField(67, End = 130, Description = "Pressure (hPa) — IEEE 754 double decomposition")]
    public partial IEEE754Double PressureIEEE { get; set; }

    [BitFlag(131, Description = "Field separator")]
    public partial bool Sep2 { get; set; }

    [BitField(132, End = 163, Description = "Sensor health — native float")]
    public partial float SensorHealth { get; set; }
    [BitField(132, End = 163, Description = "Sensor health — IEEE 754 single decomposition")]
    public partial IEEE754Single SensorHealthIEEE { get; set; }

    [BitFlag(164, Description = "Field separator")]
    public partial bool Sep3 { get; set; }

    [BitField(165, End = 180, Description = "Checksum — native Half")]
    public partial Half Checksum { get; set; }
    [BitField(165, End = 180, Description = "Checksum — IEEE 754 half decomposition")]
    public partial IEEE754Half ChecksumIEEE { get; set; }

    [BitFlag(181, Description = "End-of-frame marker")]
    public partial bool EOF { get; set; }
}

// ── Offset 3 ─────────────────────────────────────────────────────
/// <summary>
/// Telemetry frame at stream offset 3 (3-bit leading pad).
/// Same frame layout shifted by 3 bits within the stream buffer.
/// </summary>
[BitFields(Description = "Telemetry Frame — stream offset 3 (3-bit lead)")]
public partial record struct TelemetryStreamAt3
{
    [BitField(0, End = 2, Description = "Stream offset padding (3 bits)")]
    public partial byte Pad { get; set; }

    [BitFlag(3, Description = "Start-of-frame marker")]
    public partial bool SOF { get; set; }

    [BitField(4, End = 67, Description = "Temperature (°C) — native double")]
    public partial double Temperature { get; set; }
    [BitField(4, End = 67, Description = "Temperature (°C) — IEEE 754 double decomposition")]
    public partial IEEE754Double TemperatureIEEE { get; set; }

    [BitFlag(68, Description = "Field separator")]
    public partial bool Sep1 { get; set; }

    [BitField(69, End = 132, Description = "Pressure (hPa) — native double")]
    public partial double Pressure { get; set; }
    [BitField(69, End = 132, Description = "Pressure (hPa) — IEEE 754 double decomposition")]
    public partial IEEE754Double PressureIEEE { get; set; }

    [BitFlag(133, Description = "Field separator")]
    public partial bool Sep2 { get; set; }

    [BitField(134, End = 165, Description = "Sensor health — native float")]
    public partial float SensorHealth { get; set; }
    [BitField(134, End = 165, Description = "Sensor health — IEEE 754 single decomposition")]
    public partial IEEE754Single SensorHealthIEEE { get; set; }

    [BitFlag(166, Description = "Field separator")]
    public partial bool Sep3 { get; set; }

    [BitField(167, End = 182, Description = "Checksum — native Half")]
    public partial Half Checksum { get; set; }
    [BitField(167, End = 182, Description = "Checksum — IEEE 754 half decomposition")]
    public partial IEEE754Half ChecksumIEEE { get; set; }

    [BitFlag(183, Description = "End-of-frame marker")]
    public partial bool EOF { get; set; }
}

// ── Offset 7 ─────────────────────────────────────────────────────
/// <summary>
/// Telemetry frame at stream offset 7 (7-bit leading pad).
/// Same frame layout shifted by 7 bits within the stream buffer.
/// </summary>
[BitFields(Description = "Telemetry Frame — stream offset 7 (7-bit lead)")]
public partial record struct TelemetryStreamAt7
{
    [BitField(0, End = 6, Description = "Stream offset padding (7 bits)")]
    public partial byte Pad { get; set; }

    [BitFlag(7, Description = "Start-of-frame marker")]
    public partial bool SOF { get; set; }

    [BitField(8, End = 71, Description = "Temperature (°C) — native double")]
    public partial double Temperature { get; set; }
    [BitField(8, End = 71, Description = "Temperature (°C) — IEEE 754 double decomposition")]
    public partial IEEE754Double TemperatureIEEE { get; set; }

    [BitFlag(72, Description = "Field separator")]
    public partial bool Sep1 { get; set; }

    [BitField(73, End = 136, Description = "Pressure (hPa) — native double")]
    public partial double Pressure { get; set; }
    [BitField(73, End = 136, Description = "Pressure (hPa) — IEEE 754 double decomposition")]
    public partial IEEE754Double PressureIEEE { get; set; }

    [BitFlag(137, Description = "Field separator")]
    public partial bool Sep2 { get; set; }

    [BitField(138, End = 169, Description = "Sensor health — native float")]
    public partial float SensorHealth { get; set; }
    [BitField(138, End = 169, Description = "Sensor health — IEEE 754 single decomposition")]
    public partial IEEE754Single SensorHealthIEEE { get; set; }

    [BitFlag(170, Description = "Field separator")]
    public partial bool Sep3 { get; set; }

    [BitField(171, End = 186, Description = "Checksum — native Half")]
    public partial Half Checksum { get; set; }
    [BitField(171, End = 186, Description = "Checksum — IEEE 754 half decomposition")]
    public partial IEEE754Half ChecksumIEEE { get; set; }

    [BitFlag(187, Description = "End-of-frame marker")]
    public partial bool EOF { get; set; }
}

// ═══════════════════════════════════════════════════════════════════
//  Shared demo logic — platform-agnostic, consumed by WPF & Blazor
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// IEEE 754 decomposition of all four floating-point fields in the telemetry frame.
/// Returned by <see cref="TelemetryStreamDemo.ReadIEEE"/> for display in the UI.
/// </summary>
public readonly record struct IEEEReadback(
    IEEE754Double Temperature,
    IEEE754Double Pressure,
    IEEE754Single SensorHealth,
    IEEE754Half Checksum);

/// <summary>
/// Values read back from a telemetry buffer (native types).
/// </summary>
public readonly record struct TelemetryReadback(
    double Temperature,
    double Pressure,
    float SensorHealth,
    Half Checksum,
    bool SOF,
    bool EOF);

/// <summary>
/// Shared logic for the telemetry stream demo. Platform-agnostic so it can be
/// consumed by both the WPF and Blazor front-ends.
/// </summary>
public static class TelemetryStreamDemo
{
    /// <summary>Supported stream offsets.</summary>
    public static readonly int[] OFFSETS = [0, 1, 3, 7];

    /// <summary>Returns the buffer size for the selected stream offset.</summary>
    public static int BufferSize(int offset) => offset switch
    {
        0 => TelemetryStreamAt0.SIZE_IN_BYTES,
        1 => TelemetryStreamAt1.SIZE_IN_BYTES,
        3 => TelemetryStreamAt3.SIZE_IN_BYTES,
        7 => TelemetryStreamAt7.SIZE_IN_BYTES,
        _ => TelemetryStreamAt0.SIZE_IN_BYTES,
    };

    /// <summary>
    /// Writes telemetry values into a byte buffer using the appropriate
    /// offset view. Returns the view type name.
    /// </summary>
    public static string WriteFrame(
        byte[] buffer, int offset,
        double temperature, double pressure,
        float sensorHealth, Half checksum)
    {
        Array.Clear(buffer, 0, buffer.Length);

        switch (offset)
        {
            case 0:
            {
                var v = new TelemetryStreamAt0(buffer);
                v.SOF = true;
                v.Temperature = temperature;
                v.Sep1 = true;
                v.Pressure = pressure;
                v.Sep2 = true;
                v.SensorHealth = sensorHealth;
                v.Sep3 = true;
                v.Checksum = checksum;
                v.EOF = true;
                return nameof(TelemetryStreamAt0);
            }
            case 1:
            {
                var v = new TelemetryStreamAt1(buffer);
                v.SOF = true;
                v.Temperature = temperature;
                v.Sep1 = true;
                v.Pressure = pressure;
                v.Sep2 = true;
                v.SensorHealth = sensorHealth;
                v.Sep3 = true;
                v.Checksum = checksum;
                v.EOF = true;
                return nameof(TelemetryStreamAt1);
            }
            case 3:
            {
                var v = new TelemetryStreamAt3(buffer);
                v.SOF = true;
                v.Temperature = temperature;
                v.Sep1 = true;
                v.Pressure = pressure;
                v.Sep2 = true;
                v.SensorHealth = sensorHealth;
                v.Sep3 = true;
                v.Checksum = checksum;
                v.EOF = true;
                return nameof(TelemetryStreamAt3);
            }
            case 7:
            {
                var v = new TelemetryStreamAt7(buffer);
                v.SOF = true;
                v.Temperature = temperature;
                v.Sep1 = true;
                v.Pressure = pressure;
                v.Sep2 = true;
                v.SensorHealth = sensorHealth;
                v.Sep3 = true;
                v.Checksum = checksum;
                v.EOF = true;
                return nameof(TelemetryStreamAt7);
            }
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Writes IEEE 754 decomposed values (sign, exponent, mantissa) back
    /// into the buffer through the IEEE 754 overlay properties.
    /// </summary>
    public static void WriteIEEE(
        byte[] buffer, int offset,
        IEEE754Double temperature, IEEE754Double pressure,
        IEEE754Single sensorHealth, IEEE754Half checksum)
    {
        switch (offset)
        {
            case 0:
            {
                var v = new TelemetryStreamAt0(buffer);
                v.TemperatureIEEE = temperature;
                v.PressureIEEE = pressure;
                v.SensorHealthIEEE = sensorHealth;
                v.ChecksumIEEE = checksum;
                break;
            }
            case 1:
            {
                var v = new TelemetryStreamAt1(buffer);
                v.TemperatureIEEE = temperature;
                v.PressureIEEE = pressure;
                v.SensorHealthIEEE = sensorHealth;
                v.ChecksumIEEE = checksum;
                break;
            }
            case 3:
            {
                var v = new TelemetryStreamAt3(buffer);
                v.TemperatureIEEE = temperature;
                v.PressureIEEE = pressure;
                v.SensorHealthIEEE = sensorHealth;
                v.ChecksumIEEE = checksum;
                break;
            }
            case 7:
            {
                var v = new TelemetryStreamAt7(buffer);
                v.TemperatureIEEE = temperature;
                v.PressureIEEE = pressure;
                v.SensorHealthIEEE = sensorHealth;
                v.ChecksumIEEE = checksum;
                break;
            }
        }
    }

    /// <summary>Reads native telemetry values back from a buffer.</summary>
    public static TelemetryReadback ReadFrame(byte[] buffer, int offset) => offset switch
    {
        0 => Read0(buffer),
        1 => Read1(buffer),
        3 => Read3(buffer),
        7 => Read7(buffer),
        _ => Read0(buffer),
    };

    /// <summary>Reads IEEE 754 decompositions from a buffer.</summary>
    public static IEEEReadback ReadIEEE(byte[] buffer, int offset) => offset switch
    {
        0 => ReadIEEE0(buffer),
        1 => ReadIEEE1(buffer),
        3 => ReadIEEE3(buffer),
        7 => ReadIEEE7(buffer),
        _ => ReadIEEE0(buffer),
    };

    // ── Native readers ───────────────────────────────────────
    private static TelemetryReadback Read0(byte[] b) { var v = new TelemetryStreamAt0(b); return new(v.Temperature, v.Pressure, v.SensorHealth, v.Checksum, v.SOF, v.EOF); }
    private static TelemetryReadback Read1(byte[] b) { var v = new TelemetryStreamAt1(b); return new(v.Temperature, v.Pressure, v.SensorHealth, v.Checksum, v.SOF, v.EOF); }
    private static TelemetryReadback Read3(byte[] b) { var v = new TelemetryStreamAt3(b); return new(v.Temperature, v.Pressure, v.SensorHealth, v.Checksum, v.SOF, v.EOF); }
    private static TelemetryReadback Read7(byte[] b) { var v = new TelemetryStreamAt7(b); return new(v.Temperature, v.Pressure, v.SensorHealth, v.Checksum, v.SOF, v.EOF); }

    // ── IEEE 754 readers ─────────────────────────────────────
    private static IEEEReadback ReadIEEE0(byte[] b) { var v = new TelemetryStreamAt0(b); return new(v.TemperatureIEEE, v.PressureIEEE, v.SensorHealthIEEE, v.ChecksumIEEE); }
    private static IEEEReadback ReadIEEE1(byte[] b) { var v = new TelemetryStreamAt1(b); return new(v.TemperatureIEEE, v.PressureIEEE, v.SensorHealthIEEE, v.ChecksumIEEE); }
    private static IEEEReadback ReadIEEE3(byte[] b) { var v = new TelemetryStreamAt3(b); return new(v.TemperatureIEEE, v.PressureIEEE, v.SensorHealthIEEE, v.ChecksumIEEE); }
    private static IEEEReadback ReadIEEE7(byte[] b) { var v = new TelemetryStreamAt7(b); return new(v.TemperatureIEEE, v.PressureIEEE, v.SensorHealthIEEE, v.ChecksumIEEE); }

    /// <summary>Returns field metadata for the selected offset.</summary>
    public static ReadOnlySpan<BitFieldInfo> GetFields(int offset) => offset switch
    {
        0 => TelemetryStreamAt0.Fields,
        1 => TelemetryStreamAt1.Fields,
        3 => TelemetryStreamAt3.Fields,
        7 => TelemetryStreamAt7.Fields,
        _ => TelemetryStreamAt0.Fields,
    };

    /// <summary>Returns the struct description for the selected offset.</summary>
    public static string? GetDescription(int offset)
    {
        var fields = GetFields(offset);
        return fields.Length > 0 ? fields[0].StructDescription : null;
    }

    /// <summary>Returns the view Type for the selected offset (for diagram generation).</summary>
    public static Type GetViewType(int offset) => offset switch
    {
        0 => typeof(TelemetryStreamAt0),
        1 => typeof(TelemetryStreamAt1),
        3 => typeof(TelemetryStreamAt3),
        7 => typeof(TelemetryStreamAt7),
        _ => typeof(TelemetryStreamAt0),
    };

    /// <summary>Default sample values.</summary>
    public static class Defaults
    {
        public const double TEMPERATURE = 23.456;
        public const double PRESSURE = 1013.25;
        public const float SENSOR_HEALTH = 0.98f;
        public static readonly Half CHECKSUM = (Half)1.5;
    }

    /// <summary>Classifies an IEEE 754 double value.</summary>
    public static string Classify(IEEE754Double d)
    {
        if (d.IsNaN) return "NaN";
        if (d.IsInfinity) return d.Sign ? "\u2212\u221E" : "+\u221E";
        if (d.IsZero) return d.Sign ? "\u22120" : "0";
        if (d.IsDenormalized) return "Denorm";
        return "Normal";
    }

    /// <summary>Classifies an IEEE 754 single value.</summary>
    public static string Classify(IEEE754Single f)
    {
        if (f.IsNaN) return "NaN";
        if (f.IsInfinity) return f.Sign ? "\u2212\u221E" : "+\u221E";
        if (f.IsZero) return f.Sign ? "\u22120" : "0";
        if (f.IsDenormalized) return "Denorm";
        return "Normal";
    }

    /// <summary>Classifies an IEEE 754 half value.</summary>
    public static string Classify(IEEE754Half h)
    {
        if (h.IsNaN) return "NaN";
        if (h.IsInfinity) return h.Sign ? "\u2212\u221E" : "+\u221E";
        if (h.IsZero) return h.Sign ? "\u22120" : "0";
        if (h.IsDenormalized) return "Denorm";
        return "Normal";
    }
}
