using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Test;

#region Test Types

/// <summary>
/// Simple breakpoint class for testing (reference type payload).
/// </summary>
public class Breakpoint
{
    public uint Address { get; }
    public int HitCount { get; }

    public Breakpoint(uint address, int hitCount)
    {
        Address = address;
        HitCount = hitCount;
    }
}

/// <summary>
/// Test enhanced enum with various payload types (struct-based, zero allocation).
/// </summary>
[EnhancedEnum]
public partial struct TestCommand
{
    [EnumKind]
    public enum Kind
    {
        [EnumValue(typeof((uint, int)))]
        SetValue,

        [EnumValue(typeof(Breakpoint))]
        SetBreakpoint,

        [EnumValue(typeof(string))]
        Evaluate,

        [EnumValue]
        Step,

        [EnumValue]
        Continue,
    }
}

#endregion

/// <summary>
/// Tests for the EnhancedEnum source generator (struct-based).
/// </summary>
public class GeneratedEnhancedEnumTests
{
    /// <summary>
    /// Tests that factory methods create correct variants.
    /// </summary>
    [Fact]
    public void FactoryMethods_CreateCorrectVariants()
    {
        var setValue = TestCommand.SetValue((0x1000u, 42));
        var step = TestCommand.Step();
        var cont = TestCommand.Continue();
        var bp = TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5));
        var eval = TestCommand.Evaluate("PC + 4");

        setValue.Tag.Should().Be(TestCommand.Kind.SetValue);
        step.Tag.Should().Be(TestCommand.Kind.Step);
        cont.Tag.Should().Be(TestCommand.Kind.Continue);
        bp.Tag.Should().Be(TestCommand.Kind.SetBreakpoint);
        eval.Tag.Should().Be(TestCommand.Kind.Evaluate);
    }

    /// <summary>
    /// Tests that payloads are correctly stored and retrievable via TryGet.
    /// </summary>
    [Fact]
    public void Payloads_AreCorrectlyStored()
    {
        var setValue = TestCommand.SetValue((0x1000u, 42));
        var bp = TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5));
        var eval = TestCommand.Evaluate("PC + 4");

        setValue.TryGetSetValue(out var tuple).Should().BeTrue();
        tuple.Should().Be((0x1000u, 42));

        bp.TryGetSetBreakpoint(out var breakpoint).Should().BeTrue();
        breakpoint.Address.Should().Be(0x2000u);
        breakpoint.HitCount.Should().Be(5);

        eval.TryGetEvaluate(out var expr).Should().BeTrue();
        expr.Should().Be("PC + 4");
    }

    /// <summary>
    /// Tests pattern matching with Match method.
    /// </summary>
    [Fact]
    public void Match_WorksForAllVariants()
    {
        TestCommand[] commands =
        [
            TestCommand.SetValue((0x1000u, 42)),
            TestCommand.Step(),
            TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5)),
            TestCommand.Evaluate("PC + 4"),
            TestCommand.Continue(),
        ];

        var results = commands.Select(cmd => cmd.Match(
            SetValue: tuple => $"SetValue: {tuple.Item1:X}, {tuple.Item2}",
            SetBreakpoint: breakpoint => $"Breakpoint: {breakpoint.Address:X}, hits={breakpoint.HitCount}",
            Evaluate: expr => $"Evaluate: {expr}",
            Step: () => "Step",
            @Continue: () => "Continue"
        )).ToArray();

        results[0].Should().Be("SetValue: 1000, 42");
        results[1].Should().Be("Step");
        results[2].Should().Be("Breakpoint: 2000, hits=5");
        results[3].Should().Be("Evaluate: PC + 4");
        results[4].Should().Be("Continue");
    }

    /// <summary>
    /// Tests the Is properties for variant checking.
    /// </summary>
    [Fact]
    public void IsProperties_CorrectlyIdentifyVariants()
    {
        var setValue = TestCommand.SetValue((0x1000u, 42));
        var step = TestCommand.Step();

        setValue.IsSetValue.Should().BeTrue();
        setValue.IsStep.Should().BeFalse();
        setValue.IsContinue.Should().BeFalse();

        step.IsStep.Should().BeTrue();
        step.IsSetValue.Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryGet returns false for wrong variants.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsFalseForWrongVariant()
    {
        var step = TestCommand.Step();

        step.TryGetSetValue(out _).Should().BeFalse();
        step.TryGetEvaluate(out _).Should().BeFalse();
        step.TryGetSetBreakpoint(out _).Should().BeFalse();
    }

    /// <summary>
    /// Tests void Match overload.
    /// </summary>
    [Fact]
    public void Match_VoidOverload_Works()
    {
        var cmd = TestCommand.SetValue((100u, 200));
        string? result = null;

        cmd.Match(
            SetValue: v => result = $"SetValue: {v}",
            SetBreakpoint: _ => result = "SetBreakpoint",
            Evaluate: _ => result = "Evaluate",
            Step: () => result = "Step",
            @Continue: () => result = "Continue"
        );

        result.Should().Be("SetValue: (100, 200)");
    }

    /// <summary>
    /// Tests struct equality for variants.
    /// </summary>
    [Fact]
    public void Equality_WorksForVariants()
    {
        var step1 = TestCommand.Step();
        var step2 = TestCommand.Step();
        var cont = TestCommand.Continue();

        // Unit variants should be equal
        step1.Should().Be(step2);
        (step1 == step2).Should().BeTrue();
        step1.Should().NotBe(cont);
        (step1 != cont).Should().BeTrue();

        // Payload variants with same values should be equal
        var sv1 = TestCommand.SetValue((100u, 200));
        var sv2 = TestCommand.SetValue((100u, 200));
        var sv3 = TestCommand.SetValue((100u, 300));

        sv1.Should().Be(sv2);
        (sv1 == sv2).Should().BeTrue();
        sv1.Should().NotBe(sv3);
    }

    /// <summary>
    /// Tests that the struct is zero-allocation (value type).
    /// </summary>
    [Fact]
    public void Struct_IsValueType()
    {
        typeof(TestCommand).IsValueType.Should().BeTrue();
    }
}
