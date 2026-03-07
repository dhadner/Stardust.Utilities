using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stardust.Generators;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Tests that the BitFields generator emits correct diagnostics for nint/nuint
/// structs with fields above bit 31 based on PlatformTarget.
/// Uses CSharpGeneratorDriver to run the generator in isolation with controlled options.
/// </summary>
public class BitFieldNativeIntDiagnosticTests
{
    /// <summary>
    /// Source code for a nuint struct with fields above bit 31.
    /// </summary>
    private const string HIGH_BIT_NUINT_SOURCE = """
        using Stardust.Utilities;
        namespace TestDiag;

        [BitFields(typeof(nuint))]
        public partial struct HighBitNuintReg
        {
            [BitField(0, 7)] public partial byte Status { get; set; }
            [BitField(24, 55)] public partial uint Address { get; set; }
            [BitFlag(56)] public partial bool Valid { get; set; }
        }
        """;

    /// <summary>
    /// Source code for a nint struct with fields above bit 31.
    /// </summary>
    private const string HIGH_BIT_NINT_SOURCE = """
        using Stardust.Utilities;
        namespace TestDiag;

        [BitFields(typeof(nint))]
        public partial struct HighBitNintReg
        {
            [BitField(0, 7)] public partial byte Status { get; set; }
            [BitFlag(56)] public partial bool Valid { get; set; }
            [BitFlag(57)] public partial bool Ready { get; set; }
        }
        """;

    /// <summary>
    /// Source code for a nuint struct where all fields are within bits 0-31 (32-bit safe).
    /// </summary>
    private const string SAFE_32BIT_NUINT_SOURCE = """
        using Stardust.Utilities;
        namespace TestDiag;

        [BitFields(typeof(nuint))]
        public partial struct SafeNuintReg
        {
            [BitField(0, 7)] public partial byte Status { get; set; }
            [BitField(8, 11)] public partial byte Command { get; set; }
            [BitFlag(28)] public partial bool Enabled { get; set; }
            [BitFlag(31)] public partial bool HighestSafeBit { get; set; }
        }
        """;

    /// <summary>
    /// Source code for a plain uint struct (non-native) - should never produce SD0001/SD0002.
    /// </summary>
    private const string UINT_SOURCE = """
        using Stardust.Utilities;
        namespace TestDiag;

        [BitFields(typeof(uint))]
        public partial struct PlainUintReg
        {
            [BitField(0, 7)] public partial byte Status { get; set; }
            [BitFlag(31)] public partial bool Flag31 { get; set; }
        }
        """;

    #region x86 (32-bit only) - Error SD0001

