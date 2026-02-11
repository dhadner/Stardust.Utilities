using System;
using System.IO;
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

        if (bytes.Length < DosHeaderView.SizeInBytes)
        {
            AddInfoCard(PeFieldSummaryPanel, "Error", "File too small", Colors.Red);
            return;
        }

        var dos = new DosHeaderView(bytes);
        int peOffset = (int)dos.Lfanew;
        int coffByteOffset = peOffset + 4;

        if (bytes.Length < coffByteOffset + CoffHeaderView.SizeInBytes)
        {
            AddInfoCard(PeFieldSummaryPanel, "Error", "Missing PE header", Colors.Red);
            return;
        }

        var signature = BitConverter.ToUInt32(bytes, peOffset);
        var coff = new CoffHeaderView(bytes, coffByteOffset);
        int optByteOffset = coffByteOffset + CoffHeaderView.SizeInBytes;
        int optHeaderSize = Math.Min((int)coff.SizeOfOptionalHeader, OptionalHeaderView.SizeInBytes);
        bool hasOptional = coff.SizeOfOptionalHeader > 0 && bytes.Length >= optByteOffset + optHeaderSize;

        // Determine total display byte count
        int totalDisplayBytes = hasOptional
            ? Math.Min(optByteOffset + optHeaderSize, bytes.Length)
            : Math.Min(coffByteOffset + CoffHeaderView.SizeInBytes, bytes.Length);

        // Build field list with GLOBAL bit positions (byte offset * 8)
        var allFields = new List<FieldDef>();
        int ci = 0;

        // DOS header fields: view starts at byte 0, so bit positions are already global
        foreach (var m in DosHeaderView.Fields)
            allFields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], FormatViewField(dos, m), m.BitOrder, m.GetDescription()));

        // PE signature: 4 bytes at peOffset
        int sigBitBase = peOffset * 8;
        allFields.Add(new FieldDef("PE Sig", sigBitBase, sigBitBase + 31, Palette[ci++ % Palette.Length],
            signature == PeHeader.Signature ? "PE\\0\\0" : $"0x{signature:X8}", Description: "PE signature magic bytes ('PE\\0\\0' = 0x00004550)"));

        // COFF header fields
        int coffBitBase = coffByteOffset * 8;
        foreach (var m in CoffHeaderView.Fields)
            allFields.Add(new FieldDef(m.Name, m.StartBit + coffBitBase, m.EndBit + coffBitBase, Palette[ci++ % Palette.Length], FormatViewField(coff, m), m.BitOrder, m.GetDescription()));

        // Optional header fields (if present)
        if (hasOptional)
        {
            int optBitBase = optByteOffset * 8;
            var opt = new OptionalHeaderView(bytes, optByteOffset);
            foreach (var m in OptionalHeaderView.Fields)
            {
                int globalStart = m.StartBit + optBitBase;
                int globalEnd = m.EndBit + optBitBase;
                if (globalEnd / 8 < totalDisplayBytes)
                    allFields.Add(new FieldDef(m.Name, globalStart, globalEnd, Palette[ci++ % Palette.Length], FormatViewField(opt, m), m.BitOrder, m.GetDescription()));
            }
        }

        _activePeField = null;
        PopulateFieldSummary(PeFieldSummaryPanel, allFields, nameof(_activePeField));
        PopulateHexDisplay(PeHexBytesPanel, bytes, totalDisplayBytes, allFields, nameof(_activePeField));
        PopulateBinaryDisplay(PeBinaryBitsPanel, bytes, totalDisplayBytes, allFields, nameof(_activePeField));
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

        if (bytes.Length < IPv4HeaderView.SizeInBytes)
        {
            HttpPayload.Text = "Packet too small.";
            return;
        }

        var ip = new IPv4HeaderView(bytes);
        int tcpOffset = ip.HeaderLengthBytes;

        if (bytes.Length < tcpOffset + TcpHeaderView.SizeInBytes)
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
            fields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
        }

        foreach (var m in TcpHeaderView.Fields)
        {
            string val = FormatViewField(tcp, m);
            fields.Add(new FieldDef(m.Name, m.StartBit + tcpBitBase, m.EndBit + tcpBitBase, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
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
            fields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], val, m.BitOrder, m.GetDescription()));
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

    // ?? RFC Diagram ??????????????????????????????????????????????

    private readonly record struct DiagramSource(string Label, BitFieldInfo[] Fields);

    private DiagramSource[] _diagramSources = [];

    private void InitRfcTab()
    {
        _diagramSources =
        [
            new("IPv4 Header", IPv4HeaderView.Fields.ToArray()),
            new("TCP Header", TcpHeaderView.Fields.ToArray()),
            new("DOS Header", DosHeaderView.Fields.ToArray()),
            new("COFF Header", CoffHeaderView.Fields.ToArray()),
            new("Optional Header", OptionalHeaderView.Fields.ToArray()),
            new("CPU Status Register", CpuStatusRegister.Fields.ToArray()),
        ];

        RfcStructPicker.Items.Clear();
        foreach (var s in _diagramSources)
            RfcStructPicker.Items.Add(s.Label);
        RfcStructPicker.SelectedIndex = 0;

        RfcBitsPerRow.Items.Clear();
        RfcBitsPerRow.Items.Add("8");
        RfcBitsPerRow.Items.Add("16");
        RfcBitsPerRow.Items.Add("32");
        RfcBitsPerRow.Items.Add("64");
        RfcBitsPerRow.SelectedIndex = 2; // default 32
    }

    private void OnRfcStructChanged(object sender, RoutedEventArgs e) => UpdateRfcDiagram();
    private void OnRfcStructChanged(object sender, SelectionChangedEventArgs e) => UpdateRfcDiagram();

    private void UpdateRfcDiagram()
    {
        if (RfcStructPicker.SelectedIndex < 0 || RfcBitsPerRow.SelectedItem == null)
            return;

        var source = _diagramSources[RfcStructPicker.SelectedIndex];
        int bitsPerRow = int.Parse((string)RfcBitsPerRow.SelectedItem);
        bool showDesc = RfcShowDescriptions.IsChecked == true;

        RfcDiagramOutput.Text = BitFieldDiagram.RenderToString(source.Fields, bitsPerRow, showDesc);
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

    // ?? Shared Three-Panel Display ?????????????????????????????

    private sealed record FieldDef(string Name, int StartBit, int EndBit, Color Color, string Value, BitOrder BitOrder = BitOrder.BitZeroIsLsb, string? Description = null);
    private sealed record FieldTag(string Name, string Group, Color Color);
    private sealed record HexByteTag(string PrimaryName, string Group, Color PrimaryColor, List<(string Name, Color Color)> OverlappingFields);

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
                if (f.StartBit <= bitEnd && f.EndBit >= bitStart)
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
            if (f.StartBit / 8 >= count) break;
            bool isMsb = f.BitOrder == BitOrder.BitZeroIsMsb;

            var sb = new StringBuilder();
            int width = f.EndBit - f.StartBit + 1;
            for (int i = 0; i < width; i++)
            {
                // MSB-first: iterate StartBit..EndBit (bit 0 is MSB, natural left-to-right)
                // LSB-first: iterate EndBit..StartBit (put MSB on the left for display)
                int bit = isMsb ? f.StartBit + i : f.EndBit - i;
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
