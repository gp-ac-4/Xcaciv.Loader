# Multi-Framework Targeting

This project supports both .NET 8.0 and .NET 10.0 through conditional compilation.

## Default Target

By default, the project now targets .NET 10.0.

## Building for Different Frameworks

### Using PowerShell Script

The included `build.ps1` script allows you to build for either .NET 8.0 or .NET 10.0:

```powershell
# Build for .NET 10.0 (default)
.\build.ps1

# Build for .NET 8.0
.\build.ps1 -UseNet10:$false

# Build and run tests for .NET 10.0
.\build.ps1 -Test

# Build and run tests for .NET 8.0
.\build.ps1 -UseNet10:$false -Test
```

### Using MSBuild Properties

You can also build using the `dotnet` CLI by specifying the `UseNet10` property:

```bash
# Build for .NET 10.0 (default)
dotnet build

# Build for .NET 8.0
dotnet build /p:UseNet10=false

# Test for .NET 10.0
dotnet test

# Test for .NET 8.0
dotnet test /p:UseNet10=false
```

## How It Works

The project files use the following pattern to conditionally select the target framework:

```xml
<PropertyGroup>
  <!-- Default to .NET 10.0 now -->
  <TargetFramework>net8.0</TargetFramework>
  <!-- Define a property to conditionally switch to .NET 10 -->
  <UseNet10 Condition="'$(UseNet10)' == ''">false</UseNet10>
  <TargetFramework Condition="'$(UseNet10)' == 'true'">net10.0</TargetFramework>
</PropertyGroup>
```

This approach allows for simple switching between target frameworks without having to modify the project files directly.