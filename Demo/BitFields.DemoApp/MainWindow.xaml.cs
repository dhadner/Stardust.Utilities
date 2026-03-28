using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Stardust.Utilities;

namespace BitFields.DemoApp;

public partial class MainWindow : Window
{
    private CpuStatusRegister _statusRegister;
    private bool _suppressCpuUpdate;
    private string? _activePacketField;
    private string? _activePeField;
    private string? _activeCpuField;

    private FileSystemWatcher? _rfcAssemblyWatcher;
    private Timer? _rfcReloadDebounce;
    private string? _rfcWatchedFilePath;

    private static readonly Color[] Palette =
    [
        Rgb(0x61,0xAF,0xEF), Rgb(0xC6,0x78,0xDD), Rgb(0x56,0x9C,0x3B), Rgb(0xC4,0x8A,0x1A),
        Rgb(0xE0,0x6C,0x75), Rgb(0x2E,0x9E,0xAF), Rgb(0xC0,0x7A,0x30), Rgb(0xBE,0x50,0x46),
        Rgb(0x3E,0x9C,0xBB), Rgb(0xDB,0x50,0x9E), Rgb(0x30,0xA0,0x50), Rgb(0x9E,0x6B,0xD1),
        Rgb(0xBD,0x93,0xF9), Rgb(0xD0,0x86,0x30), Rgb(0x2F,0x8D,0xA8), Rgb(0xFF,0x55,0x55),
        Rgb(0x6A,0x99,0x55), Rgb(0xAA,0x6E,0xAB), Rgb(0x3F,0x7F,0x3F), Rgb(0x7E,0x80,0x86),
    ];

    public MainWindow()
    {
        InitializeComponent();
        InitRfcTab();
        InitFpTab();
        SeedPacketSample();
        UpdateCpuUi();
        MixedEndianSummary.Text = MixedEndianDemo.Summarize();
        LoadDemoPeFile();
    }

    private void SeedPacketSample()
    {
        var sample = SampleData.SamplePacket;
        PacketHexInput.Text = HexUtils.ToHex(sample);
    }

    private void LoadDemoPeFile()
    {
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            LoadPeFile(exePath);
    }

    // ?? PE Viewer ??????????????????????????????????????????????

