using FluentAssertions;
using Stardust.Utilities;
using Xunit;

namespace Stardust.Utilities.Test;

#region Test Types

/// <summary>
/// Simple breakpoint class for testing.
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
/// Test enhanced enum with various payload types.
/// </summary>
[EnhancedEnum]
public partial record TestCommand
{
    [EnumKind]
    private enum Kind
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
/// Tests for the EnhancedEnum source generator.
/// </summary>
public class GeneratedEnhancedEnumTests
{
    /// <summary>
    /// Tests that constructors create correct variant types.
    /// </summary>
    [Fact]
    public void Constructors_CreateCorrectVariants()
    {
        var setValue = new TestCommand.SetValue((0x1000u, 42));
        var step = new TestCommand.Step();
        var cont = new TestCommand.Continue();
        var bp = new TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5));
        var eval = new TestCommand.Evaluate("PC + 4");

        setValue.Should().BeOfType<TestCommand.SetValue>();
        step.Should().BeOfType<TestCommand.Step>();
        cont.Should().BeOfType<TestCommand.Continue>();
        bp.Should().BeOfType<TestCommand.SetBreakpoint>();
        eval.Should().BeOfType<TestCommand.Evaluate>();
    }

    /// <summary>
    /// Tests that payloads are correctly stored and retrievable.
    /// </summary>
    [Fact]
    public void Payloads_AreCorrectlyStored()
    {
        var setValue = new TestCommand.SetValue((0x1000u, 42));
        var bp = new TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5));
        var eval = new TestCommand.Evaluate("PC + 4");

        setValue.Value.Should().Be((0x1000u, 42));
        bp.Value.Address.Should().Be(0x2000u);
        bp.Value.HitCount.Should().Be(5);
        eval.Value.Should().Be("PC + 4");
    }

    /// <summary>
    /// Tests pattern matching with switch expression.
    /// </summary>
    [Fact]
    public void PatternMatching_WorksInSwitchExpression()
    {
        TestCommand[] commands =
        [
            new TestCommand.SetValue((0x1000u, 42)),
            new TestCommand.Step(),
            new TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5)),
            new TestCommand.Evaluate("PC + 4"),
            new TestCommand.Continue(),
        ];

        var results = commands.Select(cmd => cmd switch
        {
            TestCommand.SetValue(var tuple) => $"SetValue: {tuple.Item1:X}, {tuple.Item2}",
            TestCommand.SetBreakpoint(var breakpoint) => $"Breakpoint: {breakpoint.Address:X}, hits={breakpoint.HitCount}",
            TestCommand.Evaluate(var expr) => $"Evaluate: {expr}",
            TestCommand.Step => "Step",
            TestCommand.Continue => "Continue",
            _ => "Unknown"
        }).ToArray();

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
        var setValue = new TestCommand.SetValue((0x1000u, 42));
        var step = new TestCommand.Step();

        setValue.IsSetValue.Should().BeTrue();
        setValue.IsStep.Should().BeFalse();
        setValue.IsContinue.Should().BeFalse();

        step.IsStep.Should().BeTrue();
        step.IsSetValue.Should().BeFalse();
    }

    /// <summary>
    /// Tests that all variants inherit from the base type.
    /// </summary>
    [Fact]
    public void AllVariants_InheritFromBaseType()
    {
        TestCommand setValue = new TestCommand.SetValue((0x1000u, 42));
        TestCommand step = new TestCommand.Step();
        TestCommand bp = new TestCommand.SetBreakpoint(new Breakpoint(0x2000, 5));

        // All should be assignable to TestCommand
        setValue.Should().BeAssignableTo<TestCommand>();
        step.Should().BeAssignableTo<TestCommand>();
        bp.Should().BeAssignableTo<TestCommand>();
    }

    /// <summary>
    /// Tests deconstruction for pattern matching.
    /// </summary>
    [Fact]
    public void Deconstruction_WorksForPayloadVariants()
    {
        var setValue = new TestCommand.SetValue((0x1000u, 42));

        // Deconstruct
        setValue.Deconstruct(out var tuple);
        tuple.Item1.Should().Be(0x1000u);
        tuple.Item2.Should().Be(42);
    }

    /// <summary>
    /// Tests using the generated enum in a method that accepts the base type.
    /// </summary>
    [Fact]
    public void PolymorphicUsage_WorksCorrectly()
    {
        static string ProcessCommand(TestCommand cmd)
        {
            return cmd switch
            {
                TestCommand.SetValue sv => $"Set {sv.Value.Item1:X8} = {sv.Value.Item2}",
                TestCommand.SetBreakpoint sbp => $"BP at {sbp.Value.Address:X8}",
                TestCommand.Evaluate e => $"Eval: {e.Value}",
                TestCommand.Step => "Step",
                TestCommand.Continue => "Continue",
                _ => "?"
            };
        }

        ProcessCommand(new TestCommand.SetValue((0xDEADBEEFu, -1)))
            .Should().Be("Set DEADBEEF = -1");

        ProcessCommand(new TestCommand.Step())
            .Should().Be("Step");
    }

    /// <summary>
    /// Tests record equality for variants.
    /// </summary>
    [Fact]
    public void RecordEquality_WorksForVariants()
    {
        var step1 = new TestCommand.Step();
        var step2 = new TestCommand.Step();
        var cont = new TestCommand.Continue();

        // Unit variants should be equal
        step1.Should().Be(step2);
        step1.Should().NotBe(cont);

        // Payload variants with same values should be equal
        var sv1 = new TestCommand.SetValue((100u, 200));
        var sv2 = new TestCommand.SetValue((100u, 200));
        var sv3 = new TestCommand.SetValue((100u, 300));

        sv1.Should().Be(sv2);
        sv1.Should().NotBe(sv3);
    }
}
