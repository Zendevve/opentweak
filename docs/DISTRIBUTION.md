# Distribution Guide

This document outlines the distribution model for OpenTweak and explains how users can obtain the software.

---

## Overview

OpenTweak follows a **source-available, binary-commercial** model:

| Aspect | Terms |
|--------|-------|
| **Source Code** | Public on GitHub — free to view, modify, and build for personal use |
| **Pre-built Binaries** | Available exclusively through BuyMeACoffee — $25 USD |
| **License** | PolyForm Shield with Commercial Distribution Addendum |

---

## Obtaining OpenTweak

### Option 1: Build from Source (Free)

You can build OpenTweak yourself at no cost:

```powershell
# Clone the repository
git clone https://github.com/nathanielopentweak/opentweak.git
cd opentweak

# Build the project
dotnet build OpenTweak.sln --configuration Release

# Or publish a single-file executable
dotnet publish OpenTweak/OpenTweak.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -o ./publish
```

**Requirements:**
- .NET 8.0 SDK
- Windows 10/11

### Option 2: Purchase Pre-built Binary ($25 USD)

For convenience, pre-built binaries are available:

**Purchase Link:** https://buymeacoffee.com/opentweak

**What you get:**
- Ready-to-run executable (no build needed)
- Priority support via email/Discord
- Future updates (within major version)

**Note:** The purchased binary is functionally identical to what you would build from source. You are paying for convenience and support, not additional features.

---

## Why Charge for Binaries?

As a student developer, I cannot afford:
- Code signing certificates ($70-400/year)
- Extended validation processes
- Enterprise infrastructure

The $25 fee helps:
1. Cover development time and tools
2. Fund future improvements
3. Provide priority support
4. Keep the project sustainable

The source code remains fully open — you are never forced to pay. Building from source is always free and fully supported.

---

## License Summary

### What You CAN Do (Free)

- ✅ View and study the source code
- ✅ Build and run for personal use
- ✅ Modify for your own needs
- ✅ Submit bug reports and feature requests
- ✅ Fork for personal experimentation

### What You CANNOT Do (Without Permission)

- ❌ Distribute pre-built binaries
- ❌ Offer automated build services that provide binaries to others
- ❌ Include in package managers (winget, chocolatey, scoop)
- ❌ Create competing products based on this code
- ❌ Sell derivative works

### What Requires Purchase

- 💰 Downloading pre-built executables ($25 via BuyMeACoffee)

---

## For Developers: Building from Source

### Prerequisites

- Windows 10 or later
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

### Build Steps

1. **Clone the repository:**
   ```powershell
   git clone https://github.com/nathanielopentweak/opentweak.git
   ```

2. **Restore dependencies:**
   ```powershell
   dotnet restore
   ```

3. **Build:**
   ```powershell
   dotnet build --configuration Release
   ```

4. **Run tests:**
   ```powershell
   dotnet test
   ```

5. **Publish (optional):**
   ```powershell
   dotnet publish OpenTweak/OpenTweak.csproj -c Release -r win-x64 -p:PublishSingleFile=true -o ./publish
   ```

The resulting executable will be in `./publish/OpenTweak.exe`.

---

## CI/CD Policy

### GitHub Actions

The repository includes automated workflows that:
- Build the project on every push/PR
- Run the test suite
- Verify code quality

**Important:** CI/CD workflows do NOT produce downloadable binaries. The build artifacts are used only for verification and are not persisted as releases.

### Why No Free Binaries?

Providing free binaries would:
1. Undermine the sustainability model
2. Violate the license terms for commercial distribution
3. Create support burden without revenue

Users who want binaries must purchase through the authorized channel (BuyMeACoffee).

---

## Anti-Automation Clause

To protect the commercial model, the following are explicitly prohibited:

### Prohibited Services

Any service, platform, or automated system that:
- Accepts OpenTweak source code as input
- Automatically compiles/builds the software
- Distributes or makes available the resulting binaries
- Acts as a "build farm" or "CI-as-a-service" for OpenTweak

### Examples of Prohibited Activities

- Running a website where users paste source code and receive compiled binaries
- Operating a Discord bot that builds and distributes executables
- Creating GitHub Actions workflows that publish binaries to releases
- Offering OpenTweak builds through package managers

### Permitted Activities

- Building for your own personal use
- Building within your organization for internal use
- CI/CD for development/testing (without binary distribution)
- Educational demonstrations of build processes

---

## Enforcement

Violations of the distribution terms will be handled as follows:

1. **First Contact:** Written notice requesting compliance
2. **Compliance Period:** 32 days to cease prohibited activity
3. **Escalation:** If not resolved, license termination and potential legal action

---

## Frequently Asked Questions

### Q: Can I build this for my friend?

You can help your friend build it on their machine, but you cannot give them a pre-built binary. They must build it themselves or purchase a license.

### Q: Can I include this in my company's internal tools?

Yes, building from source for internal company use is permitted. You cannot distribute the binary outside your organization.

### Q: What if I can't afford $25?

Build from source — it's free and fully functional. The fee is only for convenience, not access.

### Q: Can I fork this and distribute my own binaries?

No. Forks must also comply with the license terms. You cannot distribute binaries of derivative works either.

### Q: Will there ever be free binaries?

No. The source-available/binary-commercial model is fundamental to the project's sustainability.

---

## Contact

For distribution inquiries or licensing questions:

- **GitHub Issues:** https://github.com/nathanielopentweak/opentweak/issues
- **BuyMeACoffee:** https://buymeacoffee.com/opentweak

---

*Last updated: 2025-02-01*
