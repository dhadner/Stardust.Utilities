; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.9.6

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SD0001  | Stardust.BitFields | Error | BitField exceeds 32-bit nint/nuint range on 32-bit platform
SD0002  | Stardust.BitFields | Warning | BitField may exceed 32-bit nint/nuint range on AnyCPU build
SD0003  | Stardust.BitFields | Error | Unsupported BitFields storage type
