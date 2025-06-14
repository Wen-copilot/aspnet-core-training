param(
    [Parameter(Mandatory=$true)]
    [string]$ExerciseName
)

# Set colors for output
$Blue = [ConsoleColor]::Blue
$Green = [ConsoleColor]::Green
$Red = [ConsoleColor]::Red
$Yellow = [ConsoleColor]::Yellow
$NC = [ConsoleColor]::White

# Set project name based on exercise
$ProjectName = switch ($ExerciseName) {
    "exercise01-basic-api" { "RestfulAPI" }
    "exercise02-authentication" { "RestfulAPI" }
    "exercise03-documentation" { "RestfulAPI" }
    "module04-exercise01-jwt" { "JwtAuthenticationAPI" }
    "module05-exercise01-efcore" { "EFCoreDemo" }
    "module05-exercise02-linq" { "EFCoreDemo" }
    "module05-exercise03-repository" { "EFCoreDemo" }
    default { "LibraryAPI" }
}

Write-Host "🚀 Setting up $ExerciseName" -ForegroundColor $Blue
Write-Host "=================================="

# Step 1: Create project
Write-Host -NoNewline "1. Creating Web API project... "

# Check if project already exists
if (Test-Path $ProjectName) {
    Write-Host "⚠️  Project '$ProjectName' already exists!" -ForegroundColor Yellow
    $response = Read-Host "Do you want to overwrite it? This will delete all existing work! (y/N)"
    if ($response -notmatch "^[Yy]$") {
        Write-Host "Setup cancelled. Existing project preserved." -ForegroundColor Red
        exit 1
    }
    Write-Host -NoNewline "Creating backup... "
    $backupName = "${ProjectName}_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    if (Test-Path $backupName) {
        Remove-Item -Path $backupName -Recurse -Force
    }
    Rename-Item -Path $ProjectName -NewName $backupName
    Write-Host "✓" -ForegroundColor Green
}

try {
    dotnet new webapi -n $ProjectName --framework net8.0 | Out-Null
    Write-Host "✓" -ForegroundColor $Green
}
catch {
    Write-Host "✗" -ForegroundColor $Red
    Write-Host "Failed to create project. Trying with --force flag..."
    try {
        dotnet new webapi -n $ProjectName --framework net8.0 --force | Out-Null
        Write-Host "✓" -ForegroundColor $Green
    }
    catch {
        Write-Host "✗" -ForegroundColor $Red
        exit 1
    }
}

Set-Location $ProjectName

# Step 2: Install packages with specific versions
Write-Host -NoNewline "2. Applying package versions... "

try {
    switch ($ExerciseName) {
        "exercise01-basic-api" {
            dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11 | Out-Null
            dotnet add package Swashbuckle.AspNetCore --version 6.8.1 | Out-Null
            dotnet add package Microsoft.AspNetCore.OpenApi --version 8.0.11 | Out-Null
        }
        "exercise02-authentication" {
            dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11 | Out-Null
            dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11 | Out-Null
            dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11 | Out-Null
            dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.2 | Out-Null
            dotnet add package Swashbuckle.AspNetCore --version 6.8.1 | Out-Null
        }
        "exercise03-documentation" {
            dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11 | Out-Null
            dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11 | Out-Null
            dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11 | Out-Null
            dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.2 | Out-Null
            dotnet add package Swashbuckle.AspNetCore --version 6.8.1 | Out-Null
            dotnet add package Swashbuckle.AspNetCore.Annotations --version 6.8.1 | Out-Null
            dotnet add package Asp.Versioning.Mvc --version 8.0.0 | Out-Null
            dotnet add package Asp.Versioning.Mvc.ApiExplorer --version 8.0.0 | Out-Null
            dotnet add package AspNetCore.HealthChecks.UI --version 9.0.0 | Out-Null
            dotnet add package AspNetCore.HealthChecks.UI.Client --version 9.0.0 | Out-Null
            dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage --version 9.0.0 | Out-Null
        }
    }
    Write-Host "✓" -ForegroundColor $Green
}
catch {
    Write-Host "✗" -ForegroundColor $Red
    exit 1
}

# Step 3: Restore packages
Write-Host -NoNewline "3. Restoring packages... "
try {
    dotnet restore | Out-Null
    Write-Host "✓" -ForegroundColor $Green
}
catch {
    Write-Host "✗" -ForegroundColor $Red
    exit 1
}

# Step 4: Verify build
Write-Host -NoNewline "4. Verifying build... "
try {
    dotnet build --nologo --verbosity quiet | Out-Null
    Write-Host "✓" -ForegroundColor $Green
}
catch {
    Write-Host "✗" -ForegroundColor $Red
    Write-Host "Build failed. Check the output:"
    dotnet build --nologo --verbosity minimal
    exit 1
}

Write-Host ""
Write-Host "🎉 Exercise setup complete!" -ForegroundColor $Green
Write-Host "📁 Project created in: $((Get-Location).Path)" -ForegroundColor $Blue
Write-Host ""
Write-Host "💡 Next steps:" -ForegroundColor $Yellow
Write-Host "1. Follow the exercise instructions"
Write-Host "2. Run 'dotnet run' to start the application"
Write-Host ""