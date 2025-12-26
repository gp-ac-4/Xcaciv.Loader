#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [switch]$UseNet10 = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Test = $false,
    
    [Parameter(Mandatory = $false)]
    [string]$GitHubPat = "",
    
    [Parameter(Mandatory = $false)]
    [string]$NugetEndpoint = "https://nuget.pkg.github.com/xcaciv",

    # Local NuGet packages directory (must exist)
    [Parameter(Mandatory = $false)]
    [string]$LocalNugetPath = "G:\NuGetPackages"
)

# Automatically enable publish mode if GitHubPat is provided
$Publish = $false
if ($GitHubPat -and $GitHubPat.Trim().Length -gt 0) {
    $Publish = $true
}

# Display banner
Write-Host "====================================================="
Write-Host "Xcaciv.Loader Build Script"
Write-Host "====================================================="
if ($Publish) {
    Write-Host "Mode: Publish" -ForegroundColor Cyan
    if ($UseNet10) {
        Write-Host "Target Frameworks: .NET 8.0 and .NET 10.0" -ForegroundColor Cyan
    } else {
        Write-Host "Target Framework: .NET 8.0 only" -ForegroundColor Cyan
    }
} elseif ($UseNet10) {
    Write-Host "Target Frameworks: .NET 8.0 and .NET 10.0"
} else {
    Write-Host "Target Framework: .NET 8.0 only"
}
Write-Host "Run Tests: $( if ($Test) { "Yes" } else { "No" } )"
Write-Host "Local NuGet path: $LocalNugetPath"
if ($Publish) {
    Write-Host "NuGet Push: Enabled (GitHub PAT provided)" -ForegroundColor Green
} else {
    Write-Host "NuGet Push: Disabled (no GitHub PAT)" -ForegroundColor Yellow
}
Write-Host "====================================================="

# Build
Write-Host "Building solution..." -ForegroundColor Cyan
if ($UseNet10) {
    $buildCommand = "dotnet build --configuration Release /p:UseNet10=true"
} else {
    $buildCommand = "dotnet build --configuration Release"
}
Write-Host "Executing: $buildCommand" -ForegroundColor Gray
Invoke-Expression $buildCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

