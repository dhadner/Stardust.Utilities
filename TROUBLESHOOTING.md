# Stardust.Utilities Troubleshooting

This is the full diagnostic reference for Stardust.Utilities. The [README](README.md) covers the two or three most common gotchas; everything else lives here.

## Table of Contents

- [Installation and Build](#installation-and-build)
  - ["Partial property must have an implementation" errors](#partial-property-must-have-an-implementation-errors)
  - [Generated code not updating](#generated-code-not-updating)
  - [IntelliSense not working for generated members](#intellisense-not-working-for-generated-members)
- [Compiler Diagnostics](#compiler-diagnostics)
  - [Unsupported storage type (SD0003)](#unsupported-storage-type-sd0003)
  - [Missing `partial` keyword on properties (SD0004)](#missing-partial-keyword-on-properties-sd0004)
  - [`nint`/`nuint` platform warnings (SD0001, SD0002)](#nintnuint-platform-warnings-sd0001-sd0002)
  - [BitField syntax diagnostics (SD0015-SD0023)](#bitfield-syntax-diagnostics-sd0015-sd0023)
- [Runtime Behavior](#runtime-behavior)
  - [Generated private members renamed with `__` prefix (v0.9.9)](#generated-private-members-renamed-with-__-prefix-v099)
- [Demo and Browser](#demo-and-browser)
  - [DemoWeb shows a welcome page in Edge](#demoweb-shows-a-welcome-page-in-edge)
- [Viewing and Saving Generated Code](#viewing-and-saving-generated-code)

---

## Installation and Build

### "Partial property must have an implementation" errors

**Problem:** Compiler errors like `CS9248: Partial property 'MyStruct.MyProperty' must have an implementation part`.

**Cause:** Either the source generator isn't running, or your properties are missing the `partial` keyword. If you see error **SD0004** alongside CS9248, the fix is to add `partial` to your `[BitField]` / `[BitFlag]` property declarations (see [Missing `partial` keyword on properties (SD0004)](#missing-partial-keyword-on-properties-sd0004)).

**Solution:**

1. Ensure you have the NuGet package installed:
   ```xml
   <PackageReference Include="Stardust.Utilities" Version="0.9.9" />
   ```
2. Add the `partial` keyword to all `[BitField]` and `[BitFlag]` properties.
3. Clean and rebuild the solution.
4. Restart Visual Studio if needed (sometimes required after first install).

### Generated code not updating

**Problem:** You changed your `[BitFields]` struct but the generated code wasn't updated.

**Solution:**

1. Ensure you're using `partial struct` or `partial record struct` (not `class`).
2. Check that attributes are spelled correctly: `[BitFields]`, `[BitField]`, `[BitFlag]`.
3. Clean and rebuild the solution.

### IntelliSense not working for generated members

**Problem:** Visual Studio doesn't show IntelliSense for generated properties or methods.

**Solution:**

1. Build the project at least once (source generators run during build).
2. If still not working, close and reopen the solution.
3. Check **View → Error List** for any generator errors.
4. Ensure your Visual Studio is up to date (VS 2022 17.0+ required for incremental generators).

---

## Compiler Diagnostics

### Unsupported storage type (SD0003)

If you use `[BitFields(typeof(T))]` with a type that is not in the supported list (for example, `typeof(Guid)` or `typeof(string)`), the source generator emits error **SD0003** identifying the unsupported type and listing all valid alternatives. This replaces the confusing `CS9248` ("partial property must have an implementation part") that previously appeared when the generator silently skipped the struct.

The `StorageType` enum constructor avoids this problem entirely -- IntelliSense shows only the supported values, so an invalid choice cannot be written in the first place.

### Missing `partial` keyword on properties (SD0004)

If a property decorated with `[BitField]` or `[BitFlag]` is not declared `partial`, the source generator emits error **SD0004** pointing directly at the property in your source file. The error message includes the property name, attribute, and the corrected declaration so you can fix it immediately.

Without this diagnostic, the compiler would instead produce confusing `CS9248` ("partial property must have an implementation part") or `CS0102` ("type already contains a definition") errors from the generated `.g.cs` file -- neither of which mentions the missing `partial` keyword or points to your source file.

**Example of the problem:**

```csharp
[BitFields(StorageType.Byte)]
public partial struct StatusRegister
{
    // Missing 'partial' -- produces SD0004 error
    [BitFlag(0)] public bool Ready { get; set; }
}
```

**Fix:** Add the `partial` keyword to every `[BitField]` and `[BitFlag]` property:

```csharp
[BitFields(StorageType.Byte)]
public partial struct StatusRegister
{
    [BitFlag(0)] public partial bool Ready { get; set; }  // correct
}
```

This applies to both value-type structs and record struct views.

### `nint`/`nuint` platform warnings (SD0001, SD0002)

`nint` and `nuint` are platform-dependent types: 32 bits on a 32-bit process, 64 bits on a 64-bit process. The source generator emits diagnostics when a `[BitFields]` struct backed by `nint` or `nuint` contains fields or flags that access bits above bit 31:

| Diagnostic | Severity | Condition | Meaning |
|------------|----------|-----------|---------|
| **SD0001** | Error | `PlatformTarget` is `x86` | Bits 32+ are unreachable on a 32-bit-only build. The struct is broken and will corrupt data. |
| **SD0002** | Warning | `PlatformTarget` is `AnyCPU` or unset | Bits 32+ work on 64-bit but are silently unreachable on 32-bit. The binary may run on either. |

No diagnostic is emitted when `PlatformTarget` is `x64` or `ARM64` (always 64-bit).

To resolve these diagnostics:

- **Move fields to bits 0-31** if you need 32-bit compatibility.
- **Change the storage type to `ulong`/`long`** for a fixed 64-bit width on all platforms.
- **Set `<PlatformTarget>x64</PlatformTarget>`** in your `.csproj` if you only target 64-bit.
- **Suppress SD0002** with `#pragma warning disable SD0002` if you have verified the binary will only run on 64-bit.

### BitField syntax diagnostics (SD0015-SD0023)

The source generator validates `[BitField]` attribute usage and emits diagnostics when the syntax is ambiguous, redundant, or incomplete:

| Diagnostic | Severity | Condition | Meaning |
|------------|----------|-----------|---------|
| **SD0015** | Info | `[BitField(start, end)]` two-parameter constructor used | The positional `end` parameter is easily confused with a bit width. Use `[BitField(start, End = N)]` or `[BitField(start, Width = N)]` for clarity. |
| **SD0016** | Warning | Both `End` and `Width` are specified and consistent | Redundant -- remove one for conciseness. |
| **SD0017** | Error | Both `End` and `Width` are specified but inconsistent | The generator cannot determine intent. Remove one or correct the values. |
| **SD0018** | Error | `Start` is specified but neither `End` nor `Width` | The field range is incomplete. Add `End = N` or `Width = N` or use the positional argument `end:`. |
| **SD0019** | Error | `End` or `Width` is specified but `Start` is missing | Specify `Start` explicitly, or provide both `End` and `Width` to let the generator derive `Start = End - Width + 1`. |
| **SD0020** | Error | Floating-point/decimal property type width mismatch | `Half` requires 16, `float` 32, `double` 64, `decimal` 128 bits. A mismatched width could silently corrupt the value. |
| **SD0021** | Error | Embedded `[BitFields(N)]` struct width mismatch | The field width must exactly match the embedded type's declared N bits to avoid silent data truncation. |
| **SD0022** | Error | Record struct (view) used as property in value-type struct | Views are backed by `Memory<byte>` and cannot be stored in an integer field. Use a value-type struct. |
| **SD0023** | Error | Multi-word type in single-word parent | A multi-word type (UInt128, Int128, decimal, `[BitFields(N)]` N > 64) cannot fit in a single-word (<=64-bit) parent. Use a multi-word parent or a view. |

**SD0015** is a learning aid: it reminds developers that the second positional parameter is an inclusive *end bit*, not a *width*. Once you are comfortable with the convention you can suppress it globally without touching `GlobalSuppressions.cs`.

**Option 1 -- `.editorconfig` (recommended):**

Add to a `.editorconfig` file at the project or solution root:

```ini
[*.cs]
dotnet_diagnostic.SD0015.severity = none
```

This is the most idiomatic .NET approach. The setting is version-controlled, scoped to the directory tree, and can also be set to `suggestion` (informational) or `silent` instead of `none`.

**Option 2 -- `<NoWarn>` in `.csproj`:**

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);SD0015</NoWarn>
</PropertyGroup>
```

This suppresses SD0015 for the entire project in one line.

---

## Runtime Behavior

### Generated private members renamed with `__` prefix (v0.9.9)

In v0.9.9, all generated private fields and constants were prefixed with `__` to avoid naming collisions with user-declared members. The public API (`SIZE_IN_BYTES`, `Zero`, `Fields`, operators, conversions, `With` methods, etc.) is unchanged. Only code that accessed private members via reflection or `Unsafe` is affected.

For the complete list of reserved names, see [Generated API Surface](BITFIELDS.md#generated-api-surface) in BITFIELDS.md.

---

## Demo and Browser

### DemoWeb shows a welcome page in Edge

Edge's *Enhanced Security Mode* (Strict) disables WebAssembly for large modules, which crashes the .NET WASM runtime. To prevent this crash from being the first thing a visitor sees, Edge users are shown a welcome page with a "Load Interactive Demo" button on their first visit. Once the demo loads successfully, subsequent visits auto-load normally. If the demo cannot load, the welcome page also links to a video walkthrough and screenshots. To fix the underlying issue, add the site to Edge's exception list at `edge://settings/privacy/security/secureModeSites`. This only affects Edge with Enhanced Security set to *Strict*. The default *Balanced* mode and all other browsers are not affected.

---

## Viewing and Saving Generated Code

The easiest way to view generated code is to use **Go to Definition**:

1. In your code, place the cursor on any generated type (e.g., `StatusRegister`) or property (e.g., `.Ready`, `.Mode`).
2. Press **F12** (or right-click → **Go to Definition**).
3. Visual Studio opens the generated `.g.cs` file with full IntelliSense support.

This works because VS maintains the generated code in memory during compilation. From the opened file you can:

- View all generated operators, properties, and methods.
- Use **Find All References** (Shift+F12) to see all usages.
- Navigate to other generated members.

**Note:** The file opens from a temporary location -- this is normal and expected.

### Saving generated code to disk

If you need to persist generated files to disk (for source control, code review, or CI inspection), add this to your `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<!-- Exclude persisted files from compilation (the generator already compiles them) -->
<ItemGroup>
  <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

This creates a `Generated/` folder with all `.g.cs` files.

**Important limitations:**

- These files are **reference copies only** -- not for interactive development.
- The files will **not appear in Solution Explorer** (they're excluded from the project).
- IntelliSense and "Find All References" **do not work** from these disk files.
- For interactive development, use **F12 (Go to Definition)** instead.

To view the files, open them directly from File Explorer or use **File → Open → File** in Visual Studio.

If you want to be able to open these files directly from the Visual Studio Solution Explorer, you can add them with their Build Action set to `None` by adding this to your `.csproj`:

```xml
<ItemGroup>
  <!--
       Exclude persisted generated files from compilation (they're already compiled by the generator)
       but make them visible in Solution Explorer as non-compiled files.

       NOTE: Visual Studio does not support full IntelliSense for these files - only references within
       the file itself. Use the Shift+F12 "Find all references" method above to find usage across
       all your projects.
  -->
  <None Include="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```