    private void OnOpenPeFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open PE File",
            Filter = "Executable Files (*.exe;*.dll)|*.exe;*.dll|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        LoadPeFile(dialog.FileName);
    }

    private void LoadPeFile(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        PeFilePath.Text = filePath;
        PeRawBytes.Text = HexUtils.ToHex(bytes, 32768);

        PeFieldSummaryPanel.Children.Clear();
        PeHexBytesPanel.Children.Clear();
        PeBinaryBitsPanel.Children.Clear();

        // Parse PE headers using Result pipeline
        PeParser.Parse(bytes)
            .OnSuccess(pe => PopulatePeDisplay(pe))
            .OnFailure(error => AddInfoCard(PeFieldSummaryPanel, "Error", error, Colors.Red));
    }

    private void PopulatePeDisplay(PeParseResult pe)
    {
        var bytes = pe.Bytes;
        var allFields = new List<FieldDef>();
        int ci = 0;

        // DOS header fields
        foreach (var m in DosHeaderView.Fields)
            allFields.Add(new FieldDef(m.Name, m.Start, m.End, Palette[ci++ % Palette.Length], FormatViewField(pe.Dos, m), m.BitOrder, m.GetDescription()));

        // PE signature
        int sigBitBase = pe.PeOffset * 8;
        allFields.Add(new FieldDef("PE Sig", sigBitBase, sigBitBase + 31, Palette[ci++ % Palette.Length],
            pe.Signature == PeHeader.SIGNATURE ? "PE\\0\\0" : $"0x{pe.Signature:X8}", Description: "PE signature magic bytes ('PE\\0\\0' = 0x00004550)"));

        // COFF header fields
        int coffBitBase = pe.CoffByteOffset * 8;
        foreach (var m in CoffHeaderView.Fields)
            allFields.Add(new FieldDef(m.Name, m.Start + coffBitBase, m.End + coffBitBase, Palette[ci++ % Palette.Length], FormatViewField(pe.Coff, m), m.BitOrder, m.GetDescription()));

        // Optional header fields (if present)
        if (pe.Optional is { } opt)
        {
            int optBitBase = pe.OptByteOffset * 8;
            foreach (var m in OptionalHeaderView.Fields)
            {
                int globalStart = m.Start + optBitBase;
                int globalEnd = m.End + optBitBase;
                if (globalEnd / 8 < pe.TotalDisplayBytes)
                    allFields.Add(new FieldDef(m.Name, globalStart, globalEnd, Palette[ci++ % Palette.Length], FormatViewField(opt, m), m.BitOrder, m.GetDescription()));
            }
        }

        _activePeField = null;
        PopulateFieldSummary(PeFieldSummaryPanel, allFields, nameof(_activePeField));
        PopulateHexDisplay(PeHexBytesPanel, bytes, pe.TotalDisplayBytes, allFields, nameof(_activePeField));
        PopulateBinaryDisplay(PeBinaryBitsPanel, bytes, pe.TotalDisplayBytes, allFields, nameof(_activePeField));
    }

    // ?? Network Packet Viewer ??????????????????????????????????

    private void OnPacketHexInputChanged(object sender, TextChangedEventArgs e)
    {
        // Auto-parse whenever the input text changes
        ParsePacket();
    }

    private void OnParsePacket(object sender, RoutedEventArgs e) => ParsePacket();

    private void ParsePacket()
    {
        FieldSummaryPanel.Children.Clear();
        HexBytesPanel.Children.Clear();
        BinaryBitsPanel.Children.Clear();

        if (!HexUtils.TryParseHex(PacketHexInput.Text, out var bytes))
        {
            HttpPayload.Text = "Invalid hex input.";
            return;
        }

        if (bytes.Length < IPv4HeaderView.SIZE_IN_BYTES)
        {
            HttpPayload.Text = "Packet too small.";
            return;
        }

        var ip = new IPv4HeaderView(bytes);
        int tcpOffset = ip.HeaderLengthBytes;

        if (bytes.Length < tcpOffset + TcpHeaderView.SIZE_IN_BYTES)
        {
            HttpPayload.Text = "Packet missing TCP header.";
            return;
        }

        var tcp = new TcpHeaderView(bytes, tcpOffset);
        int totalHeaderBytes = Math.Min(tcpOffset + tcp.HeaderLengthBytes, bytes.Length);
        int tcpBitBase = tcpOffset * 8;

        // Build field list from generated metadata
        var fields = new List<FieldDef>();
        int ci = 0;

        foreach (var m in IPv4HeaderView.Fields)
        {
            string val = FormatViewField(ip, m);
            fields.Add(new FieldDef(m.Name, m.Start, m.End, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
        }

        foreach (var m in TcpHeaderView.Fields)
        {
            string val = FormatViewField(tcp, m);
            fields.Add(new FieldDef(m.Name, m.Start + tcpBitBase, m.End + tcpBitBase, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
        }

        _activePacketField = null;
        PopulateFieldSummary(FieldSummaryPanel, fields, nameof(_activePacketField));
        PopulateHexDisplay(HexBytesPanel, bytes, totalHeaderBytes, fields, nameof(_activePacketField));
        PopulateBinaryDisplay(BinaryBitsPanel, bytes, totalHeaderBytes, fields, nameof(_activePacketField));

        int payloadOffset = tcpOffset + tcp.HeaderLengthBytes;
        HttpPayload.Text = payloadOffset < bytes.Length
            ? Encoding.ASCII.GetString(bytes, payloadOffset, bytes.Length - payloadOffset)
            : "No payload.";
    }

    // ?? CPU Register Lab ???????????????????????????????????????

    private void OnCpuRawHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressCpuUpdate)
            return;

        if (!HexUtils.TryParseUShort(CpuRawHex.Text, out var value))
            return;

        _statusRegister = value;
        UpdateCpuUi(skipHexBox: true);
    }

    private void OnCpuFlagChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressCpuUpdate)
            return;

        _statusRegister.Carry = FlagCarry.IsChecked == true;
        _statusRegister.ZeroFlag = FlagZero.IsChecked == true;
        _statusRegister.InterruptDisable = FlagInterrupt.IsChecked == true;
        _statusRegister.Decimal = FlagDecimal.IsChecked == true;
        _statusRegister.Overflow = FlagOverflow.IsChecked == true;
        _statusRegister.Negative = FlagNegative.IsChecked == true;

        string? flagName = sender switch
        {
            CheckBox cb when cb == FlagCarry     => nameof(CpuStatusRegister.Carry),
            CheckBox cb when cb == FlagZero      => nameof(CpuStatusRegister.ZeroFlag),
            CheckBox cb when cb == FlagInterrupt => nameof(CpuStatusRegister.InterruptDisable),
            CheckBox cb when cb == FlagDecimal   => nameof(CpuStatusRegister.Decimal),
            CheckBox cb when cb == FlagOverflow  => nameof(CpuStatusRegister.Overflow),
            CheckBox cb when cb == FlagNegative  => nameof(CpuStatusRegister.Negative),
            _ => null
        };

        UpdateCpuUi(highlightField: flagName);
    }

    private void OnCpuModeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressCpuUpdate)
            return;

        _statusRegister.Mode = (byte)e.NewValue;
        UpdateCpuUi(highlightField: "Mode");
    }

    private void UpdateCpuUi(bool skipHexBox = false, string? highlightField = null)
    {
        _suppressCpuUpdate = true;
        if (!skipHexBox)
            CpuRawHex.Text = $"0x{(ushort)_statusRegister:X4}";
        FlagCarry.IsChecked = _statusRegister.Carry;
        FlagZero.IsChecked = _statusRegister.ZeroFlag;
        FlagInterrupt.IsChecked = _statusRegister.InterruptDisable;
        FlagDecimal.IsChecked = _statusRegister.Decimal;
        FlagOverflow.IsChecked = _statusRegister.Overflow;
        FlagNegative.IsChecked = _statusRegister.Negative;
        CpuMode.Value = _statusRegister.Mode;
        CpuModeValue.Text = _statusRegister.Mode.ToString();
        _suppressCpuUpdate = false;

        BuildCpuFieldDisplay(highlightField);
    }

    private void BuildCpuFieldDisplay(string? highlightField = null)
    {
        var fields = new List<FieldDef>();
        int ci = 0;

        foreach (var m in CpuStatusRegister.Fields)
        {
            string val = FormatViewField(_statusRegister, m);
            fields.Add(new FieldDef(m.Name, m.Start, m.End, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
        }

        var bytes = BitConverter.GetBytes((ushort)_statusRegister);

        _activeCpuField = highlightField;
        PopulateFieldSummary(CpuFieldSummaryPanel, fields, nameof(_activeCpuField));
        PopulateHexDisplay(CpuHexBytesPanel, bytes, bytes.Length, fields, nameof(_activeCpuField));
        PopulateBinaryDisplay(CpuBinaryBitsPanel, bytes, bytes.Length, fields, nameof(_activeCpuField));

        if (highlightField != null)
        {
            Panel[] panels = [CpuFieldSummaryPanel, CpuHexBytesPanel, CpuBinaryBitsPanel];
            foreach (var p in panels)
                ApplyHighlighting(p, highlightField);
        }
    }

    // ── RFC Diagram ──────────────────────────────────────────────

    // ── FP Stream Lab ────────────────────────────────────────────

    private bool _suppressFpUpdate;
    private string? _activeFpField;
    private byte[] _fpStreamBuffer = [];

    private void InitFpTab()
    {
        _suppressFpUpdate = true;
        FpOffsetSlider.Value = 0;
        FpTemperature.Text = TelemetryStreamDemo.Defaults.TEMPERATURE.ToString();
        FpPressure.Text = TelemetryStreamDemo.Defaults.PRESSURE.ToString();
        FpHealth.Text = TelemetryStreamDemo.Defaults.SENSOR_HEALTH.ToString();
        _suppressFpUpdate = false;
        RebuildFpStream();
    }

    private void OnFpOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressFpUpdate) return;
        int offset = (int)e.NewValue;
        FpOffsetValue.Text = offset.ToString();
        FpOffsetBadge.Background = new SolidColorBrush(FpOffsetColor(offset));
        RebuildFpStream();
    }

    private void OnFpValueChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFpUpdate) return;
        RebuildFpStream();
    }

    private void OnFpReset(object sender, RoutedEventArgs e)
    {
        _suppressFpUpdate = true;
        FpOffsetSlider.Value = 0;
        FpTemperature.Text = TelemetryStreamDemo.Defaults.TEMPERATURE.ToString();
        FpPressure.Text = TelemetryStreamDemo.Defaults.PRESSURE.ToString();
        FpHealth.Text = TelemetryStreamDemo.Defaults.SENSOR_HEALTH.ToString();
        FpHumidity.Text = TelemetryStreamDemo.Defaults.HUMIDITY.ToString();
        FpStreamLog.Text = "";
        _suppressFpUpdate = false;
        RebuildFpStream();
    }

    private void OnFpSimulate(object sender, RoutedEventArgs e)
    {
        int offset = (int)FpOffsetSlider.Value;
        var sb = new StringBuilder();
        var rng = new Random(42);

        if (!double.TryParse(FpTemperature.Text, out var baseTemp)) baseTemp = TelemetryStreamDemo.Defaults.TEMPERATURE;
        if (!double.TryParse(FpPressure.Text, out var basePres)) basePres = TelemetryStreamDemo.Defaults.PRESSURE;
        if (!float.TryParse(FpHealth.Text, out var baseHealth)) baseHealth = TelemetryStreamDemo.Defaults.SENSOR_HEALTH;
        if (!Half.TryParse(FpHumidity.Text, out var baseHm)) baseHm = TelemetryStreamDemo.Defaults.HUMIDITY;

        for (int i = 0; i < 10; i++)
        {
            double temp = baseTemp + (rng.NextDouble() - 0.5) * 4.0;
            double pres = basePres + (rng.NextDouble() - 0.5) * 20.0;
            float health = Math.Clamp(baseHealth + (float)(rng.NextDouble() - 0.5) * 0.1f, 0f, 1f);
            Half ck = (Half)((double)baseHm + (rng.NextDouble() - 0.5) * 0.5);

            var fb = new byte[TelemetryStreamDemo.FrameBytes];
            TelemetryStreamDemo.WriteFrame(fb, temp, pres, health, ck);
            var stream = TelemetryStreamDemo.ShiftRight(fb, offset);
            var extracted = TelemetryStreamDemo.ExtractFrame(stream, offset, TelemetryStreamDemo.FrameBytes);
            var rb = TelemetryStreamDemo.ReadFrame(extracted);

            bool ok = rb.Temperature == temp && rb.Pressure == pres
                   && rb.SensorHealth == health && rb.Humidity == ck;

            sb.AppendLine($"Frame {i + 1,2}: T={rb.Temperature,10:F4}°C  P={rb.Pressure,9:F2}hPa  " +
                          $"H={rb.SensorHealth:F4}  Hm={rb.Humidity,6}  [{(ok ? "\u2705 OK" : "\u274C MISMATCH")}]  " +
                          $"({stream.Length} bytes, {offset}-bit offset)");
        }

        FpStreamLog.Text = sb.ToString();
    }

    private void RebuildFpStream()
    {
        if (FpTemperature == null || FpPressure == null || FpHealth == null || FpHumidity == null
            || FpVerifyGrid == null || FpFieldSummaryPanel == null || FpRoundTripPanel == null
            || FpStructDesc == null || FpRfcDiagram == null)
            return;

        if (!double.TryParse(FpTemperature.Text, out var temperature)) temperature = TelemetryStreamDemo.Defaults.TEMPERATURE;
        if (!double.TryParse(FpPressure.Text, out var pressure)) pressure = TelemetryStreamDemo.Defaults.PRESSURE;
        if (!float.TryParse(FpHealth.Text, out var health)) health = TelemetryStreamDemo.Defaults.SENSOR_HEALTH;
        if (!Half.TryParse(FpHumidity.Text, out var humidity)) humidity = TelemetryStreamDemo.Defaults.HUMIDITY;
        int offset = (int)FpOffsetSlider.Value;

        // Write → Shift → Extract → Read
        var frameBuffer = new byte[TelemetryStreamDemo.FrameBytes];
        TelemetryStreamDemo.WriteFrame(frameBuffer, temperature, pressure, health, humidity);
        _fpStreamBuffer = TelemetryStreamDemo.ShiftRight(frameBuffer, offset);
        var extracted = TelemetryStreamDemo.ExtractFrame(_fpStreamBuffer, offset, TelemetryStreamDemo.FrameBytes);
        var readback = TelemetryStreamDemo.ReadFrame(extracted);
        var ieee = TelemetryStreamDemo.ReadIEEE(extracted);

        FpStreamInfo.Text = $"Frame: {TelemetryStreamDemo.FrameBytes} bytes \u00b7 Stream: {_fpStreamBuffer.Length} bytes \u00b7 Offset: {offset} bits";

        bool allMatch = temperature == readback.Temperature
                     && pressure == readback.Pressure
                     && health == readback.SensorHealth
                     && humidity == readback.Humidity
                     && readback.SOF && readback.EOF;

        var desc = TelemetryStreamDemo.GetDescription();
        FpStructDesc.Text = desc ?? "";
        FpStructDesc.Visibility = string.IsNullOrEmpty(desc) ? Visibility.Collapsed : Visibility.Visible;

        BuildRoundTripSteps(offset, allMatch);

        FpVerifyGrid.ItemsSource = new List<FpVerifyRow>
        {
            new("Temperature",   temperature.ToString(),  readback.Temperature.ToString(),  temperature  == readback.Temperature  ? "\u2705" : "\u274C"),
            new("Pressure",      pressure.ToString(),     readback.Pressure.ToString(),     pressure     == readback.Pressure     ? "\u2705" : "\u274C"),
            new("Sensor Health", health.ToString(),       readback.SensorHealth.ToString(), health       == readback.SensorHealth ? "\u2705" : "\u274C"),
            new("Humidity",      humidity.ToString(),     readback.Humidity.ToString(),     humidity     == readback.Humidity     ? "\u2705" : "\u274C"),
            new("SOF",           "True",                  readback.SOF.ToString(),          readback.SOF                         ? "\u2705" : "\u274C"),
            new("EOF",           "True",                  readback.EOF.ToString(),          readback.EOF                         ? "\u2705" : "\u274C"),
        };

        // Field summary with offset-adjusted positions, sorted by start bit;
        // IEEE overlay fields share their native field's color.
        var ieeeToNative = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TemperatureIEEE"] = "Temperature",
            ["PressureIEEE"] = "Pressure",
            ["SensorHealthIEEE"] = "SensorHealth",
            ["HumidityIEEE"] = "Humidity",
        };
        var fields = new List<FieldDef>();
        int ci = 0;
        var nativeColors = new Dictionary<string, Color>(StringComparer.Ordinal);
        foreach (var m in TelemetryStreamDemo.GetFields())
        {
            bool isIeee = ieeeToNative.ContainsKey(m.Name);
            Color color;
            if (isIeee)
            {
                color = nativeColors[ieeeToNative[m.Name]];
            }
            else
            {
                color = Palette[ci++ % Palette.Length];
                nativeColors[m.Name] = color;
            }
            string val = FormatFrameField(readback, ieee, m.Name);
            fields.Add(new FieldDef(m.Name, m.Start + offset, m.End + offset, color, val, Description: m.GetDescription()));
        }
        fields.Sort((a, b) =>
        {
            int cmp = a.Start.CompareTo(b.Start);
            return cmp != 0 ? cmp : (ieeeToNative.ContainsKey(a.Name) ? 1 : 0).CompareTo(ieeeToNative.ContainsKey(b.Name) ? 1 : 0);
        });

        // Physical fields only (no IEEE overlays) for hex/binary display
        var physicalFields = fields.Where(f => !ieeeToNative.ContainsKey(f.Name)).ToList();

        // Color the input textbox borders to match their field colors
        ColorInputBorder(FpTemperature, nativeColors.GetValueOrDefault("Temperature"));
        ColorInputBorder(FpPressure, nativeColors.GetValueOrDefault("Pressure"));
        ColorInputBorder(FpHealth, nativeColors.GetValueOrDefault("SensorHealth"));
        ColorInputBorder(FpHumidity, nativeColors.GetValueOrDefault("Humidity"));

        _activeFpField = null;
        PopulateFpFieldSummary(FpFieldSummaryPanel, fields, ieeeToNative, ieee,
            temperature, pressure, health, humidity);
        PopulateHexDisplay(FpHexBytesPanel, _fpStreamBuffer, _fpStreamBuffer.Length, physicalFields, nameof(_activeFpField));
        PopulateBinaryDisplay(FpBinaryBitsPanel, _fpStreamBuffer, _fpStreamBuffer.Length, physicalFields, nameof(_activeFpField));

        var diag = new BitFieldDiagram(TelemetryStreamDemo.GetViewType(), bitsPerRow: 32, includeDescriptions: true);
        FpRfcDiagram.Text = diag.RenderToString().Match(s => s, e => $"Diagram error: {e}");
    }

    private static string FormatFrameField(TelemetryReadback rb, IEEEReadback ieee, string name)
    {
        return name switch
        {
            "SOF" or "Sep1" or "Sep2" or "Sep3" or "EOF" => rb.SOF.ToString(),
            "Temperature" => rb.Temperature.ToString(),
            "Pressure" => rb.Pressure.ToString(),
            "SensorHealth" => rb.SensorHealth.ToString(),
            "Humidity" => rb.Humidity.ToString(),
            "TemperatureIEEE" => TelemetryStreamDemo.Classify(ieee.Temperature),
            "PressureIEEE" => TelemetryStreamDemo.Classify(ieee.Pressure),
            "SensorHealthIEEE" => TelemetryStreamDemo.Classify(ieee.SensorHealth),
            "HumidityIEEE" => TelemetryStreamDemo.Classify(ieee.Humidity),
            _ => "?"
        };
    }

    private void BuildRoundTripSteps(int offsetBits, bool allMatch)
    {
        FpRoundTripPanel.Children.Clear();
        AddRoundTripStep(FpRoundTripPanel, 1, "Write frame to buffer at bit 0",                             Rgb(0x61, 0xAF, 0xEF));
        AddRoundTripArrow(FpRoundTripPanel);
        AddRoundTripStep(FpRoundTripPanel, 2, $"Shift right by {offsetBits} bit{(offsetBits != 1 ? "s" : "")}", Rgb(0xC6, 0x78, 0xDD));
        AddRoundTripArrow(FpRoundTripPanel);
        AddRoundTripStep(FpRoundTripPanel, 3, "Extract frame from offset",                                  Rgb(0x7A, 0xC0, 0x4A));
        AddRoundTripArrow(FpRoundTripPanel);
        AddRoundTripStep(FpRoundTripPanel, 4,
            allMatch ? "All fields match! \u2705" : "MISMATCH \u274C",
            allMatch ? Rgb(0x50, 0xC8, 0x78) : Rgb(0xE0, 0x6C, 0x75));
    }

    private static void AddRoundTripStep(Panel panel, int num, string text, Color numColor)
    {
        var numCircle = new Border
        {
            Width = 26, Height = 26,
            CornerRadius = new CornerRadius(13),
            Background = new SolidColorBrush(numColor),
            Child = new TextBlock
            {
                Text = num.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = Brushes.White
            }
        };
        var stepInner = new StackPanel { Orientation = Orientation.Horizontal };
        stepInner.Children.Add(numCircle);
        stepInner.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        });
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 14, 6),
            Margin = new Thickness(0, 0, 0, 4),
            Child = stepInner
        });
    }

    private static void AddRoundTripArrow(Panel panel)
    {
        panel.Children.Add(new TextBlock
        {
            Text = "\u27A1",
            FontSize = 16,
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        });
    }

    private void PopulateFpFieldSummary(Panel panel, List<FieldDef> fields,
        Dictionary<string, string> ieeeToNative, IEEEReadback ieee,
        double temperature, double pressure, float health, Half humidity)
    {
        panel.Children.Clear();
        foreach (var f in fields)
        {
            bool isIeee = ieeeToNative.ContainsKey(f.Name);
            if (isIeee)
            {
                AddEditableIeeeChip(panel, f, ieee, temperature, pressure, health, humidity);
            }
            else
            {
                // Standard chip (same as PopulateFieldSummary)
                var brush = new SolidColorBrush(f.Color);
                var bg = new SolidColorBrush(Color.FromArgb(0x30, f.Color.R, f.Color.G, f.Color.B));
                var nameBlock = new TextBlock { Text = f.Name, FontSize = 10, Foreground = brush, FontWeight = FontWeights.Bold };
                var valueBlock = new TextBlock { Text = f.Value, FontFamily = new FontFamily("Consolas"), FontSize = 13 };
                var stack = new StackPanel();
                stack.Children.Add(nameBlock);
                stack.Children.Add(valueBlock);
                var border = new Border
                {
                    Background = bg, BorderBrush = brush, BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 6, 6), Child = stack,
                    Tag = new FieldTag(f.Name, nameof(_activeFpField), f.Color), Cursor = Cursors.Hand,
                    ToolTip = f.Description
                };
                border.MouseLeftButtonDown += OnFieldClicked;
                panel.Children.Add(border);
            }
        }
    }

    private void AddEditableIeeeChip(Panel panel, FieldDef f, IEEEReadback ieee,
        double temperature, double pressure, float health, Half humidity)
    {
        var brush = new SolidColorBrush(f.Color);
        var bg = new SolidColorBrush(Color.FromArgb(0x25, f.Color.R, f.Color.G, f.Color.B));
        var mono = new FontFamily("Consolas");

        // Decompose IEEE for this field
        bool sign; int biasedExp; int? unbiasedExp; string mantissaHex; string classify; int expMax; string typeName; int expBits; int manBits; int expBias; string nativeValue;
        switch (f.Name)
        {
            case "TemperatureIEEE":
            { var d = ieee.Temperature; sign = d.Sign; biasedExp = d.BiasedExponent; unbiasedExp = d.Exponent; mantissaHex = $"0x{d.Mantissa:X13}"; classify = TelemetryStreamDemo.Classify(d); expMax = 2047; typeName = "IEEE754Double (64-bit)"; expBits = 11; manBits = 52; expBias = IEEE754Double.EXPONENT_BIAS; nativeValue = double.TryParse(FpTemperature.Text, out var tv) ? tv.ToString() : "?"; break; }
            case "PressureIEEE":
            { var d = ieee.Pressure; sign = d.Sign; biasedExp = d.BiasedExponent; unbiasedExp = d.Exponent; mantissaHex = $"0x{d.Mantissa:X13}"; classify = TelemetryStreamDemo.Classify(d); expMax = 2047; typeName = "IEEE754Double (64-bit)"; expBits = 11; manBits = 52; expBias = IEEE754Double.EXPONENT_BIAS; nativeValue = double.TryParse(FpPressure.Text, out var pv) ? pv.ToString() : "?"; break; }
            case "SensorHealthIEEE":
            { var s = ieee.SensorHealth; sign = s.Sign; biasedExp = s.BiasedExponent; unbiasedExp = s.Exponent; mantissaHex = $"0x{s.Mantissa:X6}"; classify = TelemetryStreamDemo.Classify(s); expMax = 255; typeName = "IEEE754Single (32-bit)"; expBits = 8; manBits = 23; expBias = IEEE754Single.EXPONENT_BIAS; nativeValue = float.TryParse(FpHealth.Text, out var hv) ? hv.ToString() : "?"; break; }
            case "HumidityIEEE":
            { var h = ieee.Humidity; sign = h.Sign; biasedExp = h.BiasedExponent; unbiasedExp = h.Exponent; mantissaHex = $"0x{h.Mantissa:X3}"; classify = TelemetryStreamDemo.Classify(h); expMax = 31; typeName = "IEEE754Half (16-bit)"; expBits = 5; manBits = 10; expBias = IEEE754Half.EXPONENT_BIAS; nativeValue = Half.TryParse(FpHumidity.Text, out var hmv) ? hmv.ToString() : "?"; break; }
            default: return;
        }

        var classColor = classify == "Normal" ? Colors.LimeGreen : classify is "NaN" or "Denorm" || classify.Contains('\u221E') ? Colors.Tomato : Colors.Gray;

        var stack = new StackPanel();

        // Header: field name + type + classification
        var header = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
        header.Children.Add(new TextBlock { Text = f.Name, FontSize = 10, Foreground = brush, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(new TextBlock { Text = typeName, FontSize = 9, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        header.Children.Add(new TextBlock { Text = classify, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(classColor), VerticalAlignment = VerticalAlignment.Center });
        header.Children.Add(new TextBlock { Text = $"  Native: {nativeValue}", FontSize = 9, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), FontFamily = mono });
        stack.Children.Add(header);

        // Bit bar (S / Exp / Mantissa segments)
        int totalBits = 1 + expBits + manBits;
        double barTotal = 260.0;
        double signW = Math.Max(14, barTotal / totalBits);
        double expW  = Math.Max(30, barTotal * expBits / totalBits);
        double manW  = Math.Max(30, barTotal * manBits / totalBits);
        var bitBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
        bitBar.Children.Add(new Border { Width = signW, Height = 22, Background = new SolidColorBrush(Rgb(0xE0, 0x6C, 0x75)), CornerRadius = new CornerRadius(4, 0, 0, 4), Child = new TextBlock { Text = "S", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White } });
        bitBar.Children.Add(new Border { Width = expW,  Height = 22, Background = new SolidColorBrush(Rgb(0x61, 0xAF, 0xEF)), Margin = new Thickness(2, 0, 2, 0), Child = new TextBlock { Text = $"Exp ({expBits})", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White } });
        bitBar.Children.Add(new Border { Width = manW,  Height = 22, Background = new SolidColorBrush(Rgb(0x7A, 0xC0, 0x4A)), CornerRadius = new CornerRadius(0, 4, 4, 0), Child = new TextBlock { Text = $"Man ({manBits})", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White } });
        stack.Children.Add(bitBar);

        // Sign row
        var signRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        signRow.Children.Add(new TextBlock { Text = "Sign: ", FontSize = 11, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, Width = 70 });
        var signBtn = new Button
        {
            Content = sign ? "\u2212 (1)" : "+ (0)",
            FontFamily = mono, FontSize = 11, FontWeight = FontWeights.Bold,
            Padding = new Thickness(8, 1, 8, 1), Cursor = Cursors.Hand,
            Background = new SolidColorBrush(Color.FromArgb(0x20, 0xE0, 0x6C, 0x75)),
            BorderBrush = new SolidColorBrush(Colors.Gray), BorderThickness = new Thickness(1),
            Tag = f.Name
        };
        signBtn.Click += OnFpIeeeSignToggle;
        signRow.Children.Add(signBtn);
        stack.Children.Add(signRow);

        // BiasedExponent row (editable raw value)
        var biasedExpRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        biasedExpRow.Children.Add(new TextBlock { Text = "BiasedExponent: ", FontSize = 11, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, Width = 112 });
        var expBox = new TextBox
        {
            Text = biasedExp.ToString(), FontFamily = mono, FontSize = 11, Width = 60, Height = 22,
            Padding = new Thickness(4, 1, 4, 1), VerticalContentAlignment = VerticalAlignment.Center,
            Tag = f.Name, ToolTip = $"Raw biased exponent (0\u2013{expMax})"
        };
        expBox.KeyDown += OnFpIeeeExpKeyDown;
        biasedExpRow.Children.Add(expBox);
        stack.Children.Add(biasedExpRow);

        // Exponent row (read-only, unbiased)
        var unbiasedExpRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        unbiasedExpRow.Children.Add(new TextBlock { Text = $"Exponent (bias {expBias}): ", FontSize = 11, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, Width = 112 });
        unbiasedExpRow.Children.Add(new TextBlock { Text = unbiasedExp?.ToString() ?? "n/a", FontFamily = mono, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        stack.Children.Add(unbiasedExpRow);

        // Mantissa row
        var manRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        manRow.Children.Add(new TextBlock { Text = "Mantissa: ", FontSize = 11, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, Width = 70 });
        var manBox = new TextBox
        {
            Text = mantissaHex, FontFamily = mono, FontSize = 11, Width = 120, Height = 22,
            Padding = new Thickness(4, 1, 4, 1), VerticalContentAlignment = VerticalAlignment.Center,
            Tag = f.Name, ToolTip = "Mantissa (hex)"
        };
        manBox.KeyDown += OnFpIeeeManKeyDown;
        manRow.Children.Add(manBox);
        stack.Children.Add(manRow);

        var border = new Border
        {
            Background = bg, BorderBrush = brush, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 6), Child = stack,
            Tag = new FieldTag(f.Name, nameof(_activeFpField), f.Color), Cursor = Cursors.Hand,
            ToolTip = f.Description
        };
        border.MouseLeftButtonDown += OnFieldClicked;
        panel.Children.Add(border);
    }

    private void OnFpIeeeSignToggle(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string field) return;
        switch (field)
        {
            case "TemperatureIEEE":
            { if (!double.TryParse(FpTemperature.Text, out var v)) return; IEEE754Double d = v; d.Sign = !d.Sign; FpTemperature.Text = ((double)d).ToString(); break; }
            case "PressureIEEE":
            { if (!double.TryParse(FpPressure.Text, out var v)) return; IEEE754Double d = v; d.Sign = !d.Sign; FpPressure.Text = ((double)d).ToString(); break; }
            case "SensorHealthIEEE":
            { if (!float.TryParse(FpHealth.Text, out var v)) return; IEEE754Single s = v; s.Sign = !s.Sign; FpHealth.Text = ((float)s).ToString(); break; }
            case "HumidityIEEE":
            { if (!Half.TryParse(FpHumidity.Text, out var v)) return; IEEE754Half h = v; h.Sign = !h.Sign; FpHumidity.Text = ((Half)h).ToString(); break; }
        }
    }

    private void OnFpIeeeExpKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox box || box.Tag is not string field) return;
        if (!int.TryParse(box.Text, out var exp)) return;
        switch (field)
        {
            case "TemperatureIEEE":
            { if (!double.TryParse(FpTemperature.Text, out var v)) return; IEEE754Double d = v; d.BiasedExponent = (ushort)Math.Clamp(exp, 0, 2047); FpTemperature.Text = ((double)d).ToString(); break; }
            case "PressureIEEE":
            { if (!double.TryParse(FpPressure.Text, out var v)) return; IEEE754Double d = v; d.BiasedExponent = (ushort)Math.Clamp(exp, 0, 2047); FpPressure.Text = ((double)d).ToString(); break; }
            case "SensorHealthIEEE":
            { if (!float.TryParse(FpHealth.Text, out var v)) return; IEEE754Single s = v; s.BiasedExponent = (byte)Math.Clamp(exp, 0, 255); FpHealth.Text = ((float)s).ToString(); break; }
            case "HumidityIEEE":
            { if (!Half.TryParse(FpHumidity.Text, out var v)) return; IEEE754Half h = v; h.BiasedExponent = (byte)Math.Clamp(exp, 0, 31); FpHumidity.Text = ((Half)h).ToString(); break; }
        }
    }

    private void OnFpIeeeManKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox box || box.Tag is not string field) return;
        var text = box.Text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        if (!ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out var man)) return;
        switch (field)
        {
            case "TemperatureIEEE":
            { if (!double.TryParse(FpTemperature.Text, out var v)) return; IEEE754Double d = v; d.Mantissa = man; FpTemperature.Text = ((double)d).ToString(); break; }
            case "PressureIEEE":
            { if (!double.TryParse(FpPressure.Text, out var v)) return; IEEE754Double d = v; d.Mantissa = man; FpPressure.Text = ((double)d).ToString(); break; }
            case "SensorHealthIEEE":
            { if (!float.TryParse(FpHealth.Text, out var v)) return; IEEE754Single s = v; s.Mantissa = (uint)man; FpHealth.Text = ((float)s).ToString(); break; }
            case "HumidityIEEE":
            { if (!Half.TryParse(FpHumidity.Text, out var v)) return; IEEE754Half h = v; h.Mantissa = (ushort)man; FpHumidity.Text = ((Half)h).ToString(); break; }
        }
    }

    private static void AddIeeeCard(Panel panel, string field, bool sign, int? exponent, string mantissa, string classify, Color color)
    {
        var brush = new SolidColorBrush(color);
        var bg = new SolidColorBrush(Color.FromArgb(0x25, color.R, color.G, color.B));

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = field, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = brush });
        stack.Children.Add(new TextBlock { Text = $"{(sign ? "\u2212" : "+")} exp={exponent?.ToString() ?? "n/a"}", FontFamily = new FontFamily("Consolas"), FontSize = 12 });
        stack.Children.Add(new TextBlock { Text = $"m={mantissa}", FontFamily = new FontFamily("Consolas"), FontSize = 12 });
        stack.Children.Add(new TextBlock { Text = classify, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Colors.LimeGreen) });

        panel.Children.Add(new Border
        {
            Background = bg, BorderBrush = brush, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 6), Child = stack
        });
    }

    private static Color FpOffsetColor(int offset) => offset switch
    {
        0 => Rgb(0x50, 0xC8, 0x78),
        1 or 2 => Rgb(0x61, 0xAF, 0xEF),
        3 or 4 => Rgb(0xD4, 0xA0, 0x34),
        5 or 6 => Rgb(0xE0, 0x6C, 0x75),
        7 => Rgb(0xC6, 0x78, 0xDD),
        _ => Rgb(0x88, 0x88, 0x88),
    };

    private static void ColorInputBorder(TextBox? box, Color? fieldColor)
    {
        if (box == null || fieldColor == null) return;
        var c = fieldColor.Value;
        box.BorderBrush = new SolidColorBrush(c);
        box.Background = new SolidColorBrush(Color.FromArgb(0x18, c.R, c.G, c.B));
    }

    // ── RFC Diagram ──────────────────────────────────────────────

    private BitFieldDiagram[] _diagramSources = [];
    private BitFieldDiagram[] _builtInDiagramSources = [];

    private static BitFieldDiagram Single(string label, Type bitType) =>
        new(bitType, label);

    private void InitRfcTab()
    {
        _diagramSources =
        [
            Single("IPv4 Header", typeof(IPv4HeaderView)),
            Single("TCP Header", typeof(TcpHeaderView)),
            Single("DOS Header", typeof(DosHeaderView)),
            Single("COFF Header", typeof(CoffHeaderView)),
            Single("Optional Header", typeof(OptionalHeaderView)),
            Single("CPU Status Register", typeof(CpuStatusRegister)),
            Single("Telemetry Frame (Composable FP)", typeof(TelemetryFrame)),
            Single("IEEE 754 Half (16-bit)", typeof(IEEE754Half)),
            Single("IEEE 754 Single (32-bit)", typeof(IEEE754Single)),
            Single("IEEE 754 Double (64-bit)", typeof(IEEE754Double)),
            Single("Decimal (128-bit)", typeof(DecimalBitFields)),
            new(
            [
                typeof(M68020DataRegisters),
                typeof(M68020AddressRegisters),
                typeof(M68020PC),
                typeof(M68020SR),
                typeof(M68020CCR),
                typeof(M68020USP),
                typeof(M68020ISP),
                typeof(M68020MSP),
                typeof(M68020VBR),
                typeof(M68020SFC),
                typeof(M68020DFC),
                typeof(M68020CACR),
                typeof(M68020CAAR),
            ],
            "68020 Register Set")
        ];

        _builtInDiagramSources = _diagramSources;

        RfcStructPicker.Items.Clear();
        foreach (var s in _diagramSources)
            RfcStructPicker.Items.Add(s.Description);
        RfcStructPicker.SelectedIndex = 0;

        RfcBitsPerRow.Items.Clear();
        RfcBitsPerRow.Items.Add("8");
        RfcBitsPerRow.Items.Add("16");
        RfcBitsPerRow.Items.Add("32");
        RfcBitsPerRow.Items.Add("64");
        RfcBitsPerRow.SelectedIndex = 2; // default 32

        RfcCommentStyle.Items.Clear();
        RfcCommentStyle.Items.Add("None");
        RfcCommentStyle.Items.Add("* (asterisk)");
        RfcCommentStyle.Items.Add("// (double-slash)");
        RfcCommentStyle.Items.Add("/// (triple-slash)");
        RfcCommentStyle.SelectedIndex = 0;
    }

    private void OnRfcStructChanged(object sender, RoutedEventArgs e) => UpdateRfcDiagram();
    private void OnRfcStructChanged(object sender, SelectionChangedEventArgs e) => UpdateRfcDiagram();

    private void UpdateRfcDiagram()
    {
        if (RfcStructPicker.SelectedIndex < 0 || RfcBitsPerRow.SelectedItem == null)
            return;

        var source = _diagramSources[RfcStructPicker.SelectedIndex];
        source.BitsPerRow = int.Parse((string)RfcBitsPerRow.SelectedItem);
        source.IncludeDescriptions = RfcShowDescriptions.IsChecked == true;
        source.ShowByteOffset = RfcShowByteOffset.IsChecked == true;
        source.CommentPrefix = RfcCommentStyle.SelectedIndex switch
        {
            1 => "* ",
            2 => "// ",
            3 => "/// ",
            _ => null
        };

        RfcDiagramOutput.Text = source.RenderToString().Value;
    }

