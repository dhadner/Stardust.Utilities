using Stardust.Utilities;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace BitFields.DemoApp;

/// <summary>
/// Loads a .NET assembly into a collectible AssemblyLoadContext, discovers all types
/// decorated with [BitFields] or [BitFieldsView], reconstructs BitFieldInfo[] from
/// reflection-only attribute metadata, then unloads the assembly so the file is not locked.
/// </summary>
internal static class AssemblyStructDiscovery
{
    internal sealed record DiscoveredStruct(string DisplayName, BitFieldInfo[] Fields);

    internal sealed record DiscoveryResult(string AssemblyName, List<DiscoveredStruct> Structs, string? Error);

    /// <summary>
    /// Loads the PE file into a collectible ALC, discovers [BitFields]/[BitFieldsView] structs,
    /// then immediately unloads the assembly so the developer can rebuild.
    /// </summary>
    internal static DiscoveryResult Discover(string filePath)
    {
        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(filePath);
        }
        catch (Exception ex)
        {
            return new DiscoveryResult(Path.GetFileName(filePath), [], $"Could not read file: {ex.Message}");
        }

        return Discover(fileBytes, Path.GetFileName(filePath), Path.GetDirectoryName(filePath));
    }

    /// <summary>
    /// Discovers [BitFields]/[BitFieldsView] structs from raw assembly bytes.
    /// Used by Blazor WebAssembly where file paths are not available.
    /// </summary>
    /// <param name="assemblyBytes">The raw bytes of the .NET assembly.</param>
    /// <param name="displayName">A friendly name for error messages (e.g., the file name).</param>
    /// <param name="probingDirectory">Optional directory to resolve dependencies from. Null in WASM.</param>
    internal static DiscoveryResult Discover(byte[] assemblyBytes, string displayName, string? probingDirectory = null)
    {
        // When no probing directory is given (WASM), load directly into the default context.
        // Collectible ALCs are not reliably supported on the Mono/WASM runtime, and there is
        // no file-locking concern since the bytes are already in memory.
        bool useCollectible = probingDirectory != null;
        AssemblyLoadContext? alc = useCollectible
            ? new CollectibleLoadContext(probingDirectory!)
            : null;
        try
        {
            Assembly asm;
            try
            {
                asm = alc != null
                    ? alc.LoadFromStream(new MemoryStream(assemblyBytes))
                    : Assembly.Load(assemblyBytes);
            }
            catch (BadImageFormatException)
            {
                return new DiscoveryResult(displayName, [], "Not a .NET assembly (native PE or incompatible target).");
            }
            catch (FileLoadException ex)
            {
                return new DiscoveryResult(displayName, [], $"Could not load assembly: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new DiscoveryResult(displayName, [], $"Error loading assembly: {ex.Message}");
            }

            return DiscoverFromAssembly(asm, displayName);
        }
        finally
        {
            alc?.Unload();
        }
    }

    /// <summary>
    /// Discovers [BitFields]/[BitFieldsView] structs from a loaded assembly.
    /// </summary>
    private static DiscoveryResult DiscoverFromAssembly(Assembly asm, string displayName)
    {
        var structs = new List<DiscoveredStruct>();
        var types = GetLoadableTypes(asm);

        foreach (var type in types)
        {
            if (!type.IsValueType)
                continue;

            try
            {
                var structInfo = ReadStructAttribute(type);
                if (!structInfo.Found)
                    continue;

                var fields = ReadFieldsFromProperties(type, structInfo.BitOrder, structInfo.ByteOrder, structInfo.TotalBits, structInfo.Description);
                if (fields.Length == 0)
                    continue;

                string structDisplayName = FormatDisplayName(type);
                structs.Add(new DiscoveredStruct(structDisplayName, fields));
            }
            catch
            {
                // Skip types whose attributes can't be resolved (e.g., missing dependencies)
            }
        }

        structs.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));

        string? error = structs.Count == 0 ? "No [BitFields] or [BitFieldsView] structs found." : null;
        string asmName = asm.GetName().Name ?? displayName;
        return new DiscoveryResult(asmName, structs, error);
    }

    // ── Reflection-only attribute reading via CustomAttributeData ──

    private static (BitOrder BitOrder, ByteOrder ByteOrder, int TotalBits, string? Description, bool Found) ReadStructAttribute(Type type)
    {
        foreach (var cad in type.CustomAttributes)
        {
            string attrName = cad.AttributeType.Name;

            if (attrName == "BitFieldsAttribute")
            {
                var bitOrder = BitOrder.BitZeroIsLsb;
                var byteOrder = ByteOrder.LittleEndian;
                int totalBits = 0;
                string? description = null;

                // Constructor: (Type storageType, ...) or (int bitCount, ...)
                if (cad.ConstructorArguments.Count > 0)
                {
                    var first = cad.ConstructorArguments[0];
                    if (first.Value is Type storageType)
                        totalBits = GetBitCountForTypeName(storageType.Name);
                    else if (first.Value is int bitCount)
                        totalBits = bitCount;
                }

                // Optional constructor args: UndefinedBitsMustBe, BitOrder, ByteOrder
                for (int i = 1; i < cad.ConstructorArguments.Count; i++)
                {
                    var arg = cad.ConstructorArguments[i];
                    if (arg.ArgumentType.Name == "BitOrder")
                        bitOrder = (BitOrder)(int)arg.Value!;
                    else if (arg.ArgumentType.Name == "ByteOrder")
                        byteOrder = (ByteOrder)(int)arg.Value!;
                }

                description = GetNamedArgString(cad, "Description");

                return (bitOrder, byteOrder, totalBits, description, true);
            }

            if (attrName == "BitFieldsViewAttribute")
            {
                var bitOrder = BitOrder.BitZeroIsLsb;
                var byteOrder = ByteOrder.LittleEndian;
                string? description = null;

                for (int i = 0; i < cad.ConstructorArguments.Count; i++)
                {
                    var arg = cad.ConstructorArguments[i];
                    if (arg.ArgumentType.Name == "ByteOrder")
                        byteOrder = (ByteOrder)(int)arg.Value!;
                    else if (arg.ArgumentType.Name == "BitOrder")
                        bitOrder = (BitOrder)(int)arg.Value!;
                }

                description = GetNamedArgString(cad, "Description");

                return (bitOrder, byteOrder, 0, description, true);
            }
        }

        return (default, default, 0, null, false);
    }

    private static BitFieldInfo[] ReadFieldsFromProperties(Type type, BitOrder structBitOrder, ByteOrder structByteOrder, int structTotalBits, string? structDescription)
    {
        var fields = new List<BitFieldInfo>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var cad in prop.CustomAttributes)
            {
                string attrName = cad.AttributeType.Name;

                if (attrName == "BitFieldAttribute" && cad.ConstructorArguments.Count >= 2)
                {
                    int startBit = (int)cad.ConstructorArguments[0].Value!;
                    int endBit = (int)cad.ConstructorArguments[1].Value!;
                    int mustBe = cad.ConstructorArguments.Count > 2 ? (int)cad.ConstructorArguments[2].Value! : 0;
                    string? desc = GetNamedArgString(cad, "Description");

                    fields.Add(new BitFieldInfo(
                        Name: prop.Name,
                        StartBit: startBit,
                        BitLength: endBit - startBit + 1,
                        PropertyType: MapPropertyTypeName(prop.PropertyType),
                        IsFlag: false,
                        ByteOrder: structByteOrder,
                        BitOrder: structBitOrder,
                        Description: desc,
                        StructTotalBits: structTotalBits,
                        FieldMustBe: mustBe,
                        StructDescription: structDescription
                    ));
                }
                else if (attrName == "BitFlagAttribute" && cad.ConstructorArguments.Count >= 1)
                {
                    int bit = (int)cad.ConstructorArguments[0].Value!;
                    int mustBe = cad.ConstructorArguments.Count > 1 ? (int)cad.ConstructorArguments[1].Value! : 0;
                    string? desc = GetNamedArgString(cad, "Description");

                    fields.Add(new BitFieldInfo(
                        Name: prop.Name,
                        StartBit: bit,
                        BitLength: 1,
                        PropertyType: "bool",
                        IsFlag: true,
                        ByteOrder: structByteOrder,
                        BitOrder: structBitOrder,
                        Description: desc,
                        StructTotalBits: structTotalBits,
                        FieldMustBe: mustBe,
                        StructDescription: structDescription
                    ));
                }
            }
        }

        // If structTotalBits wasn't set (BitFieldsView), infer from the highest defined bit
        if (structTotalBits == 0 && fields.Count > 0)
        {
            int maxBit = fields.Max(f => f.EndBit);
            int inferred = ((maxBit + 8) / 8) * 8;
            for (int i = 0; i < fields.Count; i++)
                fields[i] = fields[i] with { StructTotalBits = inferred };
        }

        fields.Sort((a, b) => a.StartBit.CompareTo(b.StartBit));
        return fields.ToArray();
    }

    // ── Helpers ──

    /// <summary>
    /// Returns all types that can be loaded from the assembly, gracefully skipping
    /// any that fail due to missing dependencies. On CoreCLR, <c>GetTypes()</c> throws
    /// <see cref="ReflectionTypeLoadException"/> with partial results. On Mono/WASM it
    /// may throw <see cref="FileNotFoundException"/> instead, so we fall back to
    /// enumerating <c>DefinedTypes</c> one by one.
    /// </summary>
    private static Type[] GetLoadableTypes(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).ToArray()!;
        }
        catch
        {
            // Mono/WASM may throw FileNotFoundException or FileLoadException when a
            // referenced assembly is missing. DefinedTypes is an IEnumerable that can
            // be iterated element-by-element, skipping types that fail to resolve.
            var loaded = new List<Type>();
            try
            {
                foreach (var ti in asm.DefinedTypes)
                {
                    try { loaded.Add(ti); }
                    catch { /* type references unresolvable assembly — skip */ }
                }
            }
            catch { /* iteration itself failed — return what we have */ }
            return loaded.ToArray();
        }
    }

    private static string? GetNamedArgString(CustomAttributeData cad, string name)
    {
        foreach (var na in cad.NamedArguments)
        {
            if (na.MemberName == name && na.TypedValue.Value is string s)
                return s;
        }
        return null;
    }

    private static string MapPropertyTypeName(Type type)
    {
        // Match by name (not identity) since the type comes from a different ALC
        return type.Name switch
        {
            "Boolean" => "bool",
            "Byte"    => "byte",
            "SByte"   => "sbyte",
            "UInt16"  => "ushort",
            "Int16"   => "short",
            "UInt32"  => "uint",
            "Int32"   => "int",
            "UInt64"  => "ulong",
            "Int64"   => "long",
            _ when type.IsEnum => MapPropertyTypeName(type.GetEnumUnderlyingType()),
            _ => type.Name.ToLowerInvariant()
        };
    }

    private static int GetBitCountForTypeName(string typeName) => typeName switch
    {
        "Byte" or "SByte"       => 8,
        "UInt16" or "Int16"     => 16,
        "UInt32" or "Int32"     => 32,
        "UInt64" or "Int64"     => 64,
        "Single"                => 32,
        "Double"                => 64,
        "Half"                  => 16,
        "Decimal"               => 128,
        "UInt128" or "Int128"   => 128,
        _                       => 0
    };

    private static string FormatDisplayName(Type type)
    {
        if (type.DeclaringType == null)
            return type.Name;
        return $"{type.DeclaringType.Name}.{type.Name}";
    }

    /// <summary>
    /// A collectible ALC that resolves dependencies from the assembly's directory
    /// and falls back to the default context for framework/shared assemblies.
    /// </summary>
    private sealed class CollectibleLoadContext(string assemblyDir) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Try to resolve from the same directory as the target assembly.
            // Read bytes and load from stream to avoid locking the file.
            string path = Path.Combine(assemblyDir, assemblyName.Name + ".dll");
            if (File.Exists(path))
                return LoadFromStream(new MemoryStream(File.ReadAllBytes(path)));

            // Fall back to the default context (framework assemblies, Stardust.Utilities, etc.)
            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
                return null;
            }
        }
    }
}
