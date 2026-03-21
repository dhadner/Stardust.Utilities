using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stardust.Generators;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests that the BitFields generators emit correct diagnostics for the named
/// BitField syntax (SD0015–SD0019): deprecated positional End, redundant
/// End+Width, inconsistent End+Width, missing End/Width, missing Start.
/// </summary>
public class BitFieldNamedSyntaxDiagnosticTests
{
    #region SD0015 - Deprecated two-parameter constructor

    /// <summary>
    /// The deprecated two-parameter constructor [BitField(start, end)] should produce warning SD0015.
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct DepReg
            {
                [BitField(0, 3)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Where(d => d.Id == "SD0015").ToList();
        sd0015.Should().HaveCount(1);
        sd0015[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        sd0015[0].GetMessage().Should().Contain("Nibble");
        sd0015[0].GetMessage().Should().Contain("0");
        sd0015[0].GetMessage().Should().Contain("3");
    }

    /// <summary>
    /// Multiple deprecated two-parameter fields should each produce SD0015.
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_MultipleProduce_MultipleWarnings()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct MultiReg
            {
                [BitField(0, 7)] public partial byte Low { get; set; }
                [BitField(8, 15)] public partial byte High { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Where(d => d.Id == "SD0015").ToList();
        sd0015.Should().HaveCount(2);
        sd0015.Should().Contain(d => d.GetMessage().Contains("Low"));
        sd0015.Should().Contain(d => d.GetMessage().Contains("High"));
    }

    /// <summary>
    /// SD0015 message should suggest both End and Width alternatives with correct values.
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_MessageSuggestsBothAlternatives()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct SugReg
            {
                [BitField(2, 4)] public partial byte Mode { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Single(d => d.Id == "SD0015");
        sd0015.GetMessage().Should().Contain("End = 4");
        sd0015.GetMessage().Should().Contain("Width = 3");
    }

    /// <summary>
    /// SD0015 should fire for BitFieldsView as well (not just BitFields).
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_BitFieldsView_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct DepView
            {
                [BitField(0, 7)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Where(d => d.Id == "SD0015").ToList();
        sd0015.Should().HaveCount(1);
        sd0015[0].GetMessage().Should().Contain("Status");
    }

    /// <summary>
    /// SD0015 should have a source location pointing at the property.
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_HasSourceLocation()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct LocReg
            {
                [BitField(0, 3)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Single(d => d.Id == "SD0015");
        sd0015.Location.Should().NotBe(Location.None);
    }

    #endregion

    #region SD0016 - Redundant End and Width

    /// <summary>
    /// Specifying both End and Width that agree should produce warning SD0016.
    /// </summary>
    [Fact]
    public void RedundantEndBitAndWidth_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct RedReg
            {
                [BitField(0, End = 3, Width = 4)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0016 = diagnostics.Where(d => d.Id == "SD0016").ToList();
        sd0016.Should().HaveCount(1);
        sd0016[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        sd0016[0].GetMessage().Should().Contain("Nibble");
        sd0016[0].GetMessage().Should().Contain("3");
        sd0016[0].GetMessage().Should().Contain("4");
    }

    /// <summary>
    /// SD0016 should fire when both End and Width are given via fully named syntax.
    /// </summary>
    [Fact]
    public void RedundantEndBitAndWidth_FullyNamed_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct FullReg
            {
                [BitField(Start = 4, End = 11, Width = 8)] public partial byte Mid { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0016 = diagnostics.Where(d => d.Id == "SD0016").ToList();
        sd0016.Should().HaveCount(1);
        sd0016[0].GetMessage().Should().Contain("Mid");
    }

    /// <summary>
    /// Redundant End and Width on the deprecated constructor should still produce SD0016 (plus SD0015).
    /// </summary>
    [Fact]
    public void RedundantEndBitAndWidth_OnDeprecatedCtor_ProducesBothWarnings()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct BothReg
            {
                [BitField(0, 3, Width = 4)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0015").Should().HaveCount(1);
        diagnostics.Where(d => d.Id == "SD0016").Should().HaveCount(1);
    }

    /// <summary>
    /// SD0016 for BitFieldsView: redundant End+Width should also warn.
    /// </summary>
    [Fact]
    public void RedundantEndBitAndWidth_BitFieldsView_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct RedView
            {
                [BitField(0, End = 7, Width = 8)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0016").Should().HaveCount(1);
    }

    #endregion

    #region SD0017 - Inconsistent End and Width

    /// <summary>
    /// Specifying both End and Width that disagree should produce error SD0017.
    /// </summary>
    [Fact]
    public void InconsistentEndBitAndWidth_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct BadReg
            {
                [BitField(0, End = 3, Width = 8)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0017 = diagnostics.Where(d => d.Id == "SD0017").ToList();
        sd0017.Should().HaveCount(1);
        sd0017[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0017[0].GetMessage().Should().Contain("Nibble");
        sd0017[0].GetMessage().Should().Contain("3");
        sd0017[0].GetMessage().Should().Contain("8");
    }

    /// <summary>
    /// Inconsistent End+Width via fully named syntax should also produce SD0017.
    /// </summary>
    [Fact]
    public void InconsistentEndBitAndWidth_FullyNamed_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct IncReg
            {
                [BitField(Start = 4, End = 7, Width = 2)] public partial byte Field { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0017 = diagnostics.Where(d => d.Id == "SD0017").ToList();
        sd0017.Should().HaveCount(1);
        sd0017[0].GetMessage().Should().Contain("Field");
    }

    /// <summary>
    /// Inconsistent Width on the deprecated constructor should produce SD0017 (plus SD0015).
    /// </summary>
    [Fact]
    public void InconsistentEndBitAndWidth_OnDeprecatedCtor_ProducesErrorAndWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct DepIncReg
            {
                [BitField(0, 3, Width = 2)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0015").Should().HaveCount(1);
        diagnostics.Where(d => d.Id == "SD0017").Should().HaveCount(1);
    }

    /// <summary>
    /// SD0017 for BitFieldsView: inconsistent End+Width should produce error.
    /// </summary>
    [Fact]
    public void InconsistentEndBitAndWidth_BitFieldsView_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct IncView
            {
                [BitField(0, End = 7, Width = 4)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0017").Should().HaveCount(1);
    }

    #endregion

    #region SD0018 - Missing End or Width

    /// <summary>
    /// Single-parameter constructor without End or Width should produce error SD0018.
    /// </summary>
    [Fact]
    public void MissingEndBitOrWidth_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct NoRangeReg
            {
                [BitField(0)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0018 = diagnostics.Where(d => d.Id == "SD0018").ToList();
        sd0018.Should().HaveCount(1);
        sd0018[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0018[0].GetMessage().Should().Contain("Nibble");
        sd0018[0].GetMessage().Should().Contain("NoRangeReg");
        sd0018[0].GetMessage().Should().Contain("0");
    }

    /// <summary>
    /// SD0018 message should suggest both End and Width syntax.
    /// </summary>
    [Fact]
    public void MissingEndBitOrWidth_MessageSuggestsSyntax()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct SugReg
            {
                [BitField(8)] public partial byte Field { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0018 = diagnostics.Single(d => d.Id == "SD0018");
        sd0018.GetMessage().Should().Contain("End");
        sd0018.GetMessage().Should().Contain("Width");
    }

    /// <summary>
    /// SD0018 for BitFieldsView: missing range info should produce error.
    /// </summary>
    [Fact]
    public void MissingEndBitOrWidth_BitFieldsView_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct NoRangeView
            {
                [BitField(0)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0018").Should().HaveCount(1);
    }

    /// <summary>
    /// SD0018 should have a source location pointing at the property.
    /// </summary>
    [Fact]
    public void MissingEndBitOrWidth_HasSourceLocation()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct LocReg
            {
                [BitField(0)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0018 = diagnostics.Single(d => d.Id == "SD0018");
        sd0018.Location.Should().NotBe(Location.None);
    }

    #endregion

    #region SD0019 - Missing Start

    /// <summary>
    /// Parameterless constructor without Start should produce error SD0019.
    /// </summary>
    [Fact]
    public void MissingStartBit_Parameterless_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct NoStartReg
            {
                [BitField(Width = 4)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0019 = diagnostics.Where(d => d.Id == "SD0019").ToList();
        sd0019.Should().HaveCount(1);
        sd0019[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0019[0].GetMessage().Should().Contain("Nibble");
        sd0019[0].GetMessage().Should().Contain("NoStartReg");
    }

    /// <summary>
    /// Parameterless constructor with only End (no Start) should produce SD0019.
    /// </summary>
    [Fact]
    public void MissingStartBit_OnlyEndBit_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct EndOnlyReg
            {
                [BitField(End = 7)] public partial byte Full { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0019").Should().HaveCount(1);
    }

    /// <summary>
    /// Parameterless constructor with both End and Width but no Start should produce SD0019.
    /// </summary>
    [Fact]
    public void MissingStartBit_EndBitAndWidth_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct BothNoStartReg
            {
                [BitField(End = 3, Width = 4)] public partial byte Nibble { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0019").Should().HaveCount(1);
    }

    /// <summary>
    /// SD0019 for BitFieldsView: missing Start should produce error.
    /// </summary>
    [Fact]
    public void MissingStartBit_BitFieldsView_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct NoStartView
            {
                [BitField(Width = 8)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0019").Should().HaveCount(1);
    }

    #endregion

    #region Valid named syntax - No diagnostics

    /// <summary>
    /// [BitField(start, Width = N)] should NOT produce any SD0015–SD0019 diagnostics.
    /// </summary>
    [Fact]
    public void ValidWidthSyntax_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct GoodReg
            {
                [BitField(0, Width = 4)] public partial byte Nibble { get; set; }
                [BitField(4, Width = 4)] public partial byte Upper { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// [BitField(start, End = N)] should NOT produce any SD0015–SD0019 diagnostics.
    /// </summary>
    [Fact]
    public void ValidEndBitSyntax_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct GoodReg2
            {
                [BitField(0, End = 3)] public partial byte Nibble { get; set; }
                [BitField(4, End = 7)] public partial byte Upper { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// [BitField(Start = N, Width = M)] fully named syntax should NOT produce any diagnostics.
    /// </summary>
    [Fact]
    public void ValidFullyNamedWidthSyntax_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct FullGoodReg
            {
                [BitField(Start = 0, Width = 8)] public partial byte Low { get; set; }
                [BitField(Start = 8, Width = 8)] public partial byte High { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// [BitField(Start = N, End = M)] fully named syntax should NOT produce any diagnostics.
    /// </summary>
    [Fact]
    public void ValidFullyNamedEndBitSyntax_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct FullGoodReg2
            {
                [BitField(Start = 0, End = 7)] public partial byte Low { get; set; }
                [BitField(Start = 8, End = 15)] public partial byte High { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// Valid named syntax on [BitFieldsView] should NOT produce any SD0015–SD0019 diagnostics.
    /// </summary>
    [Fact]
    public void ValidNamedSyntax_BitFieldsView_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFieldsView]
            public partial record struct GoodView
            {
                [BitField(0, Width = 8)] public partial byte Status { get; set; }
                [BitField(8, End = 15)] public partial byte Code { get; set; }
            }
            """;

        var diagnostics = RunViewGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// A single-bit field using Width = 1 should NOT produce any diagnostics.
    /// </summary>
    [Fact]
    public void ValidSingleBitWidth_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct SingleBitReg
            {
                [BitField(0, Width = 1)] public partial byte Bit0 { get; set; }
                [BitField(7, End = 7)] public partial byte Bit7 { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    #endregion

    #region Mixed scenarios

    /// <summary>
    /// A struct with a mix of valid new syntax and deprecated old syntax should
    /// only warn on the deprecated fields.
    /// </summary>
    [Fact]
    public void MixedSyntax_OnlyDeprecatedFieldsWarn()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ushort))]
            public partial struct MixedReg
            {
                [BitField(0, Width = 4)] public partial byte Good { get; set; }
                [BitField(4, 7)] public partial byte Bad { get; set; }
                [BitField(8, End = 15)] public partial byte AlsoGood { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        var sd0015 = diagnostics.Where(d => d.Id == "SD0015").ToList();
        sd0015.Should().HaveCount(1);
        sd0015[0].GetMessage().Should().Contain("Bad");
        sd0015.Should().NotContain(d => d.GetMessage().Contains("Good"));
        sd0015.Should().NotContain(d => d.GetMessage().Contains("AlsoGood"));
    }

    /// <summary>
    /// Deprecated constructor with MustBe parameter should still produce SD0015.
    /// </summary>
    [Fact]
    public void DeprecatedTwoParam_WithMustBe_ProducesWarning()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct MustBeReg
            {
                [BitField(0, 3, MustBe.Zero)] public partial byte Reserved { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0015").Should().HaveCount(1);
    }

    /// <summary>
    /// Named syntax with MustBe should NOT produce deprecation warning.
    /// </summary>
    [Fact]
    public void NamedSyntax_WithMustBe_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct MustBeGoodReg
            {
                [BitField(0, MustBe.Zero, Width = 4)] public partial byte Reserved { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// Error diagnostics (SD0017, SD0018, SD0019) should cause the field to be skipped
    /// (no generated code for it), while warning diagnostics (SD0015, SD0016) still
    /// generate valid code.
    /// </summary>
    [Fact]
    public void ErrorDiagnostics_DoNotPreventOtherFields()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct PartialReg
            {
                [BitField(0, Width = 4)] public partial byte Good { get; set; }
                [BitField(4)] public partial byte Bad { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        // Bad field gets SD0018
        diagnostics.Where(d => d.Id == "SD0018").Should().HaveCount(1);

        // Good field should not produce any named syntax diagnostics
        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().NotContain(d => d.GetMessage().Contains("Good"));
    }

    #endregion

    #region Edge cases

    /// <summary>
    /// Width = 1 on a single bit field (start == end) should work cleanly.
    /// </summary>
    [Fact]
    public void Width1_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct W1Reg
            {
                [BitField(5, Width = 1)] public partial byte SingleBit { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// End equal to Start (single-bit field) via named syntax should work cleanly.
    /// </summary>
    [Fact]
    public void EndBitEqualsStartBit_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct EqReg
            {
                [BitField(3, End = 3)] public partial byte SingleBit { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// Redundant End+Width where both equal 1 should produce SD0016 (not SD0017).
    /// </summary>
    [Fact]
    public void RedundantSingleBit_ProducesSD0016()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct R1Reg
            {
                [BitField(5, End = 5, Width = 1)] public partial byte OneBit { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id == "SD0016").Should().HaveCount(1);
        diagnostics.Where(d => d.Id == "SD0017").Should().BeEmpty();
    }

    /// <summary>
    /// Large field using Width syntax (e.g., 64-bit) should work cleanly.
    /// </summary>
    [Fact]
    public void LargeFieldWidth_NoDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ulong))]
            public partial struct LargeReg
            {
                [BitField(0, Width = 32)] public partial uint Low { get; set; }
                [BitField(32, Width = 32)] public partial uint High { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    /// <summary>
    /// BitFlag attributes should NOT produce SD0015 (they use a single bit position, not the two-param BitField ctor).
    /// </summary>
    [Fact]
    public void BitFlag_NoBitFieldDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct FlagReg
            {
                [BitFlag(0)] public partial bool Ready { get; set; }
                [BitFlag(7)] public partial bool Valid { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE);

        diagnostics.Where(d => d.Id is "SD0015" or "SD0016" or "SD0017" or "SD0018" or "SD0019")
            .Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Runs the BitFieldsGenerator against the given source and returns the diagnostics.
    /// </summary>
    private static IReadOnlyList<Diagnostic> RunGeneratorAndGetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(BitFieldsAttribute).Assembly.Location),
        };

        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new BitFieldsGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()]);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out var diagnostics);

        return diagnostics;
    }

    /// <summary>
    /// Runs the BitFieldsViewGenerator against the given source and returns the diagnostics.
    /// </summary>
    private static IReadOnlyList<Diagnostic> RunViewGeneratorAndGetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(BitFieldsAttribute).Assembly.Location),
        };

        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new BitFieldsViewGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()]);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out var diagnostics);

        return diagnostics;
    }

    #endregion
}
