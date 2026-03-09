namespace Stardust.Utilities;

/// <summary>
/// IEEE 754 half-precision (16-bit) floating-point decomposition.
/// <code>
/// Bit:  15 | 14-10 | 9-0
///       S  | Exp   | Mantissa
///       1  | 5 bits| 10 bits
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The stored exponent is biased by 15. Use <see cref="BiasedExponent"/> to read
/// the raw stored value, or <see cref="Exponent"/> to get the true mathematical
/// exponent with the bias removed. The implicit leading 1 of the mantissa is not stored.
/// </para>
/// <para>
/// Implicit conversions to/from <see cref="Half"/> allow seamless use:
/// <code>
/// IEEE754Half h = (Half)1.5;
/// h.Sign;            // false
/// h.BiasedExponent;  // 15 (raw stored value)
/// h.Exponent;        // 0  (true power: 2^0)
/// h.Mantissa;        // 0x200
/// Half value = h;
/// </code>
/// </para>
/// </remarks>
[BitFields(StorageType.Half, Description = "IEEE 754 Half-Precision (16-bit)")]
public partial struct IEEE754Half
{
    /// <summary>10-bit significand (mantissa). The implicit leading 1 is not stored.</summary>
    [BitField(0, 9, Description = "10-bit significand (fractional part); implicit leading 1 not stored")] public partial ushort Mantissa { get; set; }

    /// <summary>
    /// 5-bit biased exponent as stored in the IEEE 754 encoding (bias = 15).
    /// For the true mathematical exponent, use <see cref="Exponent"/> instead.
    /// </summary>
    [BitField(10, 14, Description = "5-bit biased exponent (bias 15); subtract 15 for true power of 2")] public partial byte BiasedExponent { get; set; }

    /// <summary>Sign bit. <c>true</c> = negative, <c>false</c> = positive.</summary>
    [BitFlag(15, Description = "Sign bit: 1 = negative, 0 = positive")] public partial bool Sign { get; set; }

    // ── Constants ───────────────────────────────────────────────

    /// <summary>Exponent bias for IEEE 754 half-precision (15).</summary>
    public const int EXPONENT_BIAS = 15;

    /// <summary>Maximum biased exponent value (all 5 bits set).</summary>
    public const int MAX_BIASED_EXPONENT = 0x1F;

    // ── Classification properties ───────────────────────────────

    /// <summary><c>true</c> if this value is NaN (exponent = all 1s, mantissa != 0).</summary>
    public bool IsNaN => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa != 0;

    /// <summary><c>true</c> if this value is +/- infinity (exponent = all 1s, mantissa = 0).</summary>
    public bool IsInfinity => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa == 0;

    /// <summary><c>true</c> if this value is a denormalized (subnormal) number (exponent = 0, mantissa != 0).</summary>
    public bool IsDenormalized => BiasedExponent == 0 && Mantissa != 0;

    /// <summary><c>true</c> if this value is a normalized number (0 &lt; exponent &lt; 31).</summary>
    public bool IsNormal => BiasedExponent > 0 && BiasedExponent < MAX_BIASED_EXPONENT;

    /// <summary><c>true</c> if this value is +/- zero (exponent = 0, mantissa = 0).</summary>
    public bool IsZero => BiasedExponent == 0 && Mantissa == 0;

    /// <summary>
    /// The true mathematical exponent (<see cref="BiasedExponent"/> - 15), or <c>null</c>
    /// for non-normal values (zero, denormalized, infinity, NaN).
    /// Setting to <c>null</c> sets <see cref="BiasedExponent"/> to 0;
    /// otherwise the bias is added automatically.
    /// </summary>
    /// <example>
    /// <code>
    /// IEEE754Half h = (Half)4.0;
    /// h.BiasedExponent;  // 17 (raw stored value)
    /// h.Exponent;        // 2  (true power: 4.0 = 2^2)
    /// </code>
    /// </example>
    public int? Exponent
    {
        get => IsNormal ? BiasedExponent - EXPONENT_BIAS : null;
        set => BiasedExponent = value is { } v ? (byte)(v + EXPONENT_BIAS) : (byte)0;
    }

    // ── Computed exponent constants ──────────────────────────────

    /// <summary>Minimum true exponent for a normal half-precision value (-14).</summary>
    public const int MIN_EXPONENT = 1 - EXPONENT_BIAS;

    /// <summary>Maximum true exponent for a normal half-precision value (15).</summary>
    public const int MAX_EXPONENT = MAX_BIASED_EXPONENT - 1 - EXPONENT_BIAS;

