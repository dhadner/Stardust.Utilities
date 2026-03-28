using System;
using Stardust.Utilities;

namespace BitFields.DemoApp;

// ═══════════════════════════════════════════════════════════════════
//  Composable Telemetry Frame — a single record-struct view with
//  dual-access floating-point fields (native + IEEE 754) and 1-bit
//  separators that force every data field off byte boundaries.
//
//  Frame layout (181 data bits):
//    SOF(1) | Temperature(64) | Sep1(1) | Pressure(64) | Sep2(1)
//    | SensorHealth(32) | Sep3(1) | Humidity(16) | EOF(1)
//
//  Each floating-point field is exposed as BOTH a native type
//  (double/float/Half) and as an IEEE 754 BitFields struct
//  (IEEE754Double/IEEE754Single/IEEE754Half) at the same bit range,
//  letting the user read and write the same bits two ways.
//
//  At runtime the demo bit-shifts the frame into a larger stream
//  buffer at a user-chosen offset (0-7 bits), then extracts and
//  reads it back — proving the library handles arbitrary alignment.
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Single composable telemetry frame with dual-access floating-point fields.
/// <code>
/// Bit  0      : SOF          (1-bit start-of-frame)
/// Bits 1-64   : Temperature  (64-bit double / IEEE754Double)
/// Bit  65     : Sep1         (1-bit separator)
/// Bits 66-129 : Pressure     (64-bit double / IEEE754Double)
/// Bit  130    : Sep2         (1-bit separator)
/// Bits 131-162: SensorHealth (32-bit float / IEEE754Single)
/// Bit  163    : Sep3         (1-bit separator)
/// Bits 164-179: Humidity     (16-bit Half / IEEE754Half)
/// Bit  180    : EOF          (1-bit end-of-frame)
/// </code>
/// </summary>
[BitFields(Description = "Composable Telemetry Frame — dual float/IEEE 754 access, 1-bit separators")]
public partial record struct TelemetryFrame
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

    [BitField(164, End = 179, Description = "Humidity (%) — native Half")]
    public partial Half Humidity { get; set; }
    [BitField(164, End = 179, Description = "Humidity (%) — IEEE 754 half decomposition")]
    public partial IEEE754Half HumidityIEEE { get; set; }

    [BitFlag(180, Description = "End-of-frame marker")]
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
    IEEE754Half Humidity);

/// <summary>
/// Values read back from a telemetry buffer (native types).
/// </summary>
public readonly record struct TelemetryReadback(
    double Temperature,
    double Pressure,
    float SensorHealth,
    Half Humidity,
    bool SOF,
    bool EOF);

/// <summary>
/// Shared logic for the composable telemetry frame demo.
/// Uses a single <see cref="TelemetryFrame"/> struct and bit-shifts
/// the data at runtime to simulate arbitrary stream offsets.
/// </summary>
public static class TelemetryStreamDemo
{
    /// <summary>Maximum supported stream bit offset.</summary>
    public const int MAX_OFFSET = 7;

    /// <summary>Frame size in bytes (no offset).</summary>
    public static int FrameBytes => TelemetryFrame.SIZE_IN_BYTES;

    /// <summary>
    /// Returns the stream buffer size needed when the frame starts at
    /// <paramref name="offsetBits"/> bits into the stream.
    /// </summary>
    public static int StreamBufferSize(int offsetBits) =>
        (TelemetryFrame.SIZE_IN_BYTES * 8 + offsetBits + 7) / 8;

    /// <summary>
    /// Writes telemetry values into a frame-sized buffer at bit offset 0.
    /// </summary>
    public static void WriteFrame(byte[] frameBuffer,
        double temperature, double pressure,
        float sensorHealth, Half humidity)
    {
        Array.Clear(frameBuffer, 0, frameBuffer.Length);
        var frame = new TelemetryFrame(frameBuffer);
        frame.SOF = true;
        frame.Temperature = temperature;
        frame.Sep1 = true;
        frame.Pressure = pressure;
        frame.Sep2 = true;
        frame.SensorHealth = sensorHealth;
        frame.Sep3 = true;
        frame.Humidity = humidity;
        frame.EOF = true;
    }

