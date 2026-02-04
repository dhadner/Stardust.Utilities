---
name: Bug Report
about: Report a bug to help us improve Stardust.Utilities
title: '[BUG] '
labels: bug
assignees: ''
---

## Description

A clear and concise description of the bug.

## To Reproduce

Steps to reproduce the behavior:

1. Define a struct with `[BitFields(typeof(...))]`
2. Add properties with `[BitField(...)]` or `[BitFlag(...)]`
3. Call method '...'
4. See error

## Expected Behavior

A clear and concise description of what you expected to happen.

## Actual Behavior

What actually happened, including any error messages or stack traces.

## Code Sample

```csharp
// Minimal code to reproduce the issue
[BitFields(typeof(byte))]
public partial struct MyRegister
{
    [BitFlag(0)] public partial bool Flag { get; set; }
}
```

## Generated Code (if applicable)

If the issue is with generated code, please include the relevant portion:

```csharp
// Paste generated code here (found in obj/Generated/...)
```

## Environment

- **Stardust.Utilities version**: [e.g., 0.9.2]
- **.NET version**: [e.g., .NET 10.0]
- **OS**: [e.g., Windows 11, macOS 14, Ubuntu 24.04]
- **IDE**: [e.g., Visual Studio 2026, VS Code, Rider]

## Additional Context

Add any other context about the problem here.