    // ── Fluent exponent setter ───────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="IEEE754Half"/> with the <see cref="BiasedExponent"/> set
    /// from a true mathematical exponent (the bias is added automatically).
    /// Out-of-range values are masked by <see cref="WithBiasedExponent"/>.
    /// </summary>
    /// <param name="exponent">True exponent (bias is added automatically; out-of-range values are masked).</param>
    /// <returns>A copy of this value with the exponent field updated.</returns>
    /// <example>
    /// <code>
    /// // Build 2^3 = 8.0 from parts
    /// var h = IEEE754Half.Zero.WithExponent(3).WithMantissa(0);
    /// h.Sign = false;
    /// Half value = h;  // 8.0
    /// </code>
    /// </example>
    public IEEE754Half WithExponent(int exponent) =>
        WithBiasedExponent((byte)(exponent + EXPONENT_BIAS));
}

/// <summary>
/// IEEE 754 single-precision (32-bit) floating-point decomposition.
/// <code>
/// Bit:  31 | 30-23 | 22-0
///       S  | Exp   | Mantissa
///       1  | 8 bits| 23 bits
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The stored exponent is biased by 127. Use <see cref="BiasedExponent"/> to read
/// the raw stored value, or <see cref="Exponent"/> to get the true mathematical
/// exponent with the bias removed. The implicit leading 1 of the mantissa is not stored.
/// </para>
/// <para>
/// Implicit conversions to/from <see cref="float"/> allow seamless use:
/// <code>
/// IEEE754Single f = 3.14f;
/// f.Sign;            // false
/// f.BiasedExponent;  // 128 (raw stored value)
/// f.Exponent;        // 1   (true power: 2^1, since 2 &lt;= 3.14 &lt; 4)
/// f.Mantissa;        // 0x48F5C3
/// float value = f;
/// </code>
/// </para>
/// </remarks>
[BitFields(StorageType.Single, Description = "IEEE 754 Single-Precision (32-bit)")]
public partial struct IEEE754Single
{
    /// <summary>23-bit significand (mantissa). The implicit leading 1 is not stored.</summary>
    [BitField(0, 22, Description = "23-bit significand (fractional part); implicit leading 1 not stored")] public partial uint Mantissa { get; set; }

    /// <summary>
    /// 8-bit biased exponent as stored in the IEEE 754 encoding (bias = 127).
    /// For the true mathematical exponent, use <see cref="Exponent"/> instead.
    /// </summary>
    [BitField(23, 30, Description = "8-bit biased exponent (bias 127); subtract 127 for true power of 2")] public partial byte BiasedExponent { get; set; }

    /// <summary>Sign bit. <c>true</c> = negative, <c>false</c> = positive.</summary>
    [BitFlag(31, Description = "Sign bit: 1 = negative, 0 = positive")] public partial bool Sign { get; set; }

    // ── Constants ───────────────────────────────────────────────

    /// <summary>Exponent bias for IEEE 754 single-precision (127).</summary>
    public const int EXPONENT_BIAS = 127;

    /// <summary>Maximum biased exponent value (all 8 bits set).</summary>
    public const int MAX_BIASED_EXPONENT = 0xFF;

    // ── Classification properties ───────────────────────────────

    /// <summary><c>true</c> if this value is NaN (exponent = all 1s, mantissa != 0).</summary>
    public bool IsNaN => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa != 0;

    /// <summary><c>true</c> if this value is +/- infinity (exponent = all 1s, mantissa = 0).</summary>
    public bool IsInfinity => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa == 0;

    /// <summary><c>true</c> if this value is a denormalized (subnormal) number (exponent = 0, mantissa != 0).</summary>
    public bool IsDenormalized => BiasedExponent == 0 && Mantissa != 0;

    /// <summary><c>true</c> if this value is a normalized number (0 &lt; exponent &lt; 255).</summary>
    public bool IsNormal => BiasedExponent > 0 && BiasedExponent < MAX_BIASED_EXPONENT;

    /// <summary><c>true</c> if this value is +/- zero (exponent = 0, mantissa = 0).</summary>
    public bool IsZero => BiasedExponent == 0 && Mantissa == 0;