    /// <summary>
    /// On x86, nuint fields above bit 31 should produce error SD0001.
    /// </summary>
    [Fact]
    public void X86_NuintHighBits_ProducesError()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "x86");

        var sd0001 = diagnostics.Where(d => d.Id == "SD0001").ToList();
        sd0001.Should().HaveCount(2); // Address (bit 55) and Valid (bit 56)
        sd0001.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
        sd0001.Should().Contain(d => d.GetMessage().Contains("Address"));
        sd0001.Should().Contain(d => d.GetMessage().Contains("Valid"));
    }

    /// <summary>
    /// On x86, nint fields above bit 31 should produce error SD0001.
    /// </summary>
    [Fact]
    public void X86_NintHighBits_ProducesError()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NINT_SOURCE, "x86");

        var sd0001 = diagnostics.Where(d => d.Id == "SD0001").ToList();
        sd0001.Should().HaveCount(2); // Valid (bit 56) and Ready (bit 57)
        sd0001.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// On x86, fields within bits 0-31 should NOT produce diagnostics.
    /// </summary>
    [Fact]
    public void X86_SafeNuint_NoDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(SAFE_32BIT_NUINT_SOURCE, "x86");

        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// On x86, field accessing exactly bit 31 (the highest safe bit) should not produce a diagnostic.
    /// </summary>
    [Fact]
    public void X86_Bit31_IsStillSafe()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(nuint))]
            public partial struct Bit31Reg
            {
                [BitFlag(31)] public partial bool HighestSafe { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "x86");
        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// On x86, field accessing bit 32 (the first unsafe bit) should produce an error.
    /// </summary>
    [Fact]
    public void X86_Bit32_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(nuint))]
            public partial struct Bit32Reg
            {
                [BitFlag(32)] public partial bool FirstUnsafe { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "x86");
        var sd0001 = diagnostics.Where(d => d.Id == "SD0001").ToList();
        sd0001.Should().HaveCount(1);
        sd0001[0].GetMessage().Should().Contain("FirstUnsafe");
        sd0001[0].GetMessage().Should().Contain("32");
    }

    /// <summary>
    /// On x86, a BitField that straddles the 32-bit boundary should produce an error.
    /// </summary>
    [Fact]
    public void X86_FieldStraddling32BitBoundary_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(nuint))]
            public partial struct StraddleReg
            {
                [BitField(24, 39)] public partial ushort Crossing { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "x86");
        var sd0001 = diagnostics.Where(d => d.Id == "SD0001").ToList();
        sd0001.Should().HaveCount(1);
        sd0001[0].GetMessage().Should().Contain("Crossing");
    }

    #endregion

    #region AnyCPU (default) - Warning SD0002

    /// <summary>
    /// On AnyCPU (empty PlatformTarget), nuint fields above bit 31 should produce warning SD0002.
    /// </summary>
    [Fact]
    public void AnyCPU_NuintHighBits_ProducesWarning()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "");

        var sd0002 = diagnostics.Where(d => d.Id == "SD0002").ToList();
        sd0002.Should().HaveCount(2); // Address (bit 55) and Valid (bit 56)
        sd0002.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// On explicit AnyCPU, nint fields above bit 31 should produce warning SD0002.
    /// </summary>
    [Fact]
    public void AnyCPU_Explicit_NintHighBits_ProducesWarning()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NINT_SOURCE, "AnyCPU");

        var sd0002 = diagnostics.Where(d => d.Id == "SD0002").ToList();
        sd0002.Should().HaveCount(2);
        sd0002.Should().OnlyContain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// On AnyCPU, safe 32-bit nuint structs should NOT produce any diagnostics.
    /// </summary>
    [Fact]
    public void AnyCPU_SafeNuint_NoDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(SAFE_32BIT_NUINT_SOURCE, "");

        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// On AnyCPU, SD0002 should not be emitted for low-bit fields.
    /// </summary>
    [Fact]
    public void AnyCPU_LowBitField_NoWarning()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "");

        // Status (bits 0-7) should NOT have a warning
        var statusDiags = diagnostics.Where(d => d.GetMessage().Contains("'Status'")).ToList();
        statusDiags.Should().BeEmpty();
    }

    #endregion

    #region x64 / ARM64 - No Diagnostics

    /// <summary>
    /// On x64, no diagnostics should be emitted even for high-bit fields.
    /// </summary>
    [Fact]
    public void X64_HighBits_NoDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "x64");

        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// On ARM64, no diagnostics should be emitted even for high-bit fields.
    /// </summary>
    [Fact]
    public void ARM64_HighBits_NoDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "ARM64");

        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// On x64, nint high-bit fields should not produce diagnostics.
    /// </summary>
    [Fact]
    public void X64_NintHighBits_NoDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NINT_SOURCE, "x64");

        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    #endregion

    #region Non-native types - No Diagnostics

    /// <summary>
    /// A plain uint struct (not nint/nuint) should never produce SD0001 or SD0002.
    /// </summary>
    [Fact]
    public void Uint_NeverProducesDiagnostics()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(UINT_SOURCE, "x86");
        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    /// <summary>
    /// A ulong struct should never produce native int diagnostics.
    /// </summary>
    [Fact]
    public void Ulong_NeverProducesDiagnostics()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(ulong))]
            public partial struct UlongReg
            {
                [BitFlag(63)] public partial bool HighBit { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "x86");
        diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty();
    }

    #endregion

    #region Diagnostic Message Content

    /// <summary>
    /// Verifies the diagnostic message contains the field name, struct name, and bit number.
    /// </summary>
    [Fact]
    public void DiagnosticMessage_ContainsRelevantInfo()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "x86");

        var validDiag = diagnostics.First(d => d.GetMessage().Contains("Valid"));
        validDiag.GetMessage().Should().Contain("Valid");
        validDiag.GetMessage().Should().Contain("HighBitNuintReg");
        validDiag.GetMessage().Should().Contain("56");
    }

    /// <summary>
    /// Verifies the diagnostic has a source location (not just a general diagnostic).
    /// </summary>
    [Fact]
    public void DiagnosticLocation_PointsToProperty()
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, "x86");

        var diags = diagnostics.Where(d => d.Id == "SD0001").ToList();
        diags.Should().AllSatisfy(d => d.Location.Should().NotBe(Location.None));
    }

    #endregion

    #region PlatformTarget Case Sensitivity

    /// <summary>
    /// PlatformTarget comparison should be case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("X86", true, false)]
    [InlineData("x86", true, false)]
    [InlineData("X64", false, false)]
    [InlineData("x64", false, false)]
    [InlineData("arm64", false, false)]
    [InlineData("ARM64", false, false)]
    [InlineData("", false, true)]
    [InlineData("AnyCPU", false, true)]
    public void PlatformTarget_CaseInsensitive(string platform, bool expectErrors, bool expectWarnings)
    {
        var diagnostics = RunGeneratorAndGetDiagnostics(HIGH_BIT_NUINT_SOURCE, platform);

        if (expectErrors)
        {
            diagnostics.Where(d => d.Id == "SD0001").Should().NotBeEmpty("x86 should produce errors");
            diagnostics.Where(d => d.Id == "SD0002").Should().BeEmpty("x86 should not produce warnings");
        }
        else if (expectWarnings)
        {
            diagnostics.Where(d => d.Id == "SD0002").Should().NotBeEmpty("AnyCPU should produce warnings");
            diagnostics.Where(d => d.Id == "SD0001").Should().BeEmpty("AnyCPU should not produce errors");
        }
        else
        {
            diagnostics.Where(d => d.Id is "SD0001" or "SD0002").Should().BeEmpty("64-bit should produce no diagnostics");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Runs the BitFieldsGenerator against the given source with the specified PlatformTarget
    /// and returns the diagnostics produced by the generator.
    /// </summary>
    private static IReadOnlyList<Diagnostic> RunGeneratorAndGetDiagnostics(string source, string platformTarget)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Build references - we need the Stardust.Utilities assembly for the attribute types
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(BitFieldsAttribute).Assembly.Location),
        };

        // Add System.Runtime (needed for basic type resolution)
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Configure PlatformTarget via AnalyzerConfigOptions
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(platformTarget);

        var generator = new BitFieldsGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            optionsProvider: optionsProvider);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out var diagnostics);

        return diagnostics;
    }

    /// <summary>
    /// Minimal AnalyzerConfigOptionsProvider that returns PlatformTarget as a global option.
    /// </summary>
    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestGlobalOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(string platformTarget)
        {
            _globalOptions = new TestGlobalOptions(platformTarget);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestEmptyOptions.INSTANCE;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestEmptyOptions.INSTANCE;

        private sealed class TestGlobalOptions : AnalyzerConfigOptions
        {
            private readonly string _platformTarget;

            public TestGlobalOptions(string platformTarget)
            {
                _platformTarget = platformTarget;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (key == "build_property.PlatformTarget")
                {
                    value = _platformTarget;
                    return true;
                }
                value = null;
                return false;
            }
        }

        private sealed class TestEmptyOptions : AnalyzerConfigOptions
        {
            public static readonly TestEmptyOptions INSTANCE = new();

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }
        }
    }

    #endregion

    #region Unsupported Storage Type - Error SD0003

    /// <summary>
    /// Using typeof(Guid) should produce error SD0003.
    /// </summary>
    [Fact]
    public void UnsupportedType_Guid_ProducesError()
    {
        const string SOURCE = """
            using System;
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(Guid))]
            public partial struct GuidReg
            {
                [BitField(0, 7)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");

        var sd0003 = diagnostics.Where(d => d.Id == "SD0003").ToList();
        sd0003.Should().HaveCount(1);
        sd0003[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0003[0].GetMessage().Should().Contain("Guid");
        sd0003[0].GetMessage().Should().Contain("GuidReg");
    }

    /// <summary>
    /// Using typeof(string) should produce error SD0003.
    /// </summary>
    [Fact]
    public void UnsupportedType_String_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(string))]
            public partial struct StringReg
            {
                [BitFlag(0)] public partial bool Flag { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");

        var sd0003 = diagnostics.Where(d => d.Id == "SD0003").ToList();
        sd0003.Should().HaveCount(1);
        sd0003[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0003[0].GetMessage().Should().Contain("string");
        sd0003[0].GetMessage().Should().Contain("StringReg");
    }

    /// <summary>
    /// Using typeof(bool) should produce error SD0003.
    /// </summary>
    [Fact]
    public void UnsupportedType_Bool_ProducesError()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(bool))]
            public partial struct BoolReg
            {
                [BitFlag(0)] public partial bool Flag { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");

        var sd0003 = diagnostics.Where(d => d.Id == "SD0003").ToList();
        sd0003.Should().HaveCount(1);
        sd0003[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sd0003[0].GetMessage().Should().Contain("bool");
    }

    /// <summary>
    /// Supported types like byte should NOT produce SD0003.
    /// </summary>
    [Fact]
    public void SupportedType_Byte_NoDiagnostic()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(byte))]
            public partial struct ByteReg
            {
                [BitFlag(0)] public partial bool Flag { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");
        diagnostics.Where(d => d.Id == "SD0003").Should().BeEmpty();
    }

    /// <summary>
    /// Supported type Int128 should NOT produce SD0003.
    /// </summary>
    [Fact]
    public void SupportedType_Int128_NoDiagnostic()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(Int128))]
            public partial struct Int128Reg
            {
                [BitField(0, 7)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");
        diagnostics.Where(d => d.Id == "SD0003").Should().BeEmpty();
    }

    /// <summary>
    /// The diagnostic message should list supported types so the user knows what to use.
    /// </summary>
    [Fact]
    public void UnsupportedType_MessageListsSupportedTypes()
    {
        const string SOURCE = """
            using System;
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(Guid))]
            public partial struct GuidReg
            {
                [BitFlag(0)] public partial bool Flag { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");

        var sd0003 = diagnostics.First(d => d.Id == "SD0003");
        var message = sd0003.GetMessage();
        message.Should().Contain("byte");
        message.Should().Contain("ulong");
        message.Should().Contain("UInt128");
        message.Should().Contain("Half");
        message.Should().Contain("decimal");
    }

    /// <summary>
    /// The diagnostic should point to a source location (the struct).
    /// </summary>
    [Fact]
    public void UnsupportedType_DiagnosticHasLocation()
    {
        const string SOURCE = """
            using System;
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(typeof(Guid))]
            public partial struct GuidReg
            {
                [BitFlag(0)] public partial bool Flag { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");
        var sd0003 = diagnostics.First(d => d.Id == "SD0003");
        sd0003.Location.Should().NotBe(Location.None);
    }

    /// <summary>
    /// Bit-count constructor [BitFields(200)] should NOT produce SD0003.
    /// </summary>
    [Fact]
    public void BitCountConstructor_NoDiagnostic()
    {
        const string SOURCE = """
            using Stardust.Utilities;
            namespace TestDiag;

            [BitFields(200)]
            public partial struct WideReg
            {
                [BitField(0, 7)] public partial byte Status { get; set; }
            }
            """;

        var diagnostics = RunGeneratorAndGetDiagnostics(SOURCE, "");
        diagnostics.Where(d => d.Id == "SD0003").Should().BeEmpty();
    }

    #endregion
}
