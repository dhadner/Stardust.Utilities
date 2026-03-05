# Privacy Statement

## Data Collection

**Stardust.Utilities does not collect, transmit, or store any personal data, telemetry, or usage information.**

This is a compile-time source generator and runtime utility library. It runs entirely on your machine during 
build (source generator) and within your application process at runtime (utility types). There are no network 
calls, no phone-home behavior, and no analytics of any kind.

## Blazor WebAssembly Demo

The interactive demo hosted at https://dhadner.github.io/Stardust.Utilities/ runs entirely in your browser 
using WebAssembly. It does not use cookies, server-side analytics, or local storage for tracking. The demo 
does use a single `localStorage` flag to detect a prior WebAssembly crash (caused by Edge Enhanced Security 
Mode) so it can show a helpful error message instead of crashing again. This flag contains no personal data 
and is not transmitted anywhere. No data leaves your browser. GitHub Pages may collect standard web server 
logs (IP address, user agent) as described in the GitHub Privacy Statement 
(https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Third-Party Dependencies

Stardust.Utilities depends on standard Microsoft .NET libraries and Microsoft.SourceLink.GitHub (build-time 
only). These dependencies do not introduce any runtime data collection. Review their respective privacy 
policies for build-time behavior:

- Microsoft .NET: https://privacy.microsoft.com/en-us/privacystatement
- Microsoft SourceLink: https://github.com/dotnet/sourcelink (build-time only, embeds source control metadata)

## Contact

If you have questions about this privacy statement, please open an issue at 
https://github.com/dhadner/Stardust.Utilities/issues.
