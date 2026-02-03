# Contributing to Stardust.Utilities

Thank you for your interest in contributing to Stardust.Utilities! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Features](#suggesting-features)
  - [Pull Requests](#pull-requests)
- [Development Setup](#development-setup)
- [Code Style](#code-style)
- [Testing](#testing)
- [License](#license)

---

## Code of Conduct

Please be respectful and constructive in all interactions. We welcome contributors of all experience levels.

---

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Stardust.Utilities.git
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

---

## How to Contribute

### Reporting Bugs

Before reporting a bug:
- Check existing [issues](https://github.com/dhadner/Stardust.Utilities/issues) to avoid duplicates
- Ensure you're using the latest version

When reporting, please include:
- .NET version and OS
- Minimal code to reproduce the issue
- Expected vs. actual behavior
- Full error message/stack trace if applicable

### Suggesting Features

Feature requests are welcome! Please:
- Check existing issues first
- Clearly describe the use case
- Explain why existing features don't meet your needs

### Pull Requests

1. **Keep PRs focused** - One feature or fix per PR
2. **Write tests** - All new features need unit tests
3. **Update documentation** - Update README.md if adding user-facing features
4. **Follow code style** - See [Code Style](#code-style) below
5. **Ensure CI passes** - All tests must pass before merge

#### PR Checklist

- [ ] Code compiles without warnings
- [ ] All tests pass (`dotnet test`)
- [ ] New features have unit tests
- [ ] Documentation updated (if applicable)
- [ ] Commit messages are clear and descriptive

---

## Development Setup

For detailed development workflows, build scripts, and package publishing instructions, see **[DEVELOPER.md](DEVELOPER.md)**.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Visual Studio 2026 or VS Code with C# Dev Kit extension

### Opening the Project

Open `Stardust.Utilities.slnx` in Visual Studio or VS Code. This is the new XML-based solution format (`.slnx`) which is cleaner and easier to merge than the legacy `.sln` format.

```powershell
# Open in Visual Studio
start Stardust.Utilities.slnx

# Or open in VS Code
code .
```

### Building

```powershell
# Restore and build
dotnet build

# Run tests
dotnet test Test/Stardust.Utilities.Tests.csproj

# Build NuGet packages (version is required)
.\Build-Combined-NuGetPackages.ps1 0.9.0

# Skip tests for faster iteration
.\Build-Combined-NuGetPackages.ps1 0.9.0 -SkipTests
```

### Build Scripts

The repository includes PowerShell build scripts to simplify package development:

| Script | Purpose |
|--------|---------|
| `Build-Combined-NuGetPackages.ps1` | Builds the distributable `Stardust.Utilities` NuGet package |
| `Build-Generator-NuGetPackage.ps1` | Builds the standalone generator package (for local development only) |

**Key behavior of `Build-Combined-NuGetPackages.ps1`:**
- Version is specified as a **required** command-line argument (e.g., `0.9.0`)
- Version is NOT stored in .csproj files
- Packages are automatically published to the local NuGet feed (`~/.nuget/local-packages/`)
- NuGet cache is automatically cleared so consuming projects pick up changes
- Packages appear in Visual Studio's NuGet Package Manager under the local feed

> **Note:** The `Stardust.Generators` package is **not for distribution**. It exists solely to support local debugging scenarios where `Stardust.Utilities` is referenced via `ProjectReference`. The source generator is embedded directly within the `Stardust.Utilities` package for end users.

See [DEVELOPER.md](DEVELOPER.md) for detailed usage examples and the complete build workflow.

### Project Structure

```
Stardust.Utilities/
├── Stardust.Utilities.slnx # Solution file (XML format, .NET 9+)
├── *.cs                    # Core library types (Result, BigEndian, etc.)
├── Generators/             # Source generator project (embedded in main package)
│   └── BitFieldsGenerator.cs
├── Test/                   # Unit tests
├── build/                  # MSBuild props/targets for NuGet
└── nupkg/                  # Local NuGet output (gitignored)
```

### Debugging Source Generators

Source generators can be tricky to debug. Tips:

1. Add `#error` directives in generated code to see compiler output
2. Use `Debugger.Launch()` in the generator (remove before committing!)
3. Check `Test/Generated/` for emitted source files

---

## Code Style

We follow standard C# conventions with these specifics:

### General

- Use C# 14 features where appropriate
- Prefer `new()` over `new ClassName()` when type is clear
- Use `[]` for empty collections instead of `new List<T>()`
- Use file-scoped namespaces

### Documentation

- All public types and members require XML documentation (`///`)
- Include `<summary>`, `<param>`, and `<returns>` as appropriate
- Use `<example>` blocks for non-obvious APIs

### Naming

- `PascalCase` for public members
- `_camelCase` for private fields
- `camelCase` for local variables and parameters

### Example

```csharp
/// <summary>
/// Represents a validated email address.
/// </summary>
/// <param name="value">The email string.</param>
public readonly record struct Email(string Value)
{
    private static readonly Regex _pattern = new(@"^[^@]+@[^@]+$");

    /// <summary>
    /// Validates and creates an Email instance.
    /// </summary>
    /// <param name="input">The input string to validate.</param>
    /// <returns>A Result containing the Email or an error message.</returns>
    public static Result<Email, string> Create(string input) =>
        _pattern.IsMatch(input)
            ? Result<Email, string>.Ok(new Email(input))
            : Result<Email, string>.Err("Invalid email format");
}
```

---

## Testing

### Running Tests

```powershell
# Run all tests
dotnet test Test/Stardust.Utilities.Tests.csproj

# Run with detailed output
dotnet test Test/Stardust.Utilities.Tests.csproj --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~BitFieldTests"
```

### Writing Tests

- Use [xUnit](https://xunit.net/) for test framework
- Use [FluentAssertions](https://fluentassertions.com/) for assertions
- Name tests: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public void Ok_WithValue_ReturnsSuccessResult()
{
    var result = Result<int, string>.Ok(42);
    
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(42);
}
```

---

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).

---

## Questions?

Feel free to open an issue for any questions about contributing!
