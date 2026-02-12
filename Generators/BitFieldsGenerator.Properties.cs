using System.Text;
using System;

namespace Stardust.Generators;

public partial class BitFieldsGenerator
{
    /// <summary>
    /// Generates a BitField property with inline constants for maximum performance.
    /// For signed property types (sbyte, short, int, long), sign extension is performed
    /// when the field width is smaller than the property type width.
    /// </summary>
    private static void GenerateBitFieldProperty(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;
        int endBit = shift + width - 1;
        
        // Calculate masks as compile-time constants
        ulong mask = (1UL << width) - 1;
        ulong shiftedMask = mask << shift;
        ulong invertedShiftedMask = ~shiftedMask;

        // For signed types, use unsigned type for mask operations to avoid sign extension issues
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        
        // Format masks - always use unsigned representation for masks
        string maskHex = FormatHex(mask, maskType);
        string shiftedMaskHex = FormatHex(shiftedMask, maskType);
        string invertedMaskHex = FormatHex(invertedShiftedMask, maskType);

        // Check if the property type is signed and needs sign extension
        bool propertyTypeIsSigned = IsSignedType(field.PropertyType);
        int propertyTypeBitWidth = GetTypeBitWidth(field.PropertyType);
        bool needsSignExtension = propertyTypeIsSigned && width < propertyTypeBitWidth;

        sb.AppendLine($"{indent}public partial {field.PropertyType} {field.Name}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Getter: For signed property types with field width < type width, perform sign extension
        if (needsSignExtension)
        {
            // Optimized sign extension: mask in place, then shift left/right for sign extension.
            // This is more efficient than extract-then-extend (saves one shift operation).
            // Formula: ((signExtendType)(Value & shiftedMask) << leftShift) >> rightShift
            // - leftShift positions the field's MSB at the sign bit of the intermediate type
            // - rightShift propagates the sign and moves the result to bits [0, width-1]
            string signExtendType = GetSignExtendIntermediateType(field.PropertyType);
            int intermediateTypeBitWidth = signExtendType == "long" ? 64 : 32;
            int leftShift = intermediateTypeBitWidth - 1 - endBit;  // Put field MSB at bit 31 (or 63)
            int rightShift = intermediateTypeBitWidth - width;       // Propagate sign to result width
            
            if (info.StorageTypeIsSigned)
            {
                // For signed storage, cast to unsigned first to avoid unwanted sign extension
                sb.AppendLine($"{indent}    get => ({field.PropertyType})((({signExtendType})((({info.UnsignedStorageType})Value) & {shiftedMaskHex}) << {leftShift}) >> {rightShift});");
            }
            else
            {
                sb.AppendLine($"{indent}    get => ({field.PropertyType})((({signExtendType})(Value & {shiftedMaskHex}) << {leftShift}) >> {rightShift});");
            }
        }
        else
        {
            // No sign extension needed - original behavior
            if (info.StorageTypeIsSigned)
            {
                if (shift == 0)
                {
                    sb.AppendLine($"{indent}    get => ({field.PropertyType})((({info.UnsignedStorageType})Value) & {maskHex});");
                }
                else
                {
                    sb.AppendLine($"{indent}    get => ({field.PropertyType})(((({info.UnsignedStorageType})Value) >> {shift}) & {maskHex});");
                }
            }
            else
            {
                if (shift == 0)
                {
                    sb.AppendLine($"{indent}    get => ({field.PropertyType})(Value & {maskHex});");
                }
                else
                {
                    sb.AppendLine($"{indent}    get => ({field.PropertyType})((Value >> {shift}) & {maskHex});");
                }
            }
        }

        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        // Setter: For signed types, do operations in unsigned space then cast back
        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | ((({info.UnsignedStorageType})value) & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (((({info.UnsignedStorageType})value) << {shift}) & {shiftedMaskHex}));");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | ((({info.StorageType})value) & {shiftedMaskHex}));");
            }
            else
            {
                sb.AppendLine($"{indent}    set => Value = ({info.StorageType})((Value & {invertedMaskHex}) | (((({info.StorageType})value) << {shift}) & {shiftedMaskHex}));");
            }
        }
        
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a BitFlag property with inline constants for maximum performance.
    /// </summary>
    private static void GenerateBitFlagProperty(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        
        ulong mask = 1UL << bit;
        ulong invertedMask = ~mask;

        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;

        string maskHex = FormatHex(mask, maskType);
        string invertedMaskHex = FormatHex(invertedMask, maskType);

        sb.AppendLine($"{indent}public partial bool {flag.Name}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        
        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"{indent}    get => ((({info.UnsignedStorageType})Value) & {maskHex}) != 0;");
            sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}    set => Value = value ? ({info.StorageType})((({info.UnsignedStorageType})Value) | {maskHex}) : ({info.StorageType})((({info.UnsignedStorageType})Value) & {invertedMaskHex});");
        }
        else
        {
            sb.AppendLine($"{indent}    get => (Value & {maskHex}) != 0;");
            sb.AppendLine($"{indent}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"{indent}    set => Value = value ? ({info.StorageType})(Value | {maskHex}) : ({info.StorageType})(Value & {invertedMaskHex});");
        }
        
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a static property that returns a struct with only the specified bit set.
    /// </summary>
    private static void GenerateStaticBitProperty(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        ulong mask = 1UL << bit;
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);

        string castExpr = info.StorageTypeIsSigned 
            ? $"unchecked(({info.StorageType}){maskHex})"
            : $"({info.StorageType}){maskHex}";

        sb.AppendLine($"{indent}/// <summary>Returns a {info.TypeName} with only the {flag.Name} bit set.</summary>");
        sb.AppendLine($"{indent}public static {info.TypeName} {flag.Name}Bit => new({castExpr});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a static property that returns a struct with the mask for the specified field.
    /// </summary>
    private static void GenerateStaticMaskProperty(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;
        ulong mask = ((1UL << width) - 1) << shift;
        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);

        string castExpr = info.StorageTypeIsSigned 
            ? $"unchecked(({info.StorageType}){maskHex})"
            : $"({info.StorageType}){maskHex}";

        sb.AppendLine($"{indent}/// <summary>Returns a {info.TypeName} with the mask for the {field.Name} field (bits {shift}-{shift + width - 1}).</summary>");
        sb.AppendLine($"{indent}public static {info.TypeName} {field.Name}Mask => new({castExpr});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a With{Name} method for a BitFlag that returns a new struct with the flag set/cleared.
    /// </summary>
    private static void GenerateWithBitFlagMethod(StringBuilder sb, BitFieldsInfo info, BitFlagInfo flag, string indent)
    {
        int bit = flag.Bit;
        ulong mask = 1UL << bit;
        ulong invertedMask = ~mask;

        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string maskHex = FormatHex(mask, maskType);
        string invertedMaskHex = FormatHex(invertedMask, maskType);

        sb.AppendLine($"{indent}/// <summary>Returns a new {info.TypeName} with the {flag.Name} flag set to the specified value.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (info.StorageTypeIsSigned)
        {
            sb.AppendLine($"{indent}public {info.TypeName} With{flag.Name}(bool value) => new(value ? ({info.StorageType})((({info.UnsignedStorageType})Value) | {maskHex}) : ({info.StorageType})((({info.UnsignedStorageType})Value) & {invertedMaskHex}));");
        }
        else
        {
            sb.AppendLine($"{indent}public {info.TypeName} With{flag.Name}(bool value) => new(value ? ({info.StorageType})(Value | {maskHex}) : ({info.StorageType})(Value & {invertedMaskHex}));");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a With{Name} method for a BitField that returns a new struct with the field set to a value.
    /// </summary>
    private static void GenerateWithBitFieldMethod(StringBuilder sb, BitFieldsInfo info, BitFieldInfo field, string indent)
    {
        int shift = field.Shift;
        int width = field.Width;

        ulong mask = (1UL << width) - 1;
        ulong shiftedMask = mask << shift;
        ulong invertedShiftedMask = ~shiftedMask;

        string maskType = info.StorageTypeIsSigned ? info.UnsignedStorageType : info.StorageType;
        string shiftedMaskHex = FormatHex(shiftedMask, maskType);
        string invertedMaskHex = FormatHex(invertedShiftedMask, maskType);

        sb.AppendLine($"{indent}/// <summary>Returns a new {info.TypeName} with the {field.Name} field set to the specified value.</summary>");
        sb.AppendLine($"{indent}[MethodImpl(MethodImplOptions.AggressiveInlining)]");

        if (info.StorageTypeIsSigned)
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (({info.UnsignedStorageType})value & {shiftedMaskHex})));");
            }
            else
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})(((({info.UnsignedStorageType})Value) & {invertedMaskHex}) | (((({info.UnsignedStorageType})value) << {shift}) & {shiftedMaskHex})));");
            }
        }
        else
        {
            if (shift == 0)
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})((Value & {invertedMaskHex}) | (value & {shiftedMaskHex})));");
            }
            else
            {
                sb.AppendLine($"{indent}public {info.TypeName} With{field.Name}({field.PropertyType} value) => new(({info.StorageType})((Value & {invertedMaskHex}) | ((({info.StorageType})value << {shift}) & {shiftedMaskHex})));");
            }
        }
        sb.AppendLine();
    }
}