    /// <summary>
    /// The true mathematical exponent (<see cref="BiasedExponent"/> - 127), or <c>null</c>
    /// for non-normal values (zero, denormalized, infinity, NaN).
    /// Setting to <c>null</c> sets <see cref="BiasedExponent"/> to 0;
    /// otherwise the bias is added automatically.
    /// </summary>
    /// <example>
    /// <code>
    /// IEEE754Single f = 8.0f;
    /// f.BiasedExponent;  // 130 (raw stored value)
    /// f.Exponent;        // 3   (true power: 8.0 = 2^3)
    /// </code>
    /// </example>
    public int? Exponent
    {
        get => IsNormal ? BiasedExponent - EXPONENT_BIAS : null;
        set => BiasedExponent = value is { } v ? (byte)(v + EXPONENT_BIAS) : (byte)0;
    }

    // ── Computed exponent constants ──────────────────────────────

    /// <summary>Minimum true exponent for a normal single-precision value (-126).</summary>
    public const int MIN_EXPONENT = 1 - EXPONENT_BIAS;

    /// <summary>Maximum true exponent for a normal single-precision value (127).</summary>
    public const int MAX_EXPONENT = MAX_BIASED_EXPONENT - 1 - EXPONENT_BIAS;

    // ── Fluent exponent setter ───────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="IEEE754Single"/> with the <see cref="BiasedExponent"/> set
    /// from a true mathematical exponent (the bias is added automatically).
    /// Out-of-range values are masked by <see cref="WithBiasedExponent"/>.
    /// </summary>
    /// <param name="exponent">True exponent (bias is added automatically; out-of-range values are masked).</param>
    /// <returns>A copy of this value with the exponent field updated.</returns>
    /// <example>
    /// <code>
    /// // Build 2^3 = 8.0f from parts
    /// var f = IEEE754Single.Zero.WithExponent(3).WithMantissa(0);
    /// f.Sign = false;
    /// float value = f;  // 8.0f
    /// </code>
    /// </example>
    public IEEE754Single WithExponent(int exponent) =>
        WithBiasedExponent((byte)(exponent + EXPONENT_BIAS));
}

/// <summary>
/// IEEE 754 double-precision (64-bit) floating-point decomposition.
/// <code>
/// Bit:  63 | 62-52 | 51-0
///       S  | Exp   | Mantissa
///       1  | 11 bits| 52 bits
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The stored exponent is biased by 1023. Use <see cref="BiasedExponent"/> to read
/// the raw stored value, or <see cref="Exponent"/> to get the true mathematical
/// exponent with the bias removed. The implicit leading 1 of the mantissa is not stored.
/// </para>
/// <para>
/// Implicit conversions to/from <see cref="double"/> allow seamless use:
/// <code>
/// IEEE754Double d = Math.PI;
/// d.Sign;            // false
/// d.BiasedExponent;  // 1024 (raw stored value)
/// d.Exponent;        // 1    (true power: 2^1, since 2 &lt;= pi &lt; 4)
/// d.Mantissa;        // 0x921FB54442D18
/// double value = d;
/// </code>
/// </para>
/// </remarks>
[BitFields(StorageType.Double, Description = "IEEE 754 Double-Precision (64-bit)")]
public partial struct IEEE754Double
{
    /// <summary>52-bit significand (mantissa). The implicit leading 1 is not stored.</summary>
    [BitField(0, 51, Description = "52-bit significand (fractional part); implicit leading 1 not stored")] public partial ulong Mantissa { get; set; }

    /// <summary>
    /// 11-bit biased exponent as stored in the IEEE 754 encoding (bias = 1023).
    /// For the true mathematical exponent, use <see cref="Exponent"/> instead.
    /// </summary>
    [BitField(52, 62, Description = "11-bit biased exponent (bias 1023); subtract 1023 for true power of 2")] public partial ushort BiasedExponent { get; set; }

    /// <summary>Sign bit. <c>true</c> = negative, <c>false</c> = positive.</summary>
    [BitFlag(63, Description = "Sign bit: 1 = negative, 0 = positive")] public partial bool Sign { get; set; }

    // ── Constants ───────────────────────────────────────────────

    /// <summary>Exponent bias for IEEE 754 double-precision (1023).</summary>
    public const int EXPONENT_BIAS = 1023;

    /// <summary>Maximum biased exponent value (all 11 bits set).</summary>
    public const int MAX_BIASED_EXPONENT = 0x7FF;

    // ── Classification properties ───────────────────────────────

    /// <summary><c>true</c> if this value is NaN (exponent = all 1s, mantissa != 0).</summary>
    public bool IsNaN => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa != 0;

    /// <summary><c>true</c> if this value is +/- infinity (exponent = all 1s, mantissa = 0).</summary>
    public bool IsInfinity => BiasedExponent == MAX_BIASED_EXPONENT && Mantissa == 0;

