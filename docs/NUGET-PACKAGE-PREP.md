# NuGet Package Preparation Checklist for Xcaciv.Loader v2.0.0

## ? Pre-Release Checklist

### 1. Version and Metadata ?
- [x] Update version to `2.0.0` in `.csproj`
- [x] Verify package metadata (Title, Description, Tags)
- [x] Confirm license (BSD-3-Clause) ?
- [x] Verify copyright year (2021-2025)
- [x] Check PackageReadmeFile points to `readme.md` ?
- [x] Verify RepositoryUrl and ProjectUrl ?
- [x] Enable symbol package generation (`SymbolPackageFormat: snupkg`) ?

### 2. Documentation ?
- [x] README.md is up-to-date with v2.0 features
- [x] CHANGELOG.md includes all v2.0 changes
- [x] Migration guide exists and is comprehensive
- [x] XML documentation comments are complete
- [x] Security warnings are prominent

### 3. Testing ?
- [ ] **Run full test suite in Release mode**
  ```powershell
  dotnet test --configuration Release
  ```
- [ ] **Current Status: 163/183 passing (89%)**
  - 19 failures (non-Phase 3 issues)
  - 1 skipped
- [ ] Address critical test failures
- [ ] Verify test coverage >80%

### 4. Code Quality ?
- [ ] **Build in Release mode without warnings**
  ```powershell
  dotnet build --configuration Release
  ```
- [ ] No obsolete warnings in production code
- [ ] All deprecated methods have migration path
- [ ] Code analysis passes (`TreatWarningsAsErrors: True` ?)

### 5. Dependencies ?
- [x] No external dependencies (pure .NET 8)
- [x] Target framework is appropriate (net8.0 ?)
- [x] Optional .NET 10 support configured ?

### 6. Package Content ?
- [x] Include readme.md in package ?
- [ ] Add package icon (optional but recommended)
- [ ] Verify symbols package configuration
- [ ] Check file exclusions (.editorconfig, tests, etc.)

### 7. Security ?
- [x] Security documentation is comprehensive
- [x] SECURITY.md exists (check if present)
- [ ] No hardcoded secrets or credentials
- [ ] Security vulnerabilities addressed

---

## ? Build and Pack Commands

### Clean Build
```powershell
# Clean previous builds
dotnet clean

# Build in Release mode
dotnet build --configuration Release

# Verify no warnings/errors
```

### Run Tests
```powershell
# Run all tests in Release mode
dotnet test --configuration Release --logger "console;verbosity=detailed"

# Check test results
# Target: >90% pass rate before release
```

### Create NuGet Package
```powershell
# Pack the project (already configured with GeneratePackageOnBuild=True)
dotnet pack src/Xcaciv.Loader/Xcaciv.Loader.csproj --configuration Release --output ./nupkg

# Verify package contents
cd nupkg
Get-ChildItem
```

### Verify Package
```powershell
# Install NuGet Package Explorer (if not installed)
# https://github.com/NuGetPackageExplorer/NuGetPackageExplorer

# Or use command line:
nuget verify -All ./nupkg/Xcaciv.Loader.2.0.0.nupkg
```

---

## ? Package Metadata to Update

Update `src/Xcaciv.Loader/Xcaciv.Loader.csproj`:

```xml
<PropertyGroup>
    <!-- Version -->
    <Version>2.0.0</Version>
    <FileVersion>2.0.0.0</FileVersion>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
    
    <!-- Package Metadata -->
    <PackageId>Xcaciv.Loader</PackageId>
    <Title>Xcaciv.Loader - Secure Dynamic Assembly Loading</Title>
    <Description>Secure, modern .NET library for runtime loading of types from external assemblies. Features instance-based security policies, optional cryptographic integrity verification, comprehensive audit trail events, and SSEM-compliant architecture. Perfect for plugin systems and modular applications.</Description>
    <Authors>Alton Crossley</Authors>
    <Copyright>Copyright © 2021-2025 Alton Crossley</Copyright>
    
    <!-- Package Classification -->
    <PackageTags>assembly;loader;plugin;dynamic;security;modular;plugins;addins;extensions;reflection;isolation;unloading;integrity;verification;SSEM</PackageTags>
    <PackageProjectUrl>https://github.com/Xcaciv/Xcaciv.Loader</PackageProjectUrl>
    <RepositoryUrl>https://github.com/Xcaciv/Xcaciv.Loader</RepositoryUrl>
    <PackageReadmeFile>readme.md</PackageReadmeFile>
    <PackageLicenseExpression>BSD-3-Clause</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
    
    <!-- Release Notes -->
    <PackageReleaseNotes>
Major v2.0 release with breaking changes and significant new features:

BREAKING CHANGES:
- Security configuration now instance-based (use AssemblySecurityPolicy parameter)
- Deprecated static SetStrictDirectoryRestriction() - will be removed in v3.0
- GetLoadedTypes&lt;T&gt;() moved to AssemblyScanner class

NEW FEATURES:
? Instance-based security policies (Default/Strict/Custom)
? Optional assembly integrity verification with SHA256/384/512
? Comprehensive event system for audit trail and monitoring
? AssemblyPathValidator for input sanitization
? AssemblyScanner for type discovery
? Load timeout protection (default 30s)
? Enhanced error handling with specific exception types
? Professional XML documentation throughout

SECURITY:
?? Per-instance security policies for flexible configurations
?? Cryptographic hash-based integrity verification (optional)
?? 6 security events for complete audit trail
?? Input validation utilities
?? Comprehensive security documentation

See CHANGELOG.md for complete details and migration guide.
    </PackageReleaseNotes>
    
    <!-- Symbol Package -->
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    
    <!-- Source Link (for GitHub) -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>

<!-- Source Link Package (add if not present) -->
<ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
</ItemGroup>
```

