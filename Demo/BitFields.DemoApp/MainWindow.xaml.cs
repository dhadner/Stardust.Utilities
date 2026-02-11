using System;
using System.IO;
using System.Text;
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
        Rgb(0x61,0xAF,0xEF), Rgb(0xC6,0x78,0xDD), Rgb(0x98,0xC3,0x79), Rgb(0xE5,0xC0,0x7B),
        Rgb(0xE0,0x6C,0x75), Rgb(0x56,0xB6,0xC2), Rgb(0xD1,0x9A,0x66), Rgb(0xBE,0x50,0x46),
        Rgb(0x7E,0xC8,0xE3), Rgb(0xFF,0x79,0xC6), Rgb(0x50,0xFA,0x7B), Rgb(0xF1,0xFA,0x8C),
        Rgb(0xBD,0x93,0xF9), Rgb(0xFF,0xB8,0x6C), Rgb(0x8B,0xE9,0xFD), Rgb(0xFF,0x55,0x55),
        Rgb(0xA9,0xDC,0x76), Rgb(0xCC,0x99,0xCD), Rgb(0x6A,0x99,0x55), Rgb(0x9E,0xA0,0xA6),
    ];

    public MainWindow()
    {
        InitializeComponent();
        SeedPacketSample();
        OnParsePacket(this, new RoutedEventArgs());
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
        PeRawBytes.Text = HexUtils.ToHex(bytes, 512);

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
        if (bytes.Length < peOffset + 4 + CoffHeaderView.SizeInBytes)
        {
            AddInfoCard(PeFieldSummaryPanel, "Error", "Missing PE header", Colors.Red);
            return;
        }

        var signature = BitConverter.ToUInt32(bytes, peOffset);

        // Build field cards from generated metadata
        var allFields = new List<FieldDef>();
        int ci = 0;

        foreach (var m in DosHeaderView.Fields)
            allFields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], FormatViewField(dos, m), m.BitOrder));

        allFields.Add(new FieldDef("PE Sig", 0, 31, Palette[ci++ % Palette.Length],
            signature == PeHeader.Signature ? "PE\\0\\0" : $"0x{signature:X8}"));

        int coffColorStart = ci;
        foreach (var m in CoffHeaderView.Fields)
        {
            var coff = new CoffHeaderView(bytes, peOffset + 4);
            allFields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], FormatViewField(coff, m), m.BitOrder));
        }

        _activePeField = null;
        PopulateFieldSummary(PeFieldSummaryPanel, allFields, nameof(_activePeField));

        // COFF header hex/binary
        int coffByteOffset = peOffset + 4;
        int coffLen = Math.Min(CoffHeaderView.SizeInBytes, bytes.Length - coffByteOffset);
        var coffBytes = bytes.AsSpan(coffByteOffset, coffLen).ToArray();

        var coffFields = new List<FieldDef>();
        ci = coffColorStart;
        foreach (var m in CoffHeaderView.Fields)
        {
            var coff = new CoffHeaderView(bytes, peOffset + 4);
            coffFields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], FormatViewField(coff, m), m.BitOrder));
        }

        PopulateHexDisplay(PeHexBytesPanel, coffBytes, coffLen, coffFields, nameof(_activePeField));
        PopulateBinaryDisplay(PeBinaryBitsPanel, coffBytes, coffLen, coffFields, nameof(_activePeField));
    }

    // ?? Network Packet Viewer ??????????????????????????????????

    private void OnParsePacket(object sender, RoutedEventArgs e)
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
            fields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], val, m.BitOrder));
        }

        foreach (var m in TcpHeaderView.Fields)
        {
            string val = FormatViewField(tcp, m);
            fields.Add(new FieldDef(m.Name, m.StartBit + tcpBitBase, m.EndBit + tcpBitBase, Palette[ci++ % Palette.Length], val, m.BitOrder));
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

    private void OnApplyCpuRaw(object sender, RoutedEventArgs e)
    {
        if (!HexUtils.TryParseUShort(CpuRawHex.Text, out var value))
            return;

        _statusRegister = value;
        UpdateCpuUi();
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

        UpdateCpuUi();
    }

    private void OnCpuModeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressCpuUpdate)
            return;

        _statusRegister.Mode = (byte)e.NewValue;
        UpdateCpuUi();
    }

    private void UpdateCpuUi()
    {
        _suppressCpuUpdate = true;
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

        BuildCpuFieldDisplay();
    }

    private void BuildCpuFieldDisplay()
    {
        var fields = new List<FieldDef>();
        int ci = 0;

        foreach (var m in CpuStatusRegister.Fields)
        {
            string val = FormatViewField(_statusRegister, m);
            fields.Add(new FieldDef(m.Name, m.StartBit, m.EndBit, Palette[ci++ % Palette.Length], val, m.BitOrder));
        }

        var bytes = BitConverter.GetBytes((ushort)_statusRegister);

        _activeCpuField = null;
        PopulateFieldSummary(CpuFieldSummaryPanel, fields, nameof(_activeCpuField));
        PopulateHexDisplay(CpuHexBytesPanel, bytes, bytes.Length, fields, nameof(_activeCpuField));
        PopulateBinaryDisplay(CpuBinaryBitsPanel, bytes, bytes.Length, fields, nameof(_activeCpuField));
    }

    // ?? Shared Three-Panel Display ?????????????????????????????

    private sealed record FieldDef(string Name, int StartBit, int EndBit, Color Color, string Value, BitOrder BitOrder = BitOrder.BitZeroIsLsb);
    private sealed record FieldTag(string Name, string Group, Color Color);

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
            var valueBlock = new TextBlock { Text = f.Value, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 13 };

            var stack = new StackPanel();
            stack.Children.Add(nameBlock);
            stack.Children.Add(valueBlock);

            var border = new Border
            {
                Background = bg, BorderBrush = brush, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 6), Child = stack,
                Tag = new FieldTag(f.Name, group, f.Color), Cursor = Cursors.Hand
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
            var field = fields.Find(f => f.StartBit <= bitStart && f.EndBit >= bitStart);
            var color = field?.Color ?? Colors.Gray;

            var tb = new TextBlock
            {
                Text = $"{bytes[i]:X2} ", FontFamily = new FontFamily("Consolas"), FontSize = 14,
                Foreground = new SolidColorBrush(color),
                Tag = field != null ? new FieldTag(field.Name, group, field.Color) : null,
                Cursor = Cursors.Hand
            };
            tb.MouseLeftButtonDown += OnFieldClicked;
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
                Child = stack, Tag = new FieldTag(f.Name, group, f.Color), Cursor = Cursors.Hand
            };
            border.MouseLeftButtonDown += OnFieldClicked;
            panel.Children.Add(border);
        }
    }

    private void OnFieldClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FieldTag tag })
            return;

        ref string? active = ref _activePacketField;
        Panel[] panels;
        if (tag.Group == nameof(_activePeField))
        {
            active = ref _activePeField;
            panels = [PeFieldSummaryPanel, PeHexBytesPanel, PeBinaryBitsPanel];
        }
        else if (tag.Group == nameof(_activeCpuField))
        {
            active = ref _activeCpuField;
            panels = [CpuFieldSummaryPanel, CpuHexBytesPanel, CpuBinaryBitsPanel];
        }
        else
        {
            panels = [FieldSummaryPanel, HexBytesPanel, BinaryBitsPanel];
        }

        active = active == tag.Name ? null : tag.Name;
        foreach (var p in panels)
            ApplyHighlighting(p, active);
    }

    private static void ApplyHighlighting(Panel panel, string? activeField)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is not FrameworkElement fe) continue;
            var tag = fe.Tag as FieldTag;
            bool isSelected = activeField != null && tag?.Name == activeField;

            if (fe is Border border && tag != null)
            {
                if (isSelected)
                {
                    border.Background = new SolidColorBrush(tag.Color);
                    border.BorderBrush = Brushes.White;
                    border.BorderThickness = new Thickness(2);
                    SetDescendantForeground(border.Child, Colors.Black);
                }
                else
                {
                    RestoreFieldColors(border);
                }
            }
            else if (fe is TextBlock tb)
            {
                if (isSelected)
                {
                    tb.FontWeight = FontWeights.ExtraBold;
                    tb.TextDecorations = TextDecorations.Underline;
                }
                else
                {
                    tb.FontWeight = FontWeights.Normal;
                    tb.TextDecorations = null;
                }
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
                        tb.Foreground = new SolidColorBrush(c);
                    else if (tb.FontSize <= 10)
                        tb.Foreground = new SolidColorBrush(Color.FromArgb(0xBB, c.R, c.G, c.B));
                    else
                        tb.Foreground = new SolidColorBrush(c);
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