    /// <summary>
    /// Writes IEEE 754 decomposed values back into the buffer.
    /// </summary>
    public static void WriteIEEE(byte[] frameBuffer,
        IEEE754Double temperature, IEEE754Double pressure,
        IEEE754Single sensorHealth, IEEE754Half humidity)
    {
        var f = new TelemetryFrame(frameBuffer);
        f.TemperatureIEEE = temperature;
        f.PressureIEEE = pressure;
        f.SensorHealthIEEE = sensorHealth;
        f.HumidityIEEE = humidity;
    }

    /// <summary>Reads native telemetry values from a frame-sized buffer.</summary>
    public static TelemetryReadback ReadFrame(byte[] frameBuffer)
    {
        var f = new TelemetryFrame(frameBuffer);
        return new(f.Temperature, f.Pressure, f.SensorHealth, f.Humidity, f.SOF, f.EOF);
    }

    /// <summary>Reads IEEE 754 decompositions from a frame-sized buffer.</summary>
    public static IEEEReadback ReadIEEE(byte[] frameBuffer)
    {
        var f = new TelemetryFrame(frameBuffer);
        return new(f.TemperatureIEEE, f.PressureIEEE, f.SensorHealthIEEE, f.HumidityIEEE);
    }

    // ── Bit-shift helpers ────────────────────────────────────

    /// <summary>
    /// Shifts all bits in <paramref name="src"/> right by
    /// <paramref name="bitShift"/> positions, producing a new buffer
    /// large enough to hold the shifted result.  Simulates the frame
    /// appearing at an arbitrary bit offset within a stream.
    /// </summary>
    public static byte[] ShiftRight(byte[] src, int bitShift)
    {
        if (bitShift == 0) return (byte[])src.Clone();

        int totalBits = src.Length * 8 + bitShift;
        var dst = new byte[(totalBits + 7) / 8];

        for (int i = 0; i < src.Length * 8; i++)
        {
            if ((src[i / 8] & (1 << (i % 8))) != 0)
            {
                int d = i + bitShift;
                dst[d / 8] |= (byte)(1 << (d % 8));
            }
        }

        return dst;
    }

    /// <summary>
    /// Extracts frame data from a stream buffer that was shifted by
    /// <paramref name="bitShift"/> bits.  Returns a frame-sized buffer.
    /// </summary>
    public static byte[] ExtractFrame(byte[] streamBuffer, int bitShift, int frameBytes)
    {
        if (bitShift == 0)
        {
            var copy = new byte[frameBytes];
            Array.Copy(streamBuffer, copy, Math.Min(streamBuffer.Length, frameBytes));
            return copy;
        }

        var dst = new byte[frameBytes];
        int frameBits = frameBytes * 8;

        for (int i = 0; i < frameBits; i++)
        {
            int s = i + bitShift;
            if (s / 8 < streamBuffer.Length &&
                (streamBuffer[s / 8] & (1 << (s % 8))) != 0)
            {
                dst[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return dst;
    }

    /// <summary>Returns field metadata for the frame.</summary>
    public static ReadOnlySpan<BitFieldInfo> GetFields() =>
        TelemetryFrame.Fields;

    /// <summary>Returns the struct description.</summary>
    public static string? GetDescription()
    {
        var fields = TelemetryFrame.Fields;
        return fields.Length > 0 ? fields[0].StructDescription : null;
    }

    /// <summary>Returns the view Type for diagram generation.</summary>
    public static Type GetViewType() => typeof(TelemetryFrame);

    /// <summary>Default sample values.</summary>
    public static class Defaults
    {
        public const double TEMPERATURE = 23.456;
        public const double PRESSURE = 1013.25;
        public const float SENSOR_HEALTH = 0.98f;
        public static readonly Half HUMIDITY = (Half)0.65;
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