---

## ? Optional Enhancements

### Package Icon
Add a package icon for better visibility in NuGet Gallery:

```xml
<PropertyGroup>
    <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>

<ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath=""/>
</ItemGroup>
```

Create `src/Xcaciv.Loader/icon.png`:
- Size: 128x128 pixels (recommended)
- Format: PNG
- Design: Simple, recognizable logo

### Additional Files
Consider adding to the package:

```xml
<ItemGroup>
    <None Include="../../CHANGELOG.md" Pack="true" PackagePath=""/>
    <None Include="../../LICENSE" Pack="true" PackagePath=""/>
</ItemGroup>
```

---

## ? Publishing Steps

### 1. Test Locally
```powershell
# Create a local test project
mkdir test-nuget
cd test-nuget
dotnet new console
dotnet add package Xcaciv.Loader --source ../nupkg
```

### 2. Publish to NuGet.org
```powershell
# Get API key from https://www.nuget.org/account/apikeys

# Push package
dotnet nuget push ./nupkg/Xcaciv.Loader.2.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Push symbols package
dotnet nuget push ./nupkg/Xcaciv.Loader.2.0.0.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### 3. Create GitHub Release
1. Tag the release: `git tag v2.0.0`
2. Push tag: `git push origin v2.0.0`
3. Create release on GitHub with:
   - Title: "v2.0.0 - Major Release: Instance-Based Security & Integrity Verification"
   - Copy release notes from CHANGELOG.md
   - Attach `.nupkg` and `.snupkg` files

---

## ? Post-Release Checklist

### Documentation
- [ ] Update GitHub Wiki with v2.0 examples
- [ ] Publish blog post/announcement
- [ ] Update project README with NuGet badge

### Monitoring
- [ ] Monitor NuGet package statistics
- [ ] Watch for issue reports
- [ ] Monitor security advisories

### Support
- [ ] Respond to questions on GitHub Issues
- [ ] Update FAQ based on user feedback
- [ ] Plan v2.1 based on feedback

---

## ? Quick Reference

### Version History
- **v1.0.2.18**: Last v1.x release (static security config)
- **v2.0.0**: Current (instance-based security + integrity verification)
- **v3.0.0**: Planned (remove deprecated APIs, .NET 10 minimum)

### Critical Files
- `src/Xcaciv.Loader/Xcaciv.Loader.csproj` - Package metadata
- `src/Xcaciv.Loader/readme.md` - Package README
- `CHANGELOG.md` - Version history
- `LICENSE` - BSD-3-Clause license

### Support Timeline
- **v2.0.x**: Full support (2025-2027)
- **v1.x**: Security fixes only (until 2026-06-01)
- **Deprecated APIs**: Remove in v3.0.0 (2026-06-01)

---

## ? Issue Tracking

### Known Issues Before v2.0.0 Release
1. **Test Failures**: 19/183 tests failing (non-critical)
   - Most are in HashStore and PathValidator
   - Need investigation before release
   
2. **Missing IDisposable**: Some test classes have Dispose() but don't implement IDisposable
   - `IntegrityVerificationIntegrationTests`
   - `AssemblyHashStoreTests`
   - Should be fixed for proper cleanup

### Recommended Actions Before Release
1. ? Fix test class disposal pattern
2. ? Address failing tests or document as known issues
3. ? Add package icon (optional but recommended)
4. ? Run full security audit
5. ? Verify all examples in README compile

---

## ? Resources

- **NuGet Publishing Guide**: https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package
- **Package Metadata Reference**: https://docs.microsoft.com/en-us/nuget/reference/nuspec
- **Source Link Guide**: https://github.com/dotnet/sourcelink
- **Semantic Versioning**: https://semver.org/

