using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Stardust.Utilities.Tests;

/// <summary>
/// Unit tests for arbitrary-size multi-word [BitFields] structs.
/// </summary>
public partial class BitFieldMultiWordTests
{
    #region Test Struct Definitions

    /// <summary>128-bit struct (2 words).</summary>
    [BitFields(128)]
    public partial struct Bits128
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }     // bits 0-63
        [BitField(64, 127)] public partial ulong High { get; set; }  // bits 64-127
    }

    /// <summary>200-bit struct (4 words, last word partially used).</summary>
    [BitFields(200)]
    public partial struct Bits200
    {
        [BitField(0, 63)] public partial ulong Word0 { get; set; }    // bits 0-63
        [BitField(64, 127)] public partial ulong Word1 { get; set; }  // bits 64-127
        [BitField(128, 191)] public partial ulong Word2 { get; set; } // bits 128-191
        [BitFlag(192)] public partial bool Valid { get; set; }         // bit 192
        [BitField(193, 199)] public partial byte Tag { get; set; }    // bits 193-199 (7 bits)
    }

    /// <summary>Struct with cross-word field (spans word boundary).</summary>
    [BitFields(128)]
    public partial struct CrossWord128
    {
        [BitField(60, 67)] public partial byte CrossField { get; set; }  // bits 60-67 (spans words 0-1)
        [BitFlag(0)] public partial bool LowBit { get; set; }           // bit 0
        [BitFlag(127)] public partial bool HighBit { get; set; }        // bit 127
    }

    /// <summary>Small multi-word struct for simple tests.</summary>
    [BitFields(65)]
    public partial struct Bits65
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }  // bits 0-63
        [BitFlag(64)] public partial bool ExtraBit { get; set; }   // bit 64
    }

    /// <summary>256-bit struct (4 full words).</summary>
    [BitFields(256)]
    public partial struct Bits256
    {
        [BitField(0, 63)] public partial ulong W0 { get; set; }
        [BitField(64, 127)] public partial ulong W1 { get; set; }
        [BitField(128, 191)] public partial ulong W2 { get; set; }
        [BitField(192, 255)] public partial ulong W3 { get; set; }
    }

    /// <summary>512-bit struct (8 words).</summary>
    [BitFields(512)]
    public partial struct Bits512
    {
        [BitField(0, 63)] public partial ulong W0 { get; set; }
        [BitField(64, 127)] public partial ulong W1 { get; set; }
        [BitField(448, 511)] public partial ulong W7 { get; set; }
        [BitFlag(256)] public partial bool MidFlag { get; set; }
    }

    /// <summary>128-bit with UndefinedBitsMustBe.Zeroes.</summary>
    [BitFields(128, UndefinedBitsMustBe.Zeroes)]
    public partial struct Bits128Zeroed
    {
        [BitField(0, 31)] public partial uint Low { get; set; }   // bits 0-31
        [BitFlag(64)] public partial bool Flag { get; set; }       // bit 64
        // Bits 32-63, 65-127 undefined ? forced to zero
    }

    /// <summary>72-bit struct: 1 ulong + 1 byte = 9 bytes (NOT 16).</summary>
    [BitFields(72)]
    public partial struct Bits72
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }   // bits 0-63
        [BitField(64, 71)] public partial byte High { get; set; }   // bits 64-71
    }

    /// <summary>80-bit struct: 1 ulong + 1 ushort = 10 bytes.</summary>
    [BitFields(80)]
    public partial struct Bits80
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }
        [BitField(64, 79)] public partial ushort High { get; set; }
    }

    /// <summary>96-bit struct: 1 ulong + 1 uint = 12 bytes.</summary>
    [BitFields(96)]
    public partial struct Bits96
    {
        [BitField(0, 63)] public partial ulong Low { get; set; }
        [BitField(64, 95)] public partial uint High { get; set; }
    }

    /// <summary>16384-bit struct (256 words) — maximum supported size.</summary>
    [BitFields(16384)]
    public partial struct Bits16384
    {
        [BitField(0, 63)] public partial ulong W0 { get; set; }          // first word
        [BitField(8128, 8191)] public partial ulong WMid { get; set; }   // middle word (word 127)
        [BitField(16320, 16383)] public partial ulong WLast { get; set; }// last word (word 255)
        [BitFlag(0)] public partial bool LowBit { get; set; }            // bit 0
        [BitFlag(16383)] public partial bool HighBit { get; set; }       // bit 16383
    }


    #endregion

    #region Construction Tests

    [Fact]
    public void DefaultConstruction_AllZero()
    {
        Bits128 b = default;
        b.Low.Should().Be(0);
        b.High.Should().Be(0);
    }

    [Fact]
    public void UlongConstructor_ZeroExtends()
    {
        Bits128 b = 0xDEADBEEF;
        b.Low.Should().Be(0xDEADBEEF);
        b.High.Should().Be(0);
    }

    [Fact]
    public void FullConstructor_SetsAllWords()
    {
        var b = new Bits128(0x1111111111111111, 0x2222222222222222);
        b.Low.Should().Be(0x1111111111111111);
        b.High.Should().Be(0x2222222222222222);
    }

    [Fact]
    public void Bits65_ExtraBitWorks()
    {
        Bits65 b = default;
        b.ExtraBit.Should().BeFalse();
        b.ExtraBit = true;
        b.ExtraBit.Should().BeTrue();
        b.Low.Should().Be(0);
    }

    [Fact]
    public void IntConstructor_NegativeOne_SetsAllBits_128()
    {
        var b = new Bits128(-1);
        b.Low.Should().Be(ulong.MaxValue);
        b.High.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void IntConstructor_NegativeOne_SetsAllBits_256()
    {
        var b = new Bits256(-1);
        b.W0.Should().Be(ulong.MaxValue);
        b.W1.Should().Be(ulong.MaxValue);
        b.W2.Should().Be(ulong.MaxValue);
        b.W3.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void IntConstructor_NegativeTwo_SignExtends()
    {
        var b = new Bits128(-2);
        b.Low.Should().Be(0xFFFFFFFFFFFFFFFE);
        b.High.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void IntConstructor_Positive_ZeroExtends()
    {
        var b = new Bits128(42);
        b.Low.Should().Be(42);
        b.High.Should().Be(0);
    }

    [Fact]
    public void IntConstructor_NegativeOne_SetsAllBits_72()
    {
        var b = new Bits72(-1);
        b.Low.Should().Be(ulong.MaxValue);
        b.High.Should().Be(0xFF, "all 8 bits of byte word should be set");
    }

    [Fact]
    public void IntConstructor_NegativeOne_SetsAllBits_512()
    {
        var b = new Bits512(-1);
        b.W0.Should().Be(ulong.MaxValue);
        b.W1.Should().Be(ulong.MaxValue);
        b.W7.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void IntConstructor_NegativeOne_SetsAllBits_16384()
    {
        var b = new Bits16384(-1);
        b.W0.Should().Be(ulong.MaxValue);
        b.WMid.Should().Be(ulong.MaxValue);
        b.WLast.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void ZeroProperty_IsDefault()
    {
        Bits128.Zero.Low.Should().Be(0);
        Bits128.Zero.High.Should().Be(0);
        (Bits128.Zero == default(Bits128)).Should().BeTrue();
    }

    [Fact]
    public void ZeroProperty_FluentBuild()
    {
        var b = Bits200.Zero.WithWord0(0xAAAA).WithValid(true).WithTag(42);
        b.Word0.Should().Be(0xAAAA);
        b.Valid.Should().BeTrue();
        b.Tag.Should().Be(42);
    }

    #endregion

    #region Property Access Tests

    [Fact]
    public void SingleWordField_GetSet()
    {
        Bits200 b = default;
        b.Word0 = 0xCAFEBABE_DEADBEEF;
        b.Word0.Should().Be(0xCAFEBABE_DEADBEEF);
        b.Word1.Should().Be(0);
        b.Word2.Should().Be(0);
    }

    [Fact]
    public void BitFlag_GetSet()
    {
        Bits200 b = default;
        b.Valid = true;
        b.Valid.Should().BeTrue();
        b.Word0.Should().Be(0);
        b.Word1.Should().Be(0);
        b.Word2.Should().Be(0);
    }

    [Fact]
    public void SmallField_GetSet()
    {
        Bits200 b = default;
        b.Tag = 0x7F; // 7 bits max
        b.Tag.Should().Be(0x7F);
        b.Valid.Should().BeFalse();
    }

    [Fact]
    public void MultipleFields_Independent()
    {
        Bits200 b = default;
        b.Word0 = 0xAAAAAAAAAAAAAAAA;
        b.Word1 = 0xBBBBBBBBBBBBBBBB;
        b.Word2 = 0xCCCCCCCCCCCCCCCC;
        b.Valid = true;
        b.Tag = 42;

        b.Word0.Should().Be(0xAAAAAAAAAAAAAAAA);
        b.Word1.Should().Be(0xBBBBBBBBBBBBBBBB);
        b.Word2.Should().Be(0xCCCCCCCCCCCCCCCC);
        b.Valid.Should().BeTrue();
        b.Tag.Should().Be(42);
    }

    #endregion

    #region Cross-Word Field Tests

    [Fact]
    public void CrossWordField_Set_SpansTwoWords()
    {
        CrossWord128 b = default;
        b.CrossField = 0xFF;
        b.CrossField.Should().Be(0xFF);
    }

    [Fact]
    public void CrossWordField_SetClear()
    {
        CrossWord128 b = default;
        b.CrossField = 0xAB;
        b.CrossField.Should().Be(0xAB);
        b.CrossField = 0;
        b.CrossField.Should().Be(0);
    }

    [Fact]
    public void CrossWordField_DoesNotCorruptOtherBits()
    {
        CrossWord128 b = default;
        b.LowBit = true;
        b.HighBit = true;
        b.CrossField = 0xFF;

        b.LowBit.Should().BeTrue("LowBit should not be affected by CrossField");
        b.HighBit.Should().BeTrue("HighBit should not be affected by CrossField");
        b.CrossField.Should().Be(0xFF);
    }

    [Fact]
    public void CrossWordField_PartialValues()
    {
        CrossWord128 b = default;

        // Set a value that uses both words
        b.CrossField = 0x35; // binary: 00110101
        b.CrossField.Should().Be(0x35);

        b.CrossField = 1;
        b.CrossField.Should().Be(1);
    }

    #endregion

    #region Bitwise Operator Tests

    [Fact]
    public void BitwiseComplement()
    {
        var a = new Bits128(0xFFFFFFFFFFFFFFFF, 0);
        var result = ~a;
        result.Low.Should().Be(0);
        result.High.Should().Be(0xFFFFFFFFFFFFFFFF);
    }

    [Fact]
    public void BitwiseOr()
    {
        var a = new Bits128(0xFF00FF00FF00FF00, 0);
        var b = new Bits128(0x00FF00FF00FF00FF, 0);
        var result = a | b;
        result.Low.Should().Be(0xFFFFFFFFFFFFFFFF);
        result.High.Should().Be(0);
    }

    [Fact]
    public void BitwiseAnd()
    {
        var a = new Bits128(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF);
        var b = new Bits128(0xFF00FF00FF00FF00, 0x00FF00FF00FF00FF);
        var result = a & b;
        result.Low.Should().Be(0xFF00FF00FF00FF00);
        result.High.Should().Be(0x00FF00FF00FF00FF);
    }

    [Fact]
    public void BitwiseXor()
    {
        var a = new Bits128(0xAAAAAAAAAAAAAAAA, 0x5555555555555555);
        var b = new Bits128(0x5555555555555555, 0xAAAAAAAAAAAAAAAA);
        var result = a ^ b;
        result.Low.Should().Be(0xFFFFFFFFFFFFFFFF);
        result.High.Should().Be(0xFFFFFFFFFFFFFFFF);
    }

    #endregion

    #region Arithmetic Operator Tests

    [Fact]
    public void Addition_NoCarry()
    {
        var a = new Bits128(10, 0);
        var b = new Bits128(20, 0);
        var result = a + b;
        result.Low.Should().Be(30);
        result.High.Should().Be(0);
    }

    [Fact]
    public void Addition_WithCarry()
    {
        var a = new Bits128(ulong.MaxValue, 0);
        var b = new Bits128(1, 0);
        var result = a + b;
        result.Low.Should().Be(0);
        result.High.Should().Be(1);
    }

    [Fact]
    public void Addition_WithCarry_MultiWord()
    {
        var a = new Bits128(ulong.MaxValue, 0x100);
        var b = new Bits128(1, 0);
        var result = a + b;
        result.Low.Should().Be(0);
        result.High.Should().Be(0x101);
    }

    [Fact]
    public void Addition_WithUlong()
    {
        Bits128 a = 100UL;
        var result = a + 50UL;
        result.Low.Should().Be(150);
    }

    [Fact]
    public void Subtraction_NoBorrow()
    {
        var a = new Bits128(30, 0);
        var b = new Bits128(10, 0);
        var result = a - b;
        result.Low.Should().Be(20);
        result.High.Should().Be(0);
    }

    [Fact]
    public void Subtraction_WithBorrow()
    {
        var a = new Bits128(0, 1);
        var b = new Bits128(1, 0);
        var result = a - b;
        result.Low.Should().Be(ulong.MaxValue);
        result.High.Should().Be(0);
    }

    [Fact]
    public void UnaryNegation()
    {
        var a = new Bits128(1, 0);
        var neg = -a;
        // -1 in two's complement = all bits set
        neg.Low.Should().Be(ulong.MaxValue);
        neg.High.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void Multiplication_Simple()
    {
        Bits128 a = 100UL;
        var result = a * 200UL;
        result.Low.Should().Be(20000);
        result.High.Should().Be(0);
    }

    [Fact]
    public void Multiplication_Large()
    {
        // Test multiplication that would overflow a single ulong
        Bits128 a = ulong.MaxValue;
        var result = a * 2UL;
        // ulong.MaxValue * 2 = 0x1_FFFFFFFFFFFFFFFE
        result.Low.Should().Be(0xFFFFFFFFFFFFFFFE);
        result.High.Should().Be(1);
    }

    [Fact]
    public void Division_Simple()
    {
        Bits128 a = 100UL;
        var result = a / 10UL;
        result.Low.Should().Be(10);
    }

    [Fact]
    public void Modulus_Simple()
    {
        Bits128 a = 100UL;
        var result = a % 7UL;
        result.Low.Should().Be(2);
    }

    #endregion

    #region Shift Operator Tests

    [Fact]
    public void LeftShift_WithinWord()
    {
        Bits128 a = 1UL;
        var result = a << 10;
        result.Low.Should().Be(1UL << 10);
        result.High.Should().Be(0);
    }

    [Fact]
    public void LeftShift_AcrossWordBoundary()
    {
        Bits128 a = 1UL;
        var result = a << 64;
        result.Low.Should().Be(0);
        result.High.Should().Be(1);
    }

    [Fact]
    public void LeftShift_PartialAcrossBoundary()
    {
        Bits128 a = 0xFFUL;
        var result = a << 60;
        // 0xFF << 60: low 4 bits stay in word 0, high 4 bits go to word 1
        result.Low.Should().Be(0xF000000000000000UL);
        result.High.Should().Be(0xFUL);
    }

    [Fact]
    public void LeftShift_ByZero()
    {
        var a = new Bits128(0x1234, 0x5678);
        var result = a << 0;
        result.Low.Should().Be(0x1234);
        result.High.Should().Be(0x5678);
    }

    [Fact]
    public void LeftShift_FullWidth()
    {
        Bits128 a = 1UL;
        var result = a << 128;
        result.Low.Should().Be(0);
        result.High.Should().Be(0);
    }

    [Fact]
    public void RightShift_WithinWord()
    {
        Bits128 a = 0x100UL;
        var result = a >> 4;
        result.Low.Should().Be(0x10UL);
    }

    [Fact]
    public void RightShift_AcrossWordBoundary()
    {
        var a = new Bits128(0, 1);
        var result = a >> 64;
        result.Low.Should().Be(1);
        result.High.Should().Be(0);
    }

    [Fact]
    public void RightShift_PartialAcrossBoundary()
    {
        var a = new Bits128(0, 0xFF);
        var result = a >> 60;
        // 0xFF in word 1, right shift 60: word0 gets _w1 << (64-60) = 0xFF << 4 = 0xFF0
        // word1 gets _w1 >> 60 = 0
        result.Low.Should().Be(0xFF0UL);
        result.High.Should().Be(0UL);
    }

    #endregion

    #region Comparison Operator Tests

    [Fact]
    public void Equality_EqualValues()
    {
        var a = new Bits128(0x1234, 0x5678);
        var b = new Bits128(0x1234, 0x5678);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues()
    {
        var a = new Bits128(0x1234, 0x5678);
        var b = new Bits128(0x1234, 0x9999);
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void LessThan_ByHighWord()
    {
        var a = new Bits128(ulong.MaxValue, 0);
        var b = new Bits128(0, 1);
        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void LessThan_ByLowWord()
    {
        var a = new Bits128(10, 5);
        var b = new Bits128(20, 5);
        (a < b).Should().BeTrue();
        (b > a).Should().BeTrue();
    }

    [Fact]
    public void LessThanOrEqual_Equal()
    {
        var a = new Bits128(42, 42);
        var b = a;
        (a <= b).Should().BeTrue();
        (a >= b).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_Ordering()
    {
        var a = new Bits128(1, 0);
        var b = new Bits128(2, 0);
        var c = new Bits128(1, 0);

        a.CompareTo(b).Should().BeLessThan(0);
        b.CompareTo(a).Should().BeGreaterThan(0);
        a.CompareTo(c).Should().Be(0);
    }

    #endregion

    #region BigInteger Conversion Tests

    [Fact]
    public void ToBigInteger_LowWordOnly()
    {
        Bits128 b = 0xDEADBEEF;
        var bi = (BigInteger)b;
        bi.Should().Be(new BigInteger(0xDEADBEEF));
    }

    [Fact]
    public void ToBigInteger_BothWords()
    {
        var b = new Bits128(0xBBBBBBBBBBBBBBBB, 0xAAAAAAAAAAAAAAAA);
        var bi = (BigInteger)b;
        var expected = (new BigInteger(0xAAAAAAAAAAAAAAAA) << 64) | new BigInteger(0xBBBBBBBBBBBBBBBB);
        bi.Should().Be(expected);
    }

    [Fact]
    public void FromBigInteger_RoundTrip()
    {
        var original = (BigInteger.One << 100) + 42;
        var bits = (Bits128)original;
        var roundTrip = (BigInteger)bits;
        roundTrip.Should().Be(original);
    }

    #endregion

    #region Parsing Tests

    [Fact]
    public void Parse_Decimal()
    {
        Bits128 b = Bits128.Parse("12345");
        b.Low.Should().Be(12345);
        b.High.Should().Be(0);
    }

    [Fact]
    public void Parse_Hex()
    {
        Bits128 b = Bits128.Parse("0xDEADBEEF");
        b.Low.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Parse_Binary()
    {
        Bits128 b = Bits128.Parse("0b11111111");
        b.Low.Should().Be(0xFF);
    }

    [Fact]
    public void Parse_WithUnderscores()
    {
        Bits128 b = Bits128.Parse("0xFF_FF");
        b.Low.Should().Be(0xFFFF);
    }

    [Fact]
    public void TryParse_Valid()
    {
        Bits128.TryParse("42", out var result).Should().BeTrue();
        result.Low.Should().Be(42);
    }

    [Fact]
    public void TryParse_Invalid()
    {
        Bits128.TryParse("not_a_number", out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Null()
    {
        Bits128.TryParse(null, out _).Should().BeFalse();
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void ToString_Default()
    {
        Bits128 b = 0xFF;
        b.ToString().Should().Contain("FF");
    }

    [Fact]
    public void ToString_FormatX()
    {
        Bits128 b = 255;
        var str = b.ToString("X", null);
        // BigInteger.ToString("X") prefixes a '0' to ensure positive representation
        str.Should().Be("0FF");
    }

    [Fact]
    public void TryFormat_Success()
    {
        Bits128 b = 0xAB;
        Span<char> buffer = stackalloc char[20];
        b.TryFormat(buffer, out int written, "X", null).Should().BeTrue();
        written.Should().BeGreaterThan(0);
    }

    #endregion

    #region Equality and Hashing Tests

    [Fact]
    public void Equals_Object_SameValue()
    {
        var a = new Bits128(42, 99);
        object b = new Bits128(42, 99);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_DifferentValue()
    {
        var a = new Bits128(42, 99);
        object b = new Bits128(42, 100);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WrongType()
    {
        var a = new Bits128(42, 99);
        a.Equals("not a Bits128").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_EqualValues_SameHash()
    {
        var a = new Bits128(42, 99);
        var b = new Bits128(42, 99);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region With (Fluent) Method Tests

    [Fact]
    public void WithFlag_SetsFlag()
    {
        Bits200 b = default;
        var result = b.WithValid(true);
        result.Valid.Should().BeTrue();
        b.Valid.Should().BeFalse("original should be unchanged");
    }

    [Fact]
    public void WithField_SetsField()
    {
        Bits200 b = default;
        var result = b.WithTag(42);
        result.Tag.Should().Be(42);
        b.Tag.Should().Be(0, "original should be unchanged");
    }

    #endregion

    #region Static Bit/Mask Property Tests

    [Fact]
    public void StaticBitProperty_ReturnsCorrectBit()
    {
        var bit = Bits200.ValidBit;
        bit.Valid.Should().BeTrue();
        bit.Word0.Should().Be(0);
        bit.Word1.Should().Be(0);
        bit.Word2.Should().Be(0);
    }

    [Fact]
    public void StaticMaskProperty_ReturnsCorrectMask()
    {
        var mask = Bits200.TagMask;
        mask.Tag.Should().Be(0x7F); // 7 bits all set
    }

    #endregion

    #region 256-bit and 512-bit Tests

    [Fact]
    public void Bits256_AllWordsIndependent()
    {
        Bits256 b = default;
        b.W0 = 0x1111111111111111;
        b.W1 = 0x2222222222222222;
        b.W2 = 0x3333333333333333;
        b.W3 = 0x4444444444444444;

        b.W0.Should().Be(0x1111111111111111);
        b.W1.Should().Be(0x2222222222222222);
        b.W2.Should().Be(0x3333333333333333);
        b.W3.Should().Be(0x4444444444444444);
    }

    [Fact]
    public void Bits256_LeftShift_AcrossMultipleWords()
    {
        Bits256 b = default;
        b.W0 = 1;
        var result = b << 192;
        result.W0.Should().Be(0);
        result.W1.Should().Be(0);
        result.W2.Should().Be(0);
        result.W3.Should().Be(1);
    }

    [Fact]
    public void Bits256_Addition_MultiCarry()
    {
        var a = new Bits256(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, 0);
        Bits256 b = 1UL;
        var result = a + b;
        result.W0.Should().Be(0);
        result.W1.Should().Be(0);
        result.W2.Should().Be(0);
        result.W3.Should().Be(1);
    }

    [Fact]
    public void Bits512_HighWord()
    {
        Bits512 b = default;
        b.W7 = 0xDEADBEEF;
        b.W7.Should().Be(0xDEADBEEF);
        b.W0.Should().Be(0);
    }

    [Fact]
    public void Bits512_MidFlag()
    {
        Bits512 b = default;
        b.MidFlag = true;
        b.MidFlag.Should().BeTrue();
        b.W0.Should().Be(0);
    }

    #endregion

    #region UndefinedBitsMustBe Tests

    [Fact]
    public void UndefinedBitsZeroed_MasksUndefinedBits()
    {
        // Bits128Zeroed has fields at 0-31 and flag at 64
        // Undefined bits: 32-63, 65-127 ? should be zero
        var b = new Bits128Zeroed(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF);
        b.Low.Should().Be(0xFFFFFFFF); // bits 0-31 preserved
        b.Flag.Should().BeTrue();       // bit 64 preserved
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitFromUlong()
    {
        Bits128 b = 42UL;
        b.Low.Should().Be(42);
        b.High.Should().Be(0);
    }

    [Fact]
    public void ImplicitFromInt()
    {
        Bits128 b = 42;
        b.Low.Should().Be(42);
    }

    #endregion

    #region Struct Size Tests (byte-rounded backing)

    [Fact]
    public void Bits65_SizeIs9Bytes()
    {
        Unsafe.SizeOf<Bits65>().Should().Be(9, "65 bits ? ceil(65/8) = 9 bytes (1 ulong + 1 byte)");
    }

    [Fact]
    public void Bits72_SizeIs9Bytes()
    {
        Unsafe.SizeOf<Bits72>().Should().Be(9, "72 bits ? ceil(72/8) = 9 bytes (1 ulong + 1 byte)");
    }

    [Fact]
    public void Bits80_SizeIs10Bytes()
    {
        Unsafe.SizeOf<Bits80>().Should().Be(10, "80 bits ? ceil(80/8) = 10 bytes (1 ulong + 1 ushort)");
    }

    [Fact]
    public void Bits96_SizeIs12Bytes()
    {
        Unsafe.SizeOf<Bits96>().Should().Be(12, "96 bits ? ceil(96/8) = 12 bytes (1 ulong + 1 uint)");
    }

    [Fact]
    public void Bits128_SizeIs16Bytes()
    {
        Unsafe.SizeOf<Bits128>().Should().Be(16, "128 bits ? 16 bytes (2 ulongs, no remainder)");
    }

    [Fact]
    public void Bits200_SizeIs25Bytes()
    {
        Unsafe.SizeOf<Bits200>().Should().Be(25, "200 bits ? ceil(200/8) = 25 bytes (3 ulongs + 1 byte)");
    }

    [Fact]
    public void Bits256_SizeIs32Bytes()
    {
        Unsafe.SizeOf<Bits256>().Should().Be(32, "256 bits ? 32 bytes (4 ulongs, no remainder)");
    }

    [Fact]
    public void Bits512_SizeIs64Bytes()
    {
        Unsafe.SizeOf<Bits512>().Should().Be(64, "512 bits ? 64 bytes (8 ulongs, no remainder)");
    }

    [Fact]
    public void SizeInBytes_Constant_MatchesActualSize()
    {
        Bits128.SizeInBytes.Should().Be(Unsafe.SizeOf<Bits128>());
        Bits200.SizeInBytes.Should().Be(Unsafe.SizeOf<Bits200>());
        Bits256.SizeInBytes.Should().Be(Unsafe.SizeOf<Bits256>());
        Bits72.SizeInBytes.Should().Be(Unsafe.SizeOf<Bits72>());
    }

    #endregion

    #region Hybrid Word Type Tests

    [Fact]
    public void Bits72_FieldAccess()
    {
        Bits72 b = default;
        b.Low = 0xDEADBEEFCAFEBABE;
        b.High = 0xAB;
        b.Low.Should().Be(0xDEADBEEFCAFEBABE);
        b.High.Should().Be(0xAB);
    }

    [Fact]
    public void Bits72_BitwiseOps()
    {
        Bits72 a = default;
        a.Low = 0xFF;
        a.High = 0x0F;
        Bits72 b = default;
        b.Low = 0x0F;
        b.High = 0xF0;

        var result = a | b;
        result.Low.Should().Be(0xFF);
        result.High.Should().Be(0xFF);

        result = a & b;
        result.Low.Should().Be(0x0F);
        result.High.Should().Be(0x00);
    }

    [Fact]
    public void Bits72_Addition_CarryIntoByteWord()
    {
        Bits72 a = default;
        a.Low = ulong.MaxValue;
        a.High = 0;
        Bits72 b = default;
        b.Low = 1;

        var result = a + b;
        result.Low.Should().Be(0);
        result.High.Should().Be(1, "carry should propagate into byte word");
    }

    [Fact]
    public void Bits80_FieldAccess()
    {
        Bits80 b = default;
        b.Low = 0x1234567890ABCDEF;
        b.High = 0xBEEF;
        b.Low.Should().Be(0x1234567890ABCDEF);
        b.High.Should().Be(0xBEEF);
    }

    [Fact]
    public void Bits96_FieldAccess()
    {
        Bits96 b = default;
        b.Low = ulong.MaxValue;
        b.High = 0xDEADBEEF;
        b.Low.Should().Be(ulong.MaxValue);
        b.High.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void Bits72_BigIntegerRoundTrip()
    {
        var original = (BigInteger.One << 70) + 42;
        var bits = (Bits72)original;
        var roundTrip = (BigInteger)bits;
        roundTrip.Should().Be(original);
    }

    [Fact]
    public void Bits72_Complement()
    {
        Bits72 a = default;
        a.Low = 0;
        a.High = 0;
        var result = ~a;
        result.Low.Should().Be(ulong.MaxValue);
        result.High.Should().Be(0xFF, "complement of byte 0x00 is 0xFF");
    }

    [Fact]
    public void Bits72_Shift_IntoByteWord()
    {
        Bits72 a = default;
        a.Low = 1;
        var result = a << 64;
        result.Low.Should().Be(0);
        result.High.Should().Be(1);
    }

    [Fact]
    public void Bits72_Shift_OutOfByteWord()
    {
        Bits72 a = default;
        a.High = 0xFF;
        var result = a >> 64;
        result.Low.Should().Be(0xFF);
        result.High.Should().Be(0);
    }

    #endregion

    #region Byte Span Tests

    [Fact]
    public void Bits128_SpanConstructor_RoundTrips()
    {
        var original = new Bits128(0xDEADBEEF_CAFEBABE, 0x1234567890ABCDEF);
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(Bits128.SizeInBytes);
        var restored = new Bits128((ReadOnlySpan<byte>)bytes);
        restored.Should().Be(original);
    }

    [Fact]
    public void Bits128_ReadFrom_MatchesConstructor()
    {
        var original = new Bits128(0xFFFFFFFF_FFFFFFFF, 0x0000000000000001);
        var bytes = original.ToByteArray();
        var fromReadFrom = Bits128.ReadFrom(bytes);
        fromReadFrom.Should().Be(original);
    }

    [Fact]
    public void Bits128_WriteTo_ProducesCorrectBytes()
    {
        var value = new Bits128(0x0102030405060708, 0x090A0B0C0D0E0F10);
        Span<byte> buf = stackalloc byte[Bits128.SizeInBytes];
        value.WriteTo(buf);
        // Little-endian: low word first
        buf[0].Should().Be(0x08);
        buf[7].Should().Be(0x01);
        buf[8].Should().Be(0x10);
        buf[15].Should().Be(0x09);
    }

    [Fact]
    public void Bits128_TryWriteTo_SucceedsWithExactSize()
    {
        var value = new Bits128(42);
        Span<byte> buf = stackalloc byte[Bits128.SizeInBytes];
        value.TryWriteTo(buf, out int written).Should().BeTrue();
        written.Should().Be(Bits128.SizeInBytes);
    }

    [Fact]
    public void Bits128_TryWriteTo_FailsWithTooSmallSpan()
    {
        var value = new Bits128(42);
        Span<byte> buf = stackalloc byte[Bits128.SizeInBytes - 1];
        value.TryWriteTo(buf, out int written).Should().BeFalse();
        written.Should().Be(0);
    }

    [Fact]
    public void Bits128_SpanConstructor_ThrowsOnTooSmall()
    {
        var bytes = new byte[Bits128.SizeInBytes - 1];
        var act = () => new Bits128((ReadOnlySpan<byte>)bytes);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Bits72_SpanConstructor_RoundTrips()
    {
        // 72 bits = 1 ulong + 1 byte = 9 bytes
        Bits72 original = default;
        original.Low = 0xDEADBEEF_CAFEBABE;
        original.High = 0xAB;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(Bits72.SizeInBytes);
        bytes.Length.Should().Be(9);
        var restored = new Bits72((ReadOnlySpan<byte>)bytes);
        restored.Low.Should().Be(original.Low);
        restored.High.Should().Be(original.High);
    }

    [Fact]
    public void Bits80_SpanConstructor_RoundTrips()
    {
        // 80 bits = 1 ulong + 1 ushort = 10 bytes
        Bits80 original = default;
        original.Low = 0x1122334455667788;
        original.High = 0xAABB;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(10);
        var restored = new Bits80((ReadOnlySpan<byte>)bytes);
        restored.Low.Should().Be(original.Low);
        restored.High.Should().Be(original.High);
    }

    [Fact]
    public void Bits96_SpanConstructor_RoundTrips()
    {
        // 96 bits = 1 ulong + 1 uint = 12 bytes
        Bits96 original = default;
        original.Low = 0xDEADBEEF_DEADBEEF;
        original.High = 0xCAFEBABE;
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(12);
        var restored = new Bits96((ReadOnlySpan<byte>)bytes);
        restored.Low.Should().Be(original.Low);
        restored.High.Should().Be(original.High);
    }

    [Fact]
    public void Bits256_SpanConstructor_RoundTrips()
    {
        var original = new Bits256(0x1111111111111111, 0x2222222222222222,
                                    0x3333333333333333, 0x4444444444444444);
        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(Bits256.SizeInBytes);
        var restored = new Bits256((ReadOnlySpan<byte>)bytes);
        restored.W0.Should().Be(original.W0);
        restored.W1.Should().Be(original.W1);
        restored.W2.Should().Be(original.W2);
        restored.W3.Should().Be(original.W3);
    }

    [Fact]
    public void Bits128_SizeInBytes_Is16()
    {
        Bits128.SizeInBytes.Should().Be(16);
    }

    [Fact]
    public void Bits128_DefaultValue_RoundTripsToZeroBytes()
    {
        Bits128 value = default;
        var bytes = value.ToByteArray();
        bytes.Should().AllBeEquivalentTo((byte)0);
        var restored = new Bits128((ReadOnlySpan<byte>)bytes);
        restored.Should().Be(default(Bits128));
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void Bits128_JsonRoundTrip()
    {
        var original = new Bits128(0xDEADBEEF_CAFEBABE, 0x1234567890ABCDEF);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Bits128>(json);
        restored.Should().Be(original);
    }

    [Fact]
    public void Bits128_JsonSerializesAsString()
    {
        var value = new Bits128(0xFF, 0);
        var json = JsonSerializer.Serialize(value);
        json.Should().Contain("\"0x");
    }

    [Fact]
    public void Bits128_JsonDefaultRoundTrip()
    {
        Bits128 original = default;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Bits128>(json);
        restored.Should().Be(original);
    }

    [Fact]
    public void Bits256_JsonRoundTrip()
    {
        var original = new Bits256(0x1111111111111111, 0x2222222222222222,
                                    0x3333333333333333, 0x4444444444444444);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Bits256>(json);
        restored.W0.Should().Be(original.W0);
        restored.W1.Should().Be(original.W1);
        restored.W2.Should().Be(original.W2);
        restored.W3.Should().Be(original.W3);
    }

    [Fact]
    public void Bits72_JsonRoundTrip()
    {
        Bits72 original = default;
        original.Low = 0xCAFEBABE_DEADBEEF;
        original.High = 0xAB;
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Bits72>(json);
        restored.Low.Should().Be(original.Low);
        restored.High.Should().Be(original.High);
    }

    #endregion

    #region 16384-bit (Maximum Size) Tests

    [Fact]
    public void Bits16384_SizeIs2048Bytes()
    {
        // 16384 bits / 8 = 2048 bytes = 256 ulongs
        Bits16384.SizeInBytes.Should().Be(2048);
        Unsafe.SizeOf<Bits16384>().Should().Be(2048);
    }

    [Fact]
    public void Bits16384_DefaultIsAllZero()
    {
        Bits16384 b = default;
        b.W0.Should().Be(0);
        b.WMid.Should().Be(0);
        b.WLast.Should().Be(0);
        b.LowBit.Should().BeFalse();
        b.HighBit.Should().BeFalse();
    }

    [Fact]
    public void Bits16384_SetFirstWord()
    {
        Bits16384 b = default;
        b.W0 = 0xDEADBEEF_CAFEBABE;
        b.W0.Should().Be(0xDEADBEEF_CAFEBABE);
        b.WMid.Should().Be(0);
        b.WLast.Should().Be(0);
    }

    [Fact]
    public void Bits16384_SetMiddleWord()
    {
        Bits16384 b = default;
        b.WMid = 0x1234567890ABCDEF;
        b.WMid.Should().Be(0x1234567890ABCDEF);
        b.W0.Should().Be(0);
        b.WLast.Should().Be(0);
    }

    [Fact]
    public void Bits16384_SetLastWord()
    {
        Bits16384 b = default;
        b.WLast = 0xFFFFFFFFFFFFFFFF;
        b.WLast.Should().Be(0xFFFFFFFFFFFFFFFF);
        b.W0.Should().Be(0);
        b.WMid.Should().Be(0);
    }

    [Fact]
    public void Bits16384_LowBit_And_HighBit()
    {
        Bits16384 b = default;
        b.LowBit = true;
        b.HighBit = true;
        b.LowBit.Should().BeTrue();
        b.HighBit.Should().BeTrue();
        // LowBit is bit 0 in W0, HighBit is bit 16383 in WLast
        // Setting LowBit should not affect WLast, and vice versa
        b.WMid.Should().Be(0);
    }

    [Fact]
    public void Bits16384_AllFieldsIndependent()
    {
        Bits16384 b = default;
        b.W0 = 0xAAAAAAAAAAAAAAAA;
        b.WMid = 0xBBBBBBBBBBBBBBBB;
        b.WLast = 0xCCCCCCCCCCCCCCCC;
        b.W0.Should().Be(0xAAAAAAAAAAAAAAAA);
        b.WMid.Should().Be(0xBBBBBBBBBBBBBBBB);
        b.WLast.Should().Be(0xCCCCCCCCCCCCCCCC);
    }

    [Fact]
    public void Bits16384_BitwiseComplement()
    {
        Bits16384 a = default;
        a.W0 = 0;
        a.WLast = 0;
        var result = ~a;
        result.W0.Should().Be(ulong.MaxValue);
        result.WLast.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void Bits16384_Equality()
    {
        Bits16384 a = default;
        Bits16384 b = default;
        (a == b).Should().BeTrue();

        a.WMid = 1;
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Bits16384_SpanRoundTrip()
    {
        Bits16384 original = default;
        original.W0 = 0xDEADBEEF_CAFEBABE;
        original.WMid = 0x1234567890ABCDEF;
        original.WLast = 0xFFFFFFFFFFFFFFFF;

        var bytes = original.ToByteArray();
        bytes.Length.Should().Be(2048);

        var restored = new Bits16384((ReadOnlySpan<byte>)bytes);
        restored.W0.Should().Be(original.W0);
        restored.WMid.Should().Be(original.WMid);
        restored.WLast.Should().Be(original.WLast);
    }

    [Fact]
    public void Bits16384_JsonRoundTrip()
    {
        Bits16384 original = default;
        original.W0 = 0xDEADBEEF;
        original.WLast = 0xCAFEBABE;

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Bits16384>(json);
        restored.W0.Should().Be(original.W0);
        restored.WLast.Should().Be(original.WLast);
    }

    [Fact]
    public void Bits16384_LeftShift_ToLastWord()
    {
        Bits16384 a = 1UL;
        // Shift bit 0 to bit 16320 (start of last word)
        var result = a << 16320;
        result.W0.Should().Be(0);
        result.WLast.Should().Be(1);
    }

    [Fact]
    public void Bits16384_Addition_Simple()
    {
        Bits16384 a = 100UL;
        var result = a + 200UL;
        result.W0.Should().Be(300);
    }

    #endregion
}
