; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.9.6

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SD0001  | Stardust.BitFields | Error | BitField exceeds 32-bit nint/nuint range on 32-bit platform
SD0002  | Stardust.BitFields | Warning | BitField may exceed 32-bit nint/nuint range on AnyCPU build
SD0003  | Stardust.BitFields | Error | Unsupported BitFields storage type
SD0004  | Stardust.BitFields | Error | BitField/BitFlag property must be declared partial
SD0015  | Stardust.BitFields | Info | Two-parameter BitField constructor warning
SD0016  | Stardust.BitFields | Warning | Redundant End and Width on BitField
SD0017  | Stardust.BitFields | Error | Inconsistent End and Width on BitField
SD0018  | Stardust.BitFields | Error | BitField missing End or Width
SD0019  | Stardust.BitFields | Error | BitField missing Start
