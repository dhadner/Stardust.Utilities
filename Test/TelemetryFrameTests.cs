using System;
using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

// ═══════════════════════════════════════════════════════════════════
//  Test struct: mirrors the demo TelemetryFrame layout exactly.
//  Record struct view backed by byte[], with dual-access float/IEEE754
//  fields separated by 1-bit markers that force every data field
//  off byte boundaries.
//
//  Layout (181 bits):
//    Bit  0      : SOF          (1-bit start-of-frame)
//    Bits 1-64   : Temperature  (64-bit double / IEEE754Double)
//    Bit  65     : Sep1         (1-bit separator)
//    Bits 66-129 : Pressure     (64-bit double / IEEE754Double)
//    Bit  130    : Sep2         (1-bit separator)
//    Bits 131-162: SensorHealth (32-bit float / IEEE754Single)
//    Bit  163    : Sep3         (1-bit separator)
//    Bits 164-179: Humidity     (16-bit Half / IEEE754Half)
//    Bit  180    : EOF          (1-bit end-of-frame)
// ═══════════════════════════════════════════════════════════════════

[BitFields(Description = "Test Telemetry Frame — dual float/IEEE 754 access")]
public partial record struct TestTelemetryFrame
{
    [BitFlag(0, Description = "Start-of-frame marker")]
    public partial bool SOF { get; set; }

    [BitField(1, End = 64, Description = "Temperature (°C) — native double")]
    public partial double Temperature { get; set; }
    [BitField(1, End = 64, Description = "Temperature (°C) — IEEE 754 double")]
    public partial IEEE754Double TemperatureIEEE { get; set; }

    [BitFlag(65, Description = "Field separator 1")]
    public partial bool Sep1 { get; set; }

    [BitField(66, End = 129, Description = "Pressure (hPa) — native double")]
    public partial double Pressure { get; set; }
    [BitField(66, End = 129, Description = "Pressure (hPa) — IEEE 754 double")]
    public partial IEEE754Double PressureIEEE { get; set; }

    [BitFlag(130, Description = "Field separator 2")]
    public partial bool Sep2 { get; set; }

    [BitField(131, End = 162, Description = "Sensor health — native float")]
    public partial float SensorHealth { get; set; }
    [BitField(131, End = 162, Description = "Sensor health — IEEE 754 single")]
    public partial IEEE754Single SensorHealthIEEE { get; set; }

    [BitFlag(163, Description = "Field separator 3")]
    public partial bool Sep3 { get; set; }

    [BitField(164, End = 179, Description = "Humidity (%) — native Half")]
    public partial Half Humidity { get; set; }
    [BitField(164, End = 179, Description = "Humidity (%) — IEEE 754 half")]
    public partial IEEE754Half HumidityIEEE { get; set; }

    [BitFlag(180, Description = "End-of-frame marker")]
    public partial bool EOF { get; set; }
}

/// <summary>
/// Tests for the TelemetryFrame composable struct — dual-access floating-point
/// fields at non-byte-aligned positions with bit-shift stream simulation.
/// </summary>
public class TelemetryFrameTests
{
    // ── Constants ────────────────────────────────────────────────
    private const double TEMPERATURE = 23.456;
    private const double PRESSURE = 1013.25;
    private const float SENSOR_HEALTH = 0.98f;
    private static readonly Half HUMIDITY = (Half)0.65;
    private const int MAX_OFFSET = 7;

    // ── Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Writes a fully populated frame into a new buffer at bit offset 0.
    /// </summary>
    private static byte[] WriteDefaultFrame(
        double temperature = TEMPERATURE,
        double pressure = PRESSURE,
        float health = SENSOR_HEALTH,
        Half? humidity = null)
    {
        Half hm = humidity ?? HUMIDITY;
        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);
        frame.SOF = true;
        frame.Temperature = temperature;
        frame.Sep1 = true;
        frame.Pressure = pressure;
        frame.Sep2 = true;
        frame.SensorHealth = health;
        frame.Sep3 = true;
        frame.Humidity = hm;
        frame.EOF = true;
        return buf;
    }

    /// <summary>
    /// Shifts all bits in src right by bitShift positions into a larger buffer.
    /// </summary>
    private static byte[] ShiftRight(byte[] src, int bitShift)
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
    /// Extracts frame-sized data from a stream buffer shifted by bitShift.
    /// </summary>
    private static byte[] ExtractFrame(byte[] streamBuffer, int bitShift, int frameBytes)
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
            if (s / 8 < streamBuffer.Length && (streamBuffer[s / 8] & (1 << (s % 8))) != 0)
                dst[i / 8] |= (byte)(1 << (i % 8));
        }
        return dst;
    }

    // ═══════════════════════════════════════════════════════════════
    //  1. Basic Read/Write — Native Types
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void NativeTypes_RoundTrip_DefaultValues()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        frame.SOF.Should().BeTrue();
        frame.Temperature.Should().Be(TEMPERATURE);
        frame.Sep1.Should().BeTrue();
        frame.Pressure.Should().Be(PRESSURE);
        frame.Sep2.Should().BeTrue();
        frame.SensorHealth.Should().Be(SENSOR_HEALTH);
        frame.Sep3.Should().BeTrue();
        frame.Humidity.Should().Be(HUMIDITY);
        frame.EOF.Should().BeTrue();
    }

    [Fact]
    public void NativeTypes_RoundTrip_NegativeValues()
    {
        var buf = WriteDefaultFrame(-40.5, -1013.25, -0.5f, (Half)(-1.0));
        var frame = new TestTelemetryFrame(buf);

        frame.Temperature.Should().Be(-40.5);
        frame.Pressure.Should().Be(-1013.25);
        frame.SensorHealth.Should().Be(-0.5f);
        frame.Humidity.Should().Be((Half)(-1.0));
    }

    [Fact]
    public void NativeTypes_RoundTrip_Zero()
    {
        var buf = WriteDefaultFrame(0.0, 0.0, 0.0f, (Half)0.0);
        var frame = new TestTelemetryFrame(buf);

        frame.Temperature.Should().Be(0.0);
        frame.Pressure.Should().Be(0.0);
        frame.SensorHealth.Should().Be(0.0f);
        frame.Humidity.Should().Be((Half)0.0);
    }

    [Fact]
    public void NativeTypes_RoundTrip_Extremes()
    {
        var buf = WriteDefaultFrame(double.MaxValue, double.MinValue, float.MaxValue, Half.MaxValue);
        var frame = new TestTelemetryFrame(buf);

        frame.Temperature.Should().Be(double.MaxValue);
        frame.Pressure.Should().Be(double.MinValue);
        frame.SensorHealth.Should().Be(float.MaxValue);
        frame.Humidity.Should().Be(Half.MaxValue);
    }

    [Fact]
    public void NativeTypes_RoundTrip_SpecialValues()
    {
        // NaN, Infinity
        var buf = WriteDefaultFrame(double.NaN, double.PositiveInfinity, float.NegativeInfinity, Half.NaN);
        var frame = new TestTelemetryFrame(buf);

        double.IsNaN(frame.Temperature).Should().BeTrue();
        double.IsPositiveInfinity(frame.Pressure).Should().BeTrue();
        float.IsNegativeInfinity(frame.SensorHealth).Should().BeTrue();
        Half.IsNaN(frame.Humidity).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  2. Dual Access — IEEE 754 and Native at Same Bit Range
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DualAccess_IEEE754Double_MatchesNativeDouble()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        // Read temperature via native and IEEE754
        var ieee = frame.TemperatureIEEE;
        ieee.IsNaN.Should().BeFalse();
        ieee.IsInfinity.Should().BeFalse();
        ieee.IsDenormalized.Should().BeFalse();
        ieee.IsZero.Should().BeFalse();

        // Write via IEEE then read back as native — bits should match
        var buf2 = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame2 = new TestTelemetryFrame(buf2);
        frame2.TemperatureIEEE = ieee;
        frame2.Temperature.Should().Be(TEMPERATURE);
    }

    [Fact]
    public void DualAccess_IEEE754Single_MatchesNativeFloat()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        var ieee = frame.SensorHealthIEEE;
        ieee.IsNaN.Should().BeFalse();
        ieee.IsZero.Should().BeFalse();

        var buf2 = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame2 = new TestTelemetryFrame(buf2);
        frame2.SensorHealthIEEE = ieee;
        frame2.SensorHealth.Should().Be(SENSOR_HEALTH);
    }

    [Fact]
    public void DualAccess_IEEE754Half_MatchesNativeHalf()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        var ieee = frame.HumidityIEEE;
        ieee.IsNaN.Should().BeFalse();
        ieee.IsZero.Should().BeFalse();

        var buf2 = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame2 = new TestTelemetryFrame(buf2);
        frame2.HumidityIEEE = ieee;
        frame2.Humidity.Should().Be(HUMIDITY);
    }

    [Fact]
    public void DualAccess_WriteNative_ReadIEEE_PressureNormal()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        var ieee = frame.PressureIEEE;
        ieee.Sign.Should().BeFalse("1013.25 is positive");
        ieee.Exponent.Should().NotBeNull();
        ieee.IsDenormalized.Should().BeFalse();
        ieee.IsInfinity.Should().BeFalse();
        ieee.IsNaN.Should().BeFalse();
    }

    [Fact]
    public void DualAccess_WriteIEEE_ReadNative_AllFields()
    {
        // Write via IEEE then read back via native for all fields
        var refBuf = WriteDefaultFrame();
        var refFrame = new TestTelemetryFrame(refBuf);
        var tempIEEE = refFrame.TemperatureIEEE;
        var presIEEE = refFrame.PressureIEEE;
        var healthIEEE = refFrame.SensorHealthIEEE;
        var hmIEEE = refFrame.HumidityIEEE;

        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);
        frame.SOF = true;
        frame.TemperatureIEEE = tempIEEE;
        frame.Sep1 = true;
        frame.PressureIEEE = presIEEE;
        frame.Sep2 = true;
        frame.SensorHealthIEEE = healthIEEE;
        frame.Sep3 = true;
        frame.HumidityIEEE = hmIEEE;
        frame.EOF = true;

        frame.Temperature.Should().Be(TEMPERATURE);
        frame.Pressure.Should().Be(PRESSURE);
        frame.SensorHealth.Should().Be(SENSOR_HEALTH);
        frame.Humidity.Should().Be(HUMIDITY);
    }

    // ═══════════════════════════════════════════════════════════════
    //  3. Separator Bits — 1-bit fields between data fields
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Separators_IndependentOfDataFields()
    {
        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);

        // Set separators without setting data
        frame.Sep1 = true;
        frame.Sep2 = true;
        frame.Sep3 = true;

        frame.Sep1.Should().BeTrue();
        frame.Sep2.Should().BeTrue();
        frame.Sep3.Should().BeTrue();

        // Clear one separator — others unaffected
        frame.Sep2 = false;
        frame.Sep1.Should().BeTrue();
        frame.Sep2.Should().BeFalse();
        frame.Sep3.Should().BeTrue();
    }

    [Fact]
    public void Separators_SurviveDataFieldWrites()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        // All separators should be true
        frame.Sep1.Should().BeTrue();
        frame.Sep2.Should().BeTrue();
        frame.Sep3.Should().BeTrue();

        // Overwrite data fields — separators should survive
        frame.Temperature = -999.999;
        frame.Pressure = 0.001;
        frame.SensorHealth = float.Epsilon;

        frame.Sep1.Should().BeTrue("Sep1 at bit 65 is independent of Temperature (1-64) and Pressure (66-129)");
        frame.Sep2.Should().BeTrue("Sep2 at bit 130 is independent of Pressure (66-129) and SensorHealth (131-162)");
        frame.Sep3.Should().BeTrue("Sep3 at bit 163 is independent of SensorHealth (131-162) and Humidity (164-179)");
    }

    // ═══════════════════════════════════════════════════════════════
    //  4. Bit-Shift Stream Simulation — all offsets (0-7)
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void StreamShift_RoundTrip_AllOffsets(int offset)
    {
        var frameBuf = WriteDefaultFrame();
        var stream = ShiftRight(frameBuf, offset);
        var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
        var frame = new TestTelemetryFrame(extracted);

        frame.SOF.Should().BeTrue($"SOF should survive {offset}-bit shift");
        frame.Temperature.Should().Be(TEMPERATURE, $"Temperature should survive {offset}-bit shift");
        frame.Sep1.Should().BeTrue($"Sep1 should survive {offset}-bit shift");
        frame.Pressure.Should().Be(PRESSURE, $"Pressure should survive {offset}-bit shift");
        frame.Sep2.Should().BeTrue($"Sep2 should survive {offset}-bit shift");
        frame.SensorHealth.Should().Be(SENSOR_HEALTH, $"SensorHealth should survive {offset}-bit shift");
        frame.Sep3.Should().BeTrue($"Sep3 should survive {offset}-bit shift");
        frame.Humidity.Should().Be(HUMIDITY, $"Humidity should survive {offset}-bit shift");
        frame.EOF.Should().BeTrue($"EOF should survive {offset}-bit shift");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void StreamShift_RoundTrip_IEEE754_AllFields(int offset)
    {
        var frameBuf = WriteDefaultFrame();
        var refFrame = new TestTelemetryFrame(frameBuf);
        var refTempIEEE = refFrame.TemperatureIEEE;
        var refPresIEEE = refFrame.PressureIEEE;
        var refHealthIEEE = refFrame.SensorHealthIEEE;
        var refHmIEEE = refFrame.HumidityIEEE;

        var stream = ShiftRight(frameBuf, offset);
        var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
        var frame = new TestTelemetryFrame(extracted);

        frame.TemperatureIEEE.Sign.Should().Be(refTempIEEE.Sign);
        frame.TemperatureIEEE.Exponent.Should().Be(refTempIEEE.Exponent);
        frame.TemperatureIEEE.Mantissa.Should().Be(refTempIEEE.Mantissa);
        frame.PressureIEEE.Sign.Should().Be(refPresIEEE.Sign);
        frame.PressureIEEE.Exponent.Should().Be(refPresIEEE.Exponent);
        frame.PressureIEEE.Mantissa.Should().Be(refPresIEEE.Mantissa);
        frame.SensorHealthIEEE.Sign.Should().Be(refHealthIEEE.Sign);
        frame.SensorHealthIEEE.Exponent.Should().Be(refHealthIEEE.Exponent);
        frame.SensorHealthIEEE.Mantissa.Should().Be(refHealthIEEE.Mantissa);
        frame.HumidityIEEE.Sign.Should().Be(refHmIEEE.Sign);
        frame.HumidityIEEE.Exponent.Should().Be(refHmIEEE.Exponent);
        frame.HumidityIEEE.Mantissa.Should().Be(refHmIEEE.Mantissa);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void StreamShift_BufferSizeLarger_WhenOffsetNonZero(int offset)
    {
        var frameBuf = WriteDefaultFrame();
        var stream = ShiftRight(frameBuf, offset);

        stream.Length.Should().BeGreaterThan(frameBuf.Length,
            $"stream with {offset}-bit offset needs extra byte(s)");
    }

    [Fact]
    public void StreamShift_ZeroOffset_SameSize()
    {
        var frameBuf = WriteDefaultFrame();
        var stream = ShiftRight(frameBuf, 0);

        stream.Length.Should().Be(frameBuf.Length);
        stream.Should().BeEquivalentTo(frameBuf);
    }

    // ═══════════════════════════════════════════════════════════════
    //  5. SIZE_IN_BYTES and Fields Metadata
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void SizeInBytes_CoversAllBits()
    {
        // 181 bits → ceil(181/8) = 23 bytes, word-aligned to 24
        TestTelemetryFrame.SIZE_IN_BYTES.Should().Be(24);
    }

    [Fact]
    public void Fields_ContainsAllDeclaredProperties()
    {
        var fields = TestTelemetryFrame.Fields;
        var names = new HashSet<string>();
        foreach (var f in fields)
            names.Add(f.Name);

        names.Should().Contain("SOF");
        names.Should().Contain("Temperature");
        names.Should().Contain("TemperatureIEEE");
        names.Should().Contain("Sep1");
        names.Should().Contain("Pressure");
        names.Should().Contain("PressureIEEE");
        names.Should().Contain("Sep2");
        names.Should().Contain("SensorHealth");
        names.Should().Contain("SensorHealthIEEE");
        names.Should().Contain("Sep3");
        names.Should().Contain("Humidity");
        names.Should().Contain("HumidityIEEE");
        names.Should().Contain("EOF");
    }

    [Fact]
    public void Fields_DescriptionsPresent()
    {
        foreach (var f in TestTelemetryFrame.Fields)
            f.GetDescription().Should().NotBeNullOrEmpty($"field {f.Name} should have a description");
    }

    // ═══════════════════════════════════════════════════════════════
    //  6. Overlapping Fields — Writing one updates the other
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Overlapping_WriteNativeDouble_ChangeVisibleViaIEEE()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        // Change temperature via native
        frame.Temperature = 100.0;

        // IEEE view should reflect the change
        var ieee = frame.TemperatureIEEE;
        ieee.Sign.Should().BeFalse("100.0 is positive");
        ieee.IsZero.Should().BeFalse();
        ieee.IsNaN.Should().BeFalse();

        // Write back to a clean buffer via IEEE and read as native
        var buf2 = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame2 = new TestTelemetryFrame(buf2);
        frame2.TemperatureIEEE = ieee;
        frame2.Temperature.Should().Be(100.0);
    }

    [Fact]
    public void Overlapping_WriteNativeFloat_ChangeVisibleViaIEEE()
    {
        var buf = WriteDefaultFrame();
        var frame = new TestTelemetryFrame(buf);

        frame.SensorHealth = 0.0f;
        frame.SensorHealthIEEE.IsZero.Should().BeTrue();

        frame.SensorHealth = float.PositiveInfinity;
        frame.SensorHealthIEEE.IsInfinity.Should().BeTrue();
    }

    [Fact]
    public void Overlapping_WriteNativeHalf_ChangeVisibleViaIEEE()
    {
        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);

        frame.Humidity = Half.NaN;
        frame.HumidityIEEE.IsNaN.Should().BeTrue();

        frame.Humidity = Half.PositiveInfinity;
        frame.HumidityIEEE.IsInfinity.Should().BeTrue();
        frame.HumidityIEEE.Sign.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    //  7. Fuzz-Style — Random values at random offsets
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Fuzz_RandomValues_AllOffsets_10Iterations()
    {
        var rng = new Random(42);

        for (int i = 0; i < 10; i++)
        {
            double temp = -100.0 + rng.NextDouble() * 200.0;
            double pres = rng.NextDouble() * 2000.0;
            float health = (float)rng.NextDouble();
            Half hm = (Half)(rng.NextDouble() * 2.0);
            int offset = rng.Next(0, MAX_OFFSET + 1);

            var frameBuf = WriteDefaultFrame(temp, pres, health, hm);
            var stream = ShiftRight(frameBuf, offset);
            var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
            var frame = new TestTelemetryFrame(extracted);

            frame.Temperature.Should().Be(temp, $"iteration {i}, offset {offset}");
            frame.Pressure.Should().Be(pres, $"iteration {i}, offset {offset}");
            frame.SensorHealth.Should().Be(health, $"iteration {i}, offset {offset}");
            frame.Humidity.Should().Be(hm, $"iteration {i}, offset {offset}");
            frame.SOF.Should().BeTrue($"iteration {i}, offset {offset}");
            frame.EOF.Should().BeTrue($"iteration {i}, offset {offset}");
        }
    }

    [Fact]
    public void Fuzz_SpecialFloatValues_AllOffsets()
    {
        double[] doubles = [double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon, double.MinValue, double.MaxValue, 0.0, -0.0];
        float[] floats = [float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, 0.0f];
        Half[] halves = [Half.NaN, Half.PositiveInfinity, Half.NegativeInfinity, Half.Epsilon, Half.Zero];

        for (int offset = 0; offset <= MAX_OFFSET; offset++)
        {
            foreach (var d in doubles)
            {
                var frameBuf = WriteDefaultFrame(d, d, floats[0], halves[0]);
                var stream = ShiftRight(frameBuf, offset);
                var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
                var frame = new TestTelemetryFrame(extracted);

                if (double.IsNaN(d))
                    double.IsNaN(frame.Temperature).Should().BeTrue();
                else
                    frame.Temperature.Should().Be(d);
            }

            foreach (var f in floats)
            {
                var frameBuf = WriteDefaultFrame(TEMPERATURE, PRESSURE, f, halves[0]);
                var stream = ShiftRight(frameBuf, offset);
                var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
                var frame = new TestTelemetryFrame(extracted);

                if (float.IsNaN(f))
                    float.IsNaN(frame.SensorHealth).Should().BeTrue();
                else
                    frame.SensorHealth.Should().Be(f);
            }

            foreach (var h in halves)
            {
                var frameBuf = WriteDefaultFrame(TEMPERATURE, PRESSURE, SENSOR_HEALTH, h);
                var stream = ShiftRight(frameBuf, offset);
                var extracted = ExtractFrame(stream, offset, TestTelemetryFrame.SIZE_IN_BYTES);
                var frame = new TestTelemetryFrame(extracted);

                if (Half.IsNaN(h))
                    Half.IsNaN(frame.Humidity).Should().BeTrue();
                else
                    frame.Humidity.Should().Be(h);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  8. Empty Buffer — Fresh buffer reads as all-zeros/false
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void EmptyBuffer_AllFieldsDefault()
    {
        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);

        frame.SOF.Should().BeFalse();
        frame.Temperature.Should().Be(0.0);
        frame.Sep1.Should().BeFalse();
        frame.Pressure.Should().Be(0.0);
        frame.Sep2.Should().BeFalse();
        frame.SensorHealth.Should().Be(0.0f);
        frame.Sep3.Should().BeFalse();
        frame.Humidity.Should().Be((Half)0.0);
        frame.EOF.Should().BeFalse();
    }

    [Fact]
    public void EmptyBuffer_IEEE754_AllZero()
    {
        var buf = new byte[TestTelemetryFrame.SIZE_IN_BYTES];
        var frame = new TestTelemetryFrame(buf);

        frame.TemperatureIEEE.IsZero.Should().BeTrue();
        frame.PressureIEEE.IsZero.Should().BeTrue();
        frame.SensorHealthIEEE.IsZero.Should().BeTrue();
        frame.HumidityIEEE.IsZero.Should().BeTrue();
    }
}
