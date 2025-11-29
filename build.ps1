#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [switch]$UseNet10 = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Test = $false
)

# Display banner
Write-Host "====================================================="
Write-Host "Xcaciv.Loader Build Script"
Write-Host "====================================================="
Write-Host "Target Framework: $( if ($UseNet10) { ".NET 10.0" } else { ".NET 8.0" } )"
Write-Host "Run Tests: $( if ($Test) { "Yes" } else { "No" } )"
Write-Host "====================================================="

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

# Run tests if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Cyan
    $testCommand = "dotnet test --no-build --configuration Release $($properties -join " ")"
    Write-Host "Executing: $testCommand" -ForegroundColor Gray
    Invoke-Expression $testCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "Build completed successfully!" -ForegroundColor Green