# If publishing, copy packages to publish directory
if ($Publish) {
    # Create publish directory
    $publishDir = Join-Path $PSScriptRoot "publish"
    if (Test-Path $publishDir) {
        Write-Host "Cleaning publish directory..." -ForegroundColor Cyan
        Remove-Item -Path $publishDir -Recurse -Force
    }
    New-Item -Path $publishDir -ItemType Directory -Force | Out-Null
    
    # Find and copy NuGet packages
    Write-Host "Collecting NuGet packages..." -ForegroundColor Cyan
    $packagePaths = @(
        "src\Xcaciv.Loader\bin\Release\*.nupkg",
        "src\Xcaciv.Loader\bin\Release\*.snupkg"
    )
    
    $copiedCount = 0
    foreach ($pattern in $packagePaths) {
        $fullPattern = Join-Path $PSScriptRoot $pattern
        $packages = Get-ChildItem -Path $fullPattern -ErrorAction SilentlyContinue
        foreach ($package in $packages) {
            Copy-Item -Path $package.FullName -Destination $publishDir -Force
            Write-Host "  Copied: $($package.Name)" -ForegroundColor Green
            $copiedCount++
        }
    }
    
    if ($copiedCount -eq 0) {
        Write-Host "Warning: No NuGet packages found!" -ForegroundColor Yellow
    } else {
        Write-Host "Copied $copiedCount package(s) to publish directory" -ForegroundColor Green
    }

    # Copy to local NuGet packages directory
    if ([string]::IsNullOrWhiteSpace($LocalNugetPath)) {
        Write-Host "Warning: LocalNugetPath is not specified, skipping local copy" -ForegroundColor Yellow
    } else {
        try {
            $targetLocal = $LocalNugetPath.TrimEnd('\', '/')
            if (-not (Test-Path -Path $targetLocal -PathType Container)) {
                Write-Host "ERROR: Local NuGet directory does not exist: $targetLocal" -ForegroundColor Red
                exit 1
            }
            Write-Host "Copying packages to local directory: $targetLocal" -ForegroundColor Cyan
            $localCopied = 0
            $publishPackages = Get-ChildItem -Path $publishDir -Filter "*.nupkg" -ErrorAction SilentlyContinue
            $publishSymbols = Get-ChildItem -Path $publishDir -Filter "*.snupkg" -ErrorAction SilentlyContinue
            foreach ($pkg in @($publishPackages + $publishSymbols)) {
                Copy-Item -Path $pkg.FullName -Destination $targetLocal -Force
                Write-Host "  Copied locally: $($pkg.Name)" -ForegroundColor Green
                $localCopied++
            }
            if ($localCopied -eq 0) {
                Write-Host "No packages found to copy locally." -ForegroundColor Yellow
            } else {
                Write-Host "Copied $localCopied package(s) to local directory: $targetLocal" -ForegroundColor Green
            }
        } catch {
            Write-Host "Failed to copy packages locally: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
}

# Run tests if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Cyan
    $testCommand = "dotnet test --no-build --configuration Release"
    if ($UseNet10) {
        $testCommand += " /p:UseNet10=true"
    }
    Write-Host "Executing: $testCommand" -ForegroundColor Gray
    Invoke-Expression $testCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Push packages to NuGet (GitHub Packages) only when publishing
if ($Publish) {
    Write-Host "====================================================="
    Write-Host "Pushing packages to NuGet (GitHub Packages)..." -ForegroundColor Cyan
    
    # Use publish output directory as package source
    $packageSourceDir = Join-Path $PSScriptRoot "publish"
    
    $normalizedEndpoint = ($NugetEndpoint -replace '\\', '/')
    if ($normalizedEndpoint -notmatch '/index\.json$') {
        $sourceUrl = "$normalizedEndpoint/index.json"
    } else {
        $sourceUrl = $normalizedEndpoint
    }
    
    $packagesToPush = Get-ChildItem -Path $packageSourceDir -Filter "*.nupkg" -ErrorAction SilentlyContinue
    if (-not $packagesToPush) {
        Write-Host "No .nupkg files found to push in: $packageSourceDir" -ForegroundColor Yellow
    } else {
        foreach ($pkg in $packagesToPush) {
            Write-Host "Pushing: $($pkg.Name)" -ForegroundColor Cyan
            $pushArgs = @(
                'nuget','push',
                $pkg.FullName,
                '--source',$sourceUrl,
                '--api-key',$GitHubPat,
                '--skip-duplicate'
            )
            Write-Host "Executing: dotnet nuget push [package] --source $sourceUrl --api-key ***REDACTED*** --skip-duplicate" -ForegroundColor Gray
            # Execute and capture output to detect specific warnings-as-errors
            $pushOutput = & dotnet $pushArgs 2>&1
            # Echo output to console
            if ($pushOutput) { $pushOutput | ForEach-Object { Write-Host $_ } }
            
            if ($LASTEXITCODE -ne 0) {
                # Ignore NU1510 warnings treated as errors
                if ($pushOutput -match 'NU1510') {
                    Write-Host "NuGet push reported NU1510; treating as warning and continuing." -ForegroundColor Yellow
                    continue
                }
                Write-Host "NuGet push failed for $($pkg.Name) with exit code $LASTEXITCODE" -ForegroundColor Red
                exit $LASTEXITCODE
            } else {
                Write-Host "  Pushed: $($pkg.Name)" -ForegroundColor Green
            }
        }
    }
}

# Final summary
Write-Host "====================================================="
if ($Publish) {
    Write-Host "Publish completed successfully!" -ForegroundColor Green
    Write-Host "Packages available in: $(Join-Path $PSScriptRoot 'publish')" -ForegroundColor Cyan
    if (-not [string]::IsNullOrWhiteSpace($LocalNugetPath)) {
        Write-Host "Local packages copied to: $LocalNugetPath" -ForegroundColor Cyan
    }
} else {
    Write-Host "Build completed successfully!" -ForegroundColor Green
}
Write-Host "====================================================="