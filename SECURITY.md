# Security Policy

## Supported Versions

We actively maintain security updates for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 0.9.x   | :white_check_mark: |
| < 0.9   | :x:                |

Once version 1.0 is released, we will provide security updates for the current major version and one prior major version.

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability in Stardust.Utilities, please report it responsibly.

### How to Report

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, use GitHub's private vulnerability reporting:
1. Go to the [Security tab](https://github.com/dhadner/Stardust.Utilities/security)
2. Click "Report a vulnerability"
3. Fill out the form with details

### What to Include

Please include as much of the following information as possible:

- Type of vulnerability (e.g., buffer overflow, code injection, etc.)
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact assessment (what an attacker could achieve)

### What to Expect

- **Acknowledgment**: We will acknowledge receipt within 48 hours
- **Initial Assessment**: Within 7 days, we will provide an initial assessment
- **Resolution Timeline**: We aim to resolve critical vulnerabilities within 30 days
- **Disclosure**: We will coordinate with you on public disclosure timing

### Scope

The following are considered in-scope for security reports:

- **Source generator vulnerabilities** that could generate malicious code
- **Parsing vulnerabilities** in `Parse`/`TryParse` methods that could cause crashes or unexpected behavior
- **Memory safety issues** in unsafe code blocks
- **Denial of service** vulnerabilities in the library

The following are generally out-of-scope:

- Vulnerabilities in dependencies (report to the dependency maintainer)
- Issues requiring physical access to a user's machine
- Social engineering attacks
- Issues in unsupported versions

## Security Best Practices for Users

When using Stardust.Utilities:

1. **Keep updated**: Always use the latest stable version
2. **Validate input**: When parsing user-provided strings, always use `TryParse` instead of `Parse`
3. **Review generated code**: Use `EmitCompilerGeneratedFiles` to inspect generated source during development

## Acknowledgments

We appreciate responsible disclosure and will acknowledge security researchers who report valid vulnerabilities 
(with your permission) in our release notes.

Thank you for helping keep Stardust.Utilities and its users safe!
