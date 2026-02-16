# Copilot Instructions

## General Guidelines
- Use plain text only in responses - do not use markdown hyperlinks. Output gets garbled when it contains hyperlinks due to a known bug.

## Project-Specific Rules
- The package version is defined in Directory.Build.props at the repo root. Demo app csproj files reference it via $(Version) automatically.
- Always use the local NuGet package for Stardust.Utilities. Do not change the version in Directory.Build.props unless asked.
- Only rebuild the NuGet package (Build-Combined-NuGetPackages.ps1) when generator code or library code changes. Demo app changes only need a regular dotnet build since the generator is already packaged.
- When generator changes are made, rebuild using .\Build-Combined-NuGetPackages.ps1 -SkipTests (version is read from Directory.Build.props automatically).
- When changing features (adding, modifying, removing), ensure that appropriate test coverage is also updated (added, modified, removed).
- Features that include user or external input will require fuzz testing to ensure robustness and correct operation.
- Don't let a human or downstream user find an error that unit testing could have caught.