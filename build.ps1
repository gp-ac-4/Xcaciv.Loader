#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [switch]$UseNet10 = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Test = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Publish = $false
)

# Display banner
Write-Host "====================================================="
Write-Host "Xcaciv.Loader Build Script"
Write-Host "====================================================="
if ($Publish) {
    Write-Host "Mode: Publish (building for .NET 8.0 and .NET 10.0)"
} else {
    Write-Host "Target Framework: $( if ($UseNet10) { ".NET 10.0" } else { ".NET 8.0" } )"
}
Write-Host "Run Tests: $( if ($Test) { "Yes" } else { "No" } )"
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
    
} else {
    # Standard build mode
    # Set MSBuild property for .NET 10 if requested
    $properties = @()
    if ($UseNet10) {
        $properties += "/p:UseNet10=true"
    }

    # Build the solution
    Write-Host "Building solution..." -ForegroundColor Cyan
    $buildCommand = "dotnet build --configuration Release $($properties -join " ")"
    Write-Host "Executing: $buildCommand" -ForegroundColor Gray
    Invoke-Expression $buildCommand

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
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
    } else {
        # Standard test mode
        Write-Host "Running tests..." -ForegroundColor Cyan
        $properties = @()
        if ($UseNet10) {
            $properties += "/p:UseNet10=true"
        }
        $testCommand = "dotnet test --no-build --configuration Release $($properties -join " ")"
        Write-Host "Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}

if ($Publish) {
    Write-Host "====================================================="
    Write-Host "Publish completed successfully!" -ForegroundColor Green
    Write-Host "Packages available in: $(Join-Path $PSScriptRoot 'publish')" -ForegroundColor Cyan
    Write-Host "====================================================="
} else {
    Write-Host "Build completed successfully!" -ForegroundColor Green
}