    /// <summary><c>true</c> if this value is a denormalized (subnormal) number (exponent = 0, mantissa != 0).</summary>
    public bool IsDenormalized => BiasedExponent == 0 && Mantissa != 0;

    /// <summary><c>true</c> if this value is a normalized number (0 &lt; exponent &lt; 2047).</summary>
    public bool IsNormal => BiasedExponent > 0 && BiasedExponent < MAX_BIASED_EXPONENT;

    /// <summary><c>true</c> if this value is +/- zero (exponent = 0, mantissa = 0).</summary>
    public bool IsZero => BiasedExponent == 0 && Mantissa == 0;

    /// <summary>
    /// The true mathematical exponent (<see cref="BiasedExponent"/> - 1023), or <c>null</c>
    /// for non-normal values (zero, denormalized, infinity, NaN).
    /// Setting to <c>null</c> sets <see cref="BiasedExponent"/> to 0;
    /// otherwise the bias is added automatically.
    /// </summary>
    /// <example>
    /// <code>
    /// IEEE754Double d = Math.PI;
    /// d.BiasedExponent;  // 1024 (raw stored value)
    /// d.Exponent;        // 1    (true power: pi is in [2, 4), so 2^1)
    /// </code>
    /// </example>
    public int? Exponent
    {
        get => IsNormal ? BiasedExponent - EXPONENT_BIAS : null;
        set => BiasedExponent = value is { } v ? (ushort)(v + EXPONENT_BIAS) : (ushort)0;
    }

    // ── Computed exponent constants ──────────────────────────────

    /// <summary>Minimum true exponent for a normal double-precision value (-1022).</summary>
    public const int MIN_EXPONENT = 1 - EXPONENT_BIAS;

    /// <summary>Maximum true exponent for a normal double-precision value (1023).</summary>
    public const int MAX_EXPONENT = MAX_BIASED_EXPONENT - 1 - EXPONENT_BIAS;

    // ── Fluent exponent setter ───────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="IEEE754Double"/> with the <see cref="BiasedExponent"/> set
    /// from a true mathematical exponent (the bias is added automatically).
    /// Out-of-range values are masked by <see cref="WithBiasedExponent"/>.
    /// </summary>
    /// <param name="exponent">True exponent (bias is added automatically; out-of-range values are masked).</param>
    /// <returns>A copy of this value with the exponent field updated.</returns>
    /// <example>
    /// <code>
    /// // Build 2^3 = 8.0 from parts
    /// var d = IEEE754Double.Zero.WithExponent(3).WithMantissa(0);
    /// d.Sign = false;
    /// double value = d;  // 8.0
    /// </code>
    /// </example>
    public IEEE754Double WithExponent(int exponent) =>
        WithBiasedExponent((ushort)(exponent + EXPONENT_BIAS));
}

/// <summary>
/// .NET <see cref="decimal"/> (128-bit) decomposition into coefficient, scale, and sign.
/// <code>
/// Bits:  127 | 126-119 | 118-112 | 111-96   | 95-0
///        Sign| Reserved| Scale   | Reserved | 96-bit unsigned coefficient
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="decimal"/> value equals <c>(-1)^Sign * Coefficient / 10^Scale</c>.
/// The scale ranges from 0 to 28. The coefficient is a 96-bit unsigned integer.
/// </para>
/// <para>
/// Implicit conversions to/from <see cref="decimal"/> allow seamless use:
/// <code>
/// DecimalBitFields d = 19.99m;
/// d.Sign;          // false
/// d.Scale;         // 2
/// d.Coefficient;   // 1999
/// decimal value = d;
/// </code>
/// </para>
/// </remarks>
[BitFields(StorageType.Decimal, Description = ".NET Decimal (128-bit)")]
public partial struct DecimalBitFields
{
    /// <summary>96-bit unsigned integer coefficient.</summary>
    [BitField(0, 95, Description = "96-bit unsigned integer coefficient (value before scaling)")] public partial UInt128 Coefficient { get; set; }

    /// <summary>Scale factor (0-28). The value is divided by 10^Scale.</summary>
    [BitField(112, 118, Description = "Scale factor (0-28); value = Coefficient / 10^Scale")] public partial byte Scale { get; set; }

    /// <summary>Sign bit. <c>true</c> = negative, <c>false</c> = positive.</summary>
    [BitFlag(127, Description = "Sign bit: 1 = negative, 0 = positive")] public partial bool Sign { get; set; }

    // ── Constants ───────────────────────────────────────────────

    /// <summary>Maximum scale value for a .NET decimal (28).</summary>
    public const int MAX_SCALE = 28;
}
