# Copilot Instructions

## General Guidelines
- Use plain text only in responses - do not use markdown hyperlinks. Output gets garbled when it contains hyperlinks due to a known bug.

## Project-Specific Rules
- Always use the local NuGet package for Stardust.Utilities (currently 0.9.4 in pre-release).
- When generator changes are made, rebuild it using .\Build-Combined-NuGetPackages.ps1 0.9.4 -SkipTests. Don't update the version number.