private void OnCopyRfcDiagram(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(RfcDiagramOutput.Text))
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Clipboard.SetText(RfcDiagramOutput.Text);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }

    private void OnOpenAssemblyForRfc(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open .NET Assembly",
            Filter = "Assemblies (*.dll;*.exe)|*.dll;*.exe|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        LoadAssemblyForRfc(dialog.FileName);
    }

    private void LoadAssemblyForRfc(string filePath)
    {
        var result = AssemblyStructDiscovery.Discover(filePath);

        if (result.Error != null && result.Structs.Count == 0)
        {
            RfcAssemblyStatus.Text = $"{result.AssemblyName}: {result.Error}";
            RfcAssemblyStatus.Visibility = Visibility.Visible;
            return;
        }

        // Remember the currently selected struct name so we can restore it after reload
        string? previousSelection = RfcStructPicker.SelectedItem as string;

        // Replace the dropdown with discovered structs
        var sources = new List<BitFieldDiagram>();
        foreach (var s in result.Structs)
            sources.Add(Single(s.DisplayName, s.BitType));

        _diagramSources = sources.ToArray();

        RfcStructPicker.Items.Clear();
        foreach (var s in _diagramSources)
            RfcStructPicker.Items.Add(s.Description);

        // Restore previous selection if the struct still exists, otherwise pick the first
        int restoredIndex = -1;
        if (previousSelection != null)
        {
            for (int i = 0; i < _diagramSources.Length; i++)
            {
                if (_diagramSources[i].Description == previousSelection)
                {
                    restoredIndex = i;
                    break;
                }
            }
        }
        RfcStructPicker.SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;

        RfcAssemblyStatus.Text = $"Loaded {result.Structs.Count} struct(s) from {result.AssemblyName} — watching for changes";
        RfcAssemblyStatus.Visibility = Visibility.Visible;
        RfcResetButton.Visibility = Visibility.Visible;

        // Start watching the file for rebuild changes
        WatchAssemblyFile(filePath);
    }

    private void WatchAssemblyFile(string filePath)
    {
        // Clean up any previous watcher
        StopWatchingAssembly();

        _rfcWatchedFilePath = filePath;
        string? dir = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileName(filePath);
        if (dir == null) return;

        _rfcAssemblyWatcher = new FileSystemWatcher(dir, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _rfcAssemblyWatcher.Changed += OnWatchedAssemblyChanged;
        _rfcAssemblyWatcher.Created += OnWatchedAssemblyChanged;
    }

    private void StopWatchingAssembly()
    {
        _rfcReloadDebounce?.Dispose();
        _rfcReloadDebounce = null;

        if (_rfcAssemblyWatcher != null)
        {
            _rfcAssemblyWatcher.EnableRaisingEvents = false;
            _rfcAssemblyWatcher.Dispose();
            _rfcAssemblyWatcher = null;
        }

        _rfcWatchedFilePath = null;
    }

    private void OnWatchedAssemblyChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: builds write the file multiple times in quick succession.
        // Wait 500ms after the last change before reloading.
        _rfcReloadDebounce?.Dispose();
        _rfcReloadDebounce = new Timer(_ =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_rfcWatchedFilePath != null && File.Exists(_rfcWatchedFilePath))
                    LoadAssemblyForRfc(_rfcWatchedFilePath);
            });
        }, null, 500, Timeout.Infinite);
    }

    private void OnResetRfcStructs(object sender, RoutedEventArgs e)
    {
        StopWatchingAssembly();

        _diagramSources = _builtInDiagramSources;

        RfcStructPicker.Items.Clear();
        foreach (var s in _diagramSources)
            RfcStructPicker.Items.Add(s.Description);
        RfcStructPicker.SelectedIndex = 0;

        RfcAssemblyStatus.Visibility = Visibility.Collapsed;
        RfcResetButton.Visibility = Visibility.Collapsed;
    }

    // ?? Shared Three-Panel Display ?????????????????????????????

    private sealed record FieldDef(string Name, int Start, int End, Color Color, string Value, BitOrder BitOrder = BitOrder.BitZeroIsLsb, string? Description = null);
    private sealed record FieldTag(string Name, string Group, Color Color);
    private sealed record HexByteTag(string PrimaryName, string Group, Color PrimaryColor, List<(string Name, Color Color)> OverlappingFields);
    private sealed record FpVerifyRow(string Field, string Written, string ReadBack, string Match);

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    private static void AddInfoCard(Panel panel, string title, string value, Color color)
    {
        var brush = new SolidColorBrush(color);
        var tb = new TextBlock { Text = $"{title}: {value}", Foreground = brush, FontWeight = FontWeights.Bold };
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x30, color.R, color.G, color.B)),
            BorderBrush = brush, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 6), Child = tb
        });
    }

    private void PopulateFieldSummary(Panel panel, List<FieldDef> fields, string group)
    {
        panel.Children.Clear();
        foreach (var f in fields)
        {
            var brush = new SolidColorBrush(f.Color);
            var bg = new SolidColorBrush(Color.FromArgb(0x30, f.Color.R, f.Color.G, f.Color.B));

            var nameBlock = new TextBlock { Text = f.Name, FontSize = 10, Foreground = brush, FontWeight = FontWeights.Bold };
            var valueBlock = new TextBlock { Text = f.Value, FontFamily = new FontFamily("Consolas"), FontSize = 13 };

            var stack = new StackPanel();
            stack.Children.Add(nameBlock);
            stack.Children.Add(valueBlock);

            var border = new Border
            {
                Background = bg, BorderBrush = brush, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 6), Child = stack,
                Tag = new FieldTag(f.Name, group, f.Color), Cursor = Cursors.Hand,
                ToolTip = f.Description
            };
            border.MouseLeftButtonDown += OnFieldClicked;
            panel.Children.Add(border);
        }
    }

    private void PopulateHexDisplay(Panel panel, byte[] bytes, int count, List<FieldDef> fields, string group)
    {
        panel.Children.Clear();
        for (int i = 0; i < count; i++)
        {
            int bitStart = i * 8;
            int bitEnd = bitStart + 7;

            // Find ALL fields that overlap this byte
            var overlapping = new List<(string Name, Color Color)>();
            string? tooltip = null;
            foreach (var f in fields)
            {
                if (f.Start <= bitEnd && f.End >= bitStart)
                {
                    overlapping.Add((f.Name, f.Color));
                    if (f.Description != null)
                        tooltip = tooltip == null ? $"{f.Name}: {f.Description}" : $"{tooltip}\n{f.Name}: {f.Description}";
                }
            }

            var primaryColor = overlapping.Count > 0 ? overlapping[0].Color : Colors.Gray;
            var primaryName = overlapping.Count > 0 ? overlapping[0].Name : null;

            var tb = new TextBlock
            {
                Text = $"{bytes[i]:X2} ", FontFamily = new FontFamily("Consolas"), FontSize = 14,
                Foreground = new SolidColorBrush(primaryColor),
                Tag = primaryName != null ? new HexByteTag(primaryName, group, primaryColor, overlapping) : null,
                Cursor = Cursors.Hand,
                ToolTip = tooltip
            };
            tb.MouseLeftButtonDown += OnHexByteClicked;
            panel.Children.Add(tb);
        }
    }

    private void PopulateBinaryDisplay(Panel panel, byte[] bytes, int count, List<FieldDef> fields, string group)
    {
        panel.Children.Clear();
        foreach (var f in fields)
        {
            if (f.Start / 8 >= count) break;
            bool isMsb = f.BitOrder == BitOrder.BitZeroIsMsb;

            var sb = new StringBuilder();
            int width = f.End - f.Start + 1;
            for (int i = 0; i < width; i++)
            {
                // MSB-first: iterate Start..End (bit 0 is MSB, natural left-to-right)
                // LSB-first: iterate End..Start (put MSB on the left for display)
                int bit = isMsb ? f.Start + i : f.End - i;
                if (bit / 8 >= count) break;
                int byteIdx = bit / 8;
                int shift = isMsb ? 7 - (bit % 8) : bit % 8;
                sb.Append((bytes[byteIdx] >> shift) & 1);
                if ((i + 1) % 4 == 0 && i != width - 1)
                    sb.Append(' ');
            }

            var bg = Color.FromArgb(0x25, f.Color.R, f.Color.G, f.Color.B);
            var label = new TextBlock
            {
                Text = f.Name, FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(0xBB, f.Color.R, f.Color.G, f.Color.B)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var bits = new TextBlock
            {
                Text = sb.ToString(), FontFamily = new FontFamily("Consolas"), FontSize = 13,
                Foreground = new SolidColorBrush(f.Color), HorizontalAlignment = HorizontalAlignment.Center
            };

            var stack = new StackPanel();
            stack.Children.Add(label);
            stack.Children.Add(bits);

            var border = new Border
            {
                Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 4, 4),
                Child = stack, Tag = new FieldTag(f.Name, group, f.Color), Cursor = Cursors.Hand,
                ToolTip = f.Description
            };
            border.MouseLeftButtonDown += OnFieldClicked;
            panel.Children.Add(border);
        }
    }

    private void OnFieldClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FieldTag tag })
            return;

        ToggleActiveField(tag.Group, tag.Name);
    }

    private void OnHexByteClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { Tag: HexByteTag tag })
            return;

        // If one of the overlapping fields is already active, cycle to the next one;
        // otherwise activate the primary field.
        ref string? active = ref ResolveActiveRef(tag.Group, out _);
        string? currentActive = active;
        string? nextField = null;
        if (currentActive != null)
        {
            // Find the current active in the overlapping list, then pick the next
            for (int i = 0; i < tag.OverlappingFields.Count; i++)
            {
                if (tag.OverlappingFields[i].Name == currentActive)
                {
                    int next = i + 1;
                    if (next < tag.OverlappingFields.Count)
                        nextField = tag.OverlappingFields[next].Name;
                    // else nextField stays null => deselect
                    break;
                }
            }

            // If the active field isn't in this byte's overlap list, just pick the primary
            if (nextField == null && !tag.OverlappingFields.Exists(o => o.Name == currentActive))
                nextField = tag.PrimaryName;
        }
        else
        {
            nextField = tag.PrimaryName;
        }

        ToggleActiveField(tag.Group, nextField, forceValue: true);
    }

    private void ToggleActiveField(string group, string? fieldName, bool forceValue = false)
    {
        ref string? active = ref ResolveActiveRef(group, out var panels);

        if (forceValue)
            active = fieldName;
        else
            active = active == fieldName ? null : fieldName;

        foreach (var p in panels)
            ApplyHighlighting(p, active);
    }

    private ref string? ResolveActiveRef(string group, out Panel[] panels)
    {
        if (group == nameof(_activePeField))
        {
            panels = [PeFieldSummaryPanel, PeHexBytesPanel, PeBinaryBitsPanel];
            return ref _activePeField;
        }
        if (group == nameof(_activeCpuField))
        {
            panels = [CpuFieldSummaryPanel, CpuHexBytesPanel, CpuBinaryBitsPanel];
            return ref _activeCpuField;
        }
        if (group == nameof(_activeFpField))
        {
            panels = [FpFieldSummaryPanel, FpHexBytesPanel, FpBinaryBitsPanel];
            return ref _activeFpField;
        }
        panels = [FieldSummaryPanel, HexBytesPanel, BinaryBitsPanel];
        return ref _activePacketField;
    }

    private static void ApplyHighlighting(Panel panel, string? activeField)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is not FrameworkElement fe) continue;

            if (fe is Border border && fe.Tag is FieldTag borderTag)
            {
                bool isSelected = activeField != null && borderTag.Name == activeField;
                if (isSelected)
                {
                    border.Background = new SolidColorBrush(borderTag.Color);
                    border.BorderBrush = SystemColors.HighlightBrush;
                    border.BorderThickness = new Thickness(2);
                    SetDescendantForeground(border.Child, SystemColors.HighlightTextColor);
                }
                else
                {
                    RestoreFieldColors(border);
                }
            }
            else if (fe is TextBlock tb && fe.Tag is HexByteTag hexTag)
            {
                // Check if the active field overlaps this byte
                var match = activeField != null
                    ? hexTag.OverlappingFields.Find(o => o.Name == activeField)
                    : default;

                if (match.Name != null)
                {
                    tb.Foreground = new SolidColorBrush(match.Color);
                    tb.FontWeight = FontWeights.ExtraBold;
                    tb.TextDecorations = TextDecorations.Underline;
                }
                else
                {
                    tb.Foreground = new SolidColorBrush(hexTag.PrimaryColor);
                    tb.FontWeight = FontWeights.Normal;
                    tb.TextDecorations = null;
                }
            }
            else if (fe is Border binaryBorder && fe.Tag is FieldTag binaryTag)
            {
                // handled above
            }
        }
    }

    private static void SetDescendantForeground(object? element, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (element is Panel p)
        {
            foreach (UIElement child in p.Children)
            {
                if (child is TextBlock tb) tb.Foreground = brush;
            }
        }
        else if (element is TextBlock tb)
        {
            tb.Foreground = brush;
        }
    }

    private static void RestoreFieldColors(Border border)
    {
        if (border.Tag is not FieldTag tag) return;
        var c = tag.Color;

        border.Background = new SolidColorBrush(Color.FromArgb(0x30, c.R, c.G, c.B));
        border.BorderBrush = new SolidColorBrush(c);
        border.BorderThickness = new Thickness(1);

        if (border.Child is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is TextBlock tb)
                {
                    if (tb.FontWeight == FontWeights.Bold)
                    {
                        // Name label: restore to field color
                        tb.Foreground = new SolidColorBrush(c);
                    }
                    else
                    {
                        // Value text: clear local foreground to re-inherit theme color
                        tb.ClearValue(TextBlock.ForegroundProperty);
                    }
                }
            }
        }
    }

    // ?? Value Formatting ???????????????????????????????????????

    private static string FormatViewField<T>(T view, BitFieldInfo fieldInfo) where T : struct
    {
        var prop = typeof(T).GetProperty(fieldInfo.Name);
        if (prop == null) return "?";
        var val = prop.GetValue(view);
        if (val == null) return "?";

        if (fieldInfo.IsFlag) return (bool)val ? "1" : "0";

        return fieldInfo.PropertyType switch
        {
            "byte" => $"0x{val:X2}",
            "ushort" => $"0x{(ushort)val:X4}",
            "uint" => $"0x{(uint)val:X8}",
            "ulong" => $"0x{(ulong)val:X16}",
            _ => val.ToString() ?? "?"
        };
    }
}
