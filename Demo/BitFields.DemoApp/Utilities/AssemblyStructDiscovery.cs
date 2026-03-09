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
    internal sealed record DiscoveredStruct(string DisplayName, Type BitType);

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
                var fields = type.GetBitFieldInfoFromAttributes();
                if (fields.Length == 0)
                    continue;

                string structDisplayName = FormatDisplayName(type);
                structs.Add(new DiscoveredStruct(structDisplayName, type));
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
            return [.. loaded];
        }
    }

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
            // Prefer the default context for assemblies already loaded (framework,
            // Stardust.Utilities, etc.).  This avoids loading a second copy into the
            // collectible ALC, which would make its BitFieldInfo a different type and
            // break Delegate.CreateDelegate in Extensions.GetFieldInfo.
            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch { }

            // Fall back to probing the assembly's directory for app-specific dependencies.
            // Read bytes and load from stream to avoid locking the file.
            string path = Path.Combine(assemblyDir, assemblyName.Name + ".dll");
            if (File.Exists(path))
                return LoadFromStream(new MemoryStream(File.ReadAllBytes(path)));

            return null;
        }
    }
}
