using System;
using System.Linq;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Tests;

// ── Test structs with special characters in Description ─────

/// <summary>
/// Struct with Description strings containing characters that must be escaped
/// in C# string literals. If the generator doesn't escape these properly,
/// the generated code won't compile.
/// </summary>
[BitFields(typeof(byte))]
public partial struct DescEscapeRegister
{
    [BitFlag(0, Description = "Line1\nLine2")] public partial bool Newline { get; set; }
    [BitFlag(1, Description = "Col1\tCol2")] public partial bool Tab { get; set; }
    [BitFlag(2, Description = "Say \"hello\"")] public partial bool Quotes { get; set; }
    [BitFlag(3, Description = "C:\\Users\\test")] public partial bool Backslash { get; set; }
    [BitFlag(4, Description = "CR\rhere")] public partial bool CarriageReturn { get; set; }
    [BitFlag(5, Description = "Null\0char")] public partial bool NullChar { get; set; }
    [BitFlag(6, Description = "Alert\abell\bback\fform\vvert")] public partial bool ControlChars { get; set; }
    [BitField(7, 7, Description = "Mixed: \"\\\n\t\r\0")] public partial byte AllAtOnce { get; set; }
}

/// <summary>
/// View struct with the same special-character Description values.
/// Verifies the BitFieldsViewGenerator escapes identically.
/// </summary>
[BitFieldsView]
public partial record struct DescEscapeView
{
    [BitFlag(0, Description = "Line1\nLine2")] public partial bool Newline { get; set; }
    [BitFlag(1, Description = "Col1\tCol2")] public partial bool Tab { get; set; }
    [BitFlag(2, Description = "Say \"hello\"")] public partial bool Quotes { get; set; }
    [BitFlag(3, Description = "C:\\Users\\test")] public partial bool Backslash { get; set; }
    [BitField(4, 7, Description = "Mixed: \"\\\n\t\r\0")] public partial byte AllAtOnce { get; set; }
}

/// <summary>
/// Struct with Unicode and emoji in Description to verify they pass through safely.
/// </summary>
[BitFields(typeof(ushort))]
public partial struct DescUnicodeRegister
{
    [BitFlag(0, Description = "Flag ✓ enabled")] public partial bool Check { get; set; }
    [BitFlag(1, Description = "Emoji 🚀 launch")] public partial bool Rocket { get; set; }
    [BitField(2, 7, Description = "日本語テスト")] public partial byte Japanese { get; set; }
    [BitField(8, 15, Description = "Ünïcödé àccénts")] public partial byte Accented { get; set; }
}

public class DescriptionEscapingTests
{
    // ── Compile-time verification ───────────────────────────────
    // If these tests can be *compiled*, the generator's escaping is correct.
    // The asserts verify the round-tripped Description values match the originals.

    [Fact]
    public void BitFields_Description_NewlineRoundTrips()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "Newline");
        Assert.Equal("Line1\nLine2", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_TabRoundTrips()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "Tab");
        Assert.Equal("Col1\tCol2", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_QuotesRoundTrip()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "Quotes");
        Assert.Equal("Say \"hello\"", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_BackslashRoundTrips()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "Backslash");
        Assert.Equal("C:\\Users\\test", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_CarriageReturnRoundTrips()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "CarriageReturn");
        Assert.Equal("CR\rhere", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_NullCharRoundTrips()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "NullChar");
        Assert.Equal("Null\0char", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_ControlCharsRoundTrip()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "ControlChars");
        Assert.Equal("Alert\abell\bback\fform\vvert", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_AllSpecialCharsAtOnce()
    {
        var field = DescEscapeRegister.Fields.ToArray().First(f => f.Name == "AllAtOnce");
        Assert.Equal("Mixed: \"\\\n\t\r\0", field.GetDescription());
    }

    // ── BitFieldsView same tests ────────────────────────────────

    [Fact]
    public void BitFieldsView_Description_NewlineRoundTrips()
    {
        var field = DescEscapeView.Fields.ToArray().First(f => f.Name == "Newline");
        Assert.Equal("Line1\nLine2", field.GetDescription());
    }

    [Fact]
    public void BitFieldsView_Description_QuotesRoundTrip()
    {
        var field = DescEscapeView.Fields.ToArray().First(f => f.Name == "Quotes");
        Assert.Equal("Say \"hello\"", field.GetDescription());
    }

    [Fact]
    public void BitFieldsView_Description_BackslashRoundTrips()
    {
        var field = DescEscapeView.Fields.ToArray().First(f => f.Name == "Backslash");
        Assert.Equal("C:\\Users\\test", field.GetDescription());
    }

    [Fact]
    public void BitFieldsView_Description_AllSpecialCharsAtOnce()
    {
        var field = DescEscapeView.Fields.ToArray().First(f => f.Name == "AllAtOnce");
        Assert.Equal("Mixed: \"\\\n\t\r\0", field.GetDescription());
    }

    // ── Unicode ─────────────────────────────────────────────────

    [Fact]
    public void BitFields_Description_UnicodeCheckmark()
    {
        var field = DescUnicodeRegister.Fields.ToArray().First(f => f.Name == "Check");
        Assert.Equal("Flag ✓ enabled", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_Emoji()
    {
        var field = DescUnicodeRegister.Fields.ToArray().First(f => f.Name == "Rocket");
        Assert.Equal("Emoji 🚀 launch", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_Japanese()
    {
        var field = DescUnicodeRegister.Fields.ToArray().First(f => f.Name == "Japanese");
        Assert.Equal("日本語テスト", field.GetDescription());
    }

    [Fact]
    public void BitFields_Description_Accented()
    {
        var field = DescUnicodeRegister.Fields.ToArray().First(f => f.Name == "Accented");
        Assert.Equal("Ünïcödé àccénts", field.GetDescription());
    }
}
