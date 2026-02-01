# Distribution Guide

This document outlines how to distribute OpenTweak as a signed, trusted application.

---

## Overview

OpenTweak follows a **source-available** model:
- **Code**: Public on GitHub (PolyForm Shield License)
- **Binary**: Signed executable for trust and convenience

---

## Code Signing Certificate

### Why Sign?

Unsigned executables trigger Windows SmartScreen warnings:
> "Windows protected your PC - Microsoft Defender SmartScreen prevented an unrecognized app from starting."

Signed executables show:
> "Verified Publisher: [Your Company Name]"

### Certificate Options

| Provider | Type | Cost | Validation Time |
|----------|------|------|-----------------|
| **Sectigo** | OV Code Signing | ~$200/year | 1-3 days |
| **DigiCert** | OV Code Signing | ~$400/year | 1-3 days |
| **SSL.com** | OV Code Signing | ~$130/year | 1-3 days |
| **Certum** | OV Code Signing | ~$70/year | 1-3 days |

### Recommendation

**Certum Open Source Code Signing** (~$70/year) is the most cost-effective for open source projects.

### Signing Process

1. **Purchase certificate** from provider
2. **Complete validation** (business registration, identity verification)
3. **Receive USB token** or software certificate
4. **Sign the executable**:

```powershell
# Using signtool (Windows SDK)
signtool sign /f certificate.pfx /p password /tr http://timestamp.digicert.com /td sha256 /fd sha256 OpenTweak.exe

# Or using the GitHub Action (see below)
```

---

## GitHub Actions Workflow

The repository includes [`.github/workflows/build.yml`](../.github/workflows/build.yml) which:

1. Builds the project on every push/PR
2. Runs tests
3. Publishes a single-file executable
4. Creates GitHub Releases on version tags

### Adding Code Signing to CI/CD

To sign releases automatically, add these secrets to your GitHub repository:

- `CERTIFICATE_PFX` - Base64-encoded PFX certificate
- `CERTIFICATE_PASSWORD` - PFX password

Then update the workflow:

```yaml
- name: Sign Executable
  if: startsWith(github.ref, 'refs/tags/v')
  run: |
    $certBytes = [Convert]::FromBase64String("${{ secrets.CERTIFICATE_PFX }}")
    [IO.File]::WriteAllBytes("certificate.pfx", $certBytes)
    signtool sign /f certificate.pfx /p ${{ secrets.CERTIFICATE_PASSWORD }} /tr http://timestamp.digicert.com /td sha256 /fd sha256 ./publish/OpenTweak.exe
```

---

## Distribution Channels

### 1. GitHub Releases (Free)

The CI/CD workflow automatically creates releases when you push a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow will:
1. Build and sign the executable
2. Create a GitHub Release
3. Attach the signed binary

### 2. Gumroad (Paid)

For selling the signed binary:

1. Create a Gumroad account
2. Set up a product ($5-$10 suggested)
3. Upload the signed executable
4. Link to GitHub repository for source code

**Product Description Template:**

```
OpenTweak - Transparent PC Game Optimization

✅ Signed executable (no SmartScreen warnings)
✅ Automatic updates
✅ Priority support

The source code is available at: https://github.com/yourusername/opentweak

License: PolyForm Shield - You can read, modify, and build for personal use. Cannot sell or host competing service.
```

### 3. Microsoft Store (Optional)

For wider distribution:

1. Create a Microsoft Developer account ($19 one-time)
2. Package as MSIX
3. Submit to Microsoft Store

---

## Trust Factors

### For Users

Include these in your README and website:

1. **GitHub Actions Build Logs** - Link to the workflow that built the exact binary
2. **Certificate Details** - Show the signing certificate information
3. **Source Code Availability** - Link to the public repository
4. **VirusTotal Scan** - Link to scan results

### Example README Section

```markdown
## Trust & Transparency

🔒 **Signed Binary**: This executable is signed with a code signing certificate
📋 **Build Logs**: See exactly how this was built: [GitHub Actions](link)
📖 **Open Source**: Full source code available: [GitHub](link)
🛡️ **VirusTotal**: [Scan Results](link)
```

---

## Versioning Strategy

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes (new config format, incompatible with old backups)
- **MINOR**: New features (new launcher support, new UI features)
- **PATCH**: Bug fixes (crash fixes, typo corrections)

### Release Checklist

- [ ] Update version in `OpenTweak.csproj`
- [ ] Update `CHANGELOG.md`
- [ ] Run full test suite
- [ ] Build and sign executable
- [ ] Create GitHub Release
- [ ] Upload to Gumroad (if applicable)
- [ ] Post on relevant forums (Reddit r/pcgaming, etc.)

---

## Monetization Strategy

### The "Trust Premium"

Users pay for:
1. **Convenience** - Pre-built signed executable
2. **Trust** - Code signing certificate verification
3. **Support** - Priority help with issues

### Free vs Paid

| Feature | Free (Self-Build) | Paid (Signed Binary) |
|---------|-------------------|----------------------|
| Source Code | ✅ | ✅ |
| Build Yourself | ✅ | ✅ |
| Signed Executable | ❌ | ✅ |
| Auto-Updates | ❌ | ✅ |
| Support | Community | Priority |

---

## Next Steps

1. **Purchase code signing certificate** (Certum recommended for cost)
2. **Set up GitHub secrets** for CI/CD signing
3. **Create Gumroad account** for distribution
4. **Prepare release announcement** for Reddit/Discord
5. **Monitor feedback** and iterate

---

## Resources

- [Certum Open Source Code Signing](https://en.sklep.certum.pl/data-safety/code-signing-certificates/open-source-code-signing-on-a-token.html)
- [Microsoft signtool documentation](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [GitHub Actions - Encrypted secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [PolyForm Shield License](https://polyformproject.org/licenses/shield/1.0.0/)
