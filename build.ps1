#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [switch]$UseNet10 = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Test = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Publish = $false,
    
    [Parameter(Mandatory = $false)]
    [string]$GitHubPat = "",
    
    [Parameter(Mandatory = $false)]
    [string]$NugetEndpoint = "https://nuget.pkg.github.com/xcaciv",

    # Local NuGet packages directory (must exist)
    [Parameter(Mandatory = $false)]
    [string]$LocalNugetPath = "G:\NuGetPackages"
)

# Display banner
Write-Host "====================================================="
Write-Host "Xcaciv.Loader Build Script"
Write-Host "====================================================="
if ($Publish) {
    Write-Host "Mode: Publish (building for .NET 8.0 and .NET 10.0)"
} elseif ($UseNet10) {
    Write-Host "Building for both .NET 8.0 and .NET 10.0"
} else {
    Write-Host "Target Framework: .NET 8.0"
}
Write-Host "Run Tests: $( if ($Test) { "Yes" } else { "No" } )"
Write-Host "Local NuGet path: $LocalNugetPath"
if ($GitHubPat -and $GitHubPat.Trim().Length -gt 0) {
    Write-Host "NuGet Push: Enabled (GitHub PAT provided)" -ForegroundColor Green
} else {
    Write-Host "NuGet Push: Disabled (no GitHub PAT)" -ForegroundColor Yellow
}
Write-Host "====================================================="

if ($Publish) {
    # Publish mode: Build for both .NET 8 and .NET 10
    Write-Host "Building for .NET 8.0..." -ForegroundColor Cyan
    $buildCommand = "dotnet build --configuration Release"
    Write-Host "Executing: $buildCommand" -ForegroundColor Gray
    Invoke-Expression $buildCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for .NET 8.0 with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host "Building for .NET 10.0..." -ForegroundColor Cyan
    $buildCommand = "dotnet build --configuration Release /p:UseNet10=true"
    Write-Host "Executing: $buildCommand" -ForegroundColor Gray
    Invoke-Expression $buildCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for .NET 10.0 with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
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

    # Mandatory local copy to NuGet packages directory (only if directory exists)
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
    
} else {
    # Standard build mode
    if ($UseNet10) {
        # Build for both .NET 8 and .NET 10
        Write-Host "Building for .NET 8.0..." -ForegroundColor Cyan
        $buildCommand = "dotnet build --configuration Release"
        Write-Host "Executing: $buildCommand" -ForegroundColor Gray
        Invoke-Expression $buildCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed for .NET 8.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        
        Write-Host "Building for .NET 10.0..." -ForegroundColor Cyan
        $buildCommand = "dotnet build --configuration Release /p:UseNet10=true"
        Write-Host "Executing: $buildCommand" -ForegroundColor Gray
        Invoke-Expression $buildCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed for .NET 10.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } else {
        # Build for .NET 8 only
        Write-Host "Building solution..." -ForegroundColor Cyan
        $buildCommand = "dotnet build --configuration Release"
        Write-Host "Executing: $buildCommand" -ForegroundColor Gray
        Invoke-Expression $buildCommand

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}

# Run tests if requested
if ($Test) {
    if ($Publish) {
        # Run tests for both frameworks
        Write-Host "Running tests for .NET 8.0..." -ForegroundColor Cyan
        $testCommand = "dotnet test --no-build --configuration Release"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for .NET 8.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        
        Write-Host "Running tests for .NET 10.0..." -ForegroundColor Cyan
        $testCommand = "dotnet test --no-build --configuration Release /p:UseNet10=true"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for .NET 10.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } elseif ($UseNet10) {
        # Run tests for both frameworks
        Write-Host "Running tests for .NET 8.0..." -ForegroundColor Cyan
        $testCommand = "dotnet test --no-build --configuration Release"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for .NET 8.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        
        Write-Host "Running tests for .NET 10.0..." -ForegroundColor Cyan
        $testCommand = "dotnet test --no-build --configuration Release /p:UseNet10=true"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for .NET 10.0 with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    } else {
        # Standard test mode (.NET 8 only)
        Write-Host "Running tests..." -ForegroundColor Cyan
        $testCommand = "dotnet test --no-build --configuration Release"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}

# Push packages to NuGet (GitHub Packages) only when publishing and PAT provided
if ($Publish -and $GitHubPat -and $GitHubPat.Trim().Length -gt 0) {
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
            $loggingArgs = $pushArgs.Clone()
            for ($i = 0; $i -lt $loggingArgs.Length; $i++) {
                if ($loggingArgs[$i] -eq '--api-key' -and ($i + 1) -lt $loggingArgs.Length) {
                    $loggingArgs[$i + 1] = '***REDACTED***'
                    break
                }
            }
            Write-Host "Executing: dotnet $($loggingArgs -join ' ')" -ForegroundColor Gray
            # Execute and capture output to detect specific warnings-as-errors
            $pushOutput = & dotnet $pushArgs 2>&1
            # Echo output to console
            if ($pushOutput) { $pushOutput | ForEach-Object { Write-Host $_ } }
            
            if ($LASTEXITCODE -ne 0) {
                # NU1510 indicates a signing or trust issue; log explicit context but still treat as an error
                if ($pushOutput -match 'NU1510') {
                    Write-Host "NuGet push reported NU1510 (signing or trust issue). Failing build; verify package signing and source trust configuration." -ForegroundColor Red
                }
                Write-Host "NuGet push failed for $($pkg.Name) with exit code $LASTEXITCODE" -ForegroundColor Red
                exit $LASTEXITCODE
            } else {
                Write-Host "  Pushed: $($pkg.Name)" -ForegroundColor Green
            }
        }
    }
}

if ($Publish) {
    Write-Host "====================================================="
    Write-Host "Publish completed successfully!" -ForegroundColor Green
    Write-Host "Packages available in: $(Join-Path $PSScriptRoot 'publish')" -ForegroundColor Cyan
    Write-Host "Local packages copied to: $LocalNugetPath" -ForegroundColor Cyan
    Write-Host "====================================================="
} else {
    Write-Host "====================================================="
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "====================================================="
}