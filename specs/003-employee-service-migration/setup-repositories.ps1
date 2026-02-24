# Employee Service Decomposition - Repository Setup Script
# This script automates Phase 1 tasks (T001-T053)
# Prerequisites: GitHub CLI (gh) installed and authenticated

param(
    [string]$Organization = "MALIEV-Co-Ltd",
    [string]$ParentDir = "B:\maliev",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "=== Employee Service Decomposition - Repository Setup ===" -ForegroundColor Cyan
Write-Host ""

# Service definitions
$services = @(
    @{Name = "Maliev.LeaveService"; Description = "Leave management microservice"},
    @{Name = "Maliev.CompensationService"; Description = "Compensation and benefits microservice"},
    @{Name = "Maliev.PerformanceService"; Description = "Performance reviews and goals microservice"},
    @{Name = "Maliev.LifecycleService"; Description = "Onboarding and offboarding microservice"},
    @{Name = "Maliev.ComplianceService"; Description = "Work authorization compliance microservice"}
)

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $ghVersion = gh --version 2>$null
    Write-Host "✓ GitHub CLI installed: $($ghVersion[0])" -ForegroundColor Green
} catch {
    Write-Host "✗ GitHub CLI not found. Install from https://cli.github.com/" -ForegroundColor Red
    exit 1
}

try {
    $ghAuth = gh auth status 2>&1
    if ($ghAuth -match "Logged in") {
        Write-Host "✓ GitHub CLI authenticated" -ForegroundColor Green
    } else {
        Write-Host "✗ GitHub CLI not authenticated. Run: gh auth login" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ GitHub CLI not authenticated. Run: gh auth login" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Function to create repository
function New-ServiceRepository {
    param($ServiceName, $Description)

    Write-Host "Creating repository: $ServiceName..." -ForegroundColor Cyan

    if ($DryRun) {
        Write-Host "[DRY RUN] Would create: $Organization/$ServiceName" -ForegroundColor Yellow
        return
    }

    try {
        # Create repository
        gh repo create "$Organization/$ServiceName" `
            --private `
            --description $Description `
            --add-readme `
            --gitignore "VisualStudio" `
            --license "mit"

        Write-Host "✓ Repository created: $Organization/$ServiceName" -ForegroundColor Green

        # Create CODEOWNERS file
        $codeownersContent = "* @$Organization/core-developers"
        gh api "/repos/$Organization/$ServiceName/contents/.github/CODEOWNERS" `
            -X PUT `
            -f message="Add CODEOWNERS file" `
            -f content="$(([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($codeownersContent))))"

        Write-Host "✓ CODEOWNERS file created" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create repository: $_" -ForegroundColor Red
        throw
    }
}

# Function to clone and scaffold project
function Initialize-ServiceProject {
    param($ServiceName)

    $serviceDir = Join-Path $ParentDir $ServiceName

    Write-Host ""
    Write-Host "Initializing project: $ServiceName..." -ForegroundColor Cyan

    if ($DryRun) {
        Write-Host "[DRY RUN] Would clone to: $serviceDir" -ForegroundColor Yellow
        return
    }

    try {
        # Clone repository
        if (Test-Path $serviceDir) {
            Write-Host "⚠ Directory already exists: $serviceDir" -ForegroundColor Yellow
            Write-Host "  Skipping clone..." -ForegroundColor Yellow
        } else {
            Write-Host "Cloning repository..."
            git clone "https://github.com/$Organization/$ServiceName.git" $serviceDir
            Write-Host "✓ Repository cloned" -ForegroundColor Green
        }

        Set-Location $serviceDir

        # Create solution file
        Write-Host "Creating solution file..."
        dotnet new sln -n $ServiceName
        Write-Host "✓ Solution created" -ForegroundColor Green

        # Create projects
        $projects = @(
            @{Type = "webapi"; Name = "$ServiceName.Api"},
            @{Type = "classlib"; Name = "$ServiceName.Application"},
            @{Type = "classlib"; Name = "$ServiceName.Domain"},
            @{Type = "classlib"; Name = "$ServiceName.Infrastructure"},
            @{Type = "xunit"; Name = "$ServiceName.Tests"}
        )

        foreach ($project in $projects) {
            Write-Host "Creating project: $($project.Name)..."
            dotnet new $($project.Type) -n $($project.Name) -f net10.0
            dotnet sln add "$($project.Name)/$($project.Name).csproj"
            Write-Host "✓ Project created: $($project.Name)" -ForegroundColor Green
        }

        # Create nuget.config
        Write-Host "Creating nuget.config..."
        $nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="github" value="https://nuget.pkg.github.com/$Organization/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="%NUGET_USERNAME%" />
      <add key="ClearTextPassword" value="%NUGET_PASSWORD%" />
    </github>
  </packageSourceCredentials>
</configuration>
"@
        Set-Content -Path "nuget.config" -Value $nugetConfig
        Write-Host "✓ nuget.config created" -ForegroundColor Green

        # Create .gitignore (if not exists)
        if (-not (Test-Path ".gitignore")) {
            Write-Host "Creating .gitignore..."
            $gitignore = @"
# .NET Core
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Bb]in/
[Oo]bj/

# NuGet Packages
*.nupkg
*.nuget.props
*.nuget.targets

# Test Results
[Tt]est[Rr]esult*/
*.trx
*.coverage

# IDE
.vs/
.vscode/
.idea/
*.sln.iml

# Environment
.env
.env.*
*.env

# Logs
*.log

# OS
.DS_Store
Thumbs.db
"@
            Set-Content -Path ".gitignore" -Value $gitignore
            Write-Host "✓ .gitignore created" -ForegroundColor Green
        }

        # Create .dockerignore
        Write-Host "Creating .dockerignore..."
        $dockerignore = @"
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/.git/
**/.gitignore
**/README.md
**/*.log
**/.env*
**/specs/
"@
        Set-Content -Path ".dockerignore" -Value $dockerignore
        Write-Host "✓ .dockerignore created" -ForegroundColor Green

        # Create README.md
        Write-Host "Creating README.md..."
        $readme = @"
# $ServiceName

Part of the MALIEV Employee Management System microservices architecture.

## Overview

$($services | Where-Object {$_.Name -eq $ServiceName} | Select-Object -ExpandProperty Description)

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop
- Access to GitHub Packages (GITOPS_PAT with read:packages scope)

## Getting Started

1. Set environment variables:
``````bash
# Windows
set NUGET_USERNAME=your-github-username
set NUGET_PASSWORD=your-gitops-pat

# Linux/Mac
export NUGET_USERNAME=your-github-username
export NUGET_PASSWORD=your-gitops-pat
``````

2. Restore dependencies:
``````bash
dotnet restore
``````

3. Build the solution:
``````bash
dotnet build
``````

4. Run tests:
``````bash
dotnet test
``````

## Project Structure

- **$ServiceName.Api**: REST API endpoints
- **$ServiceName.Application**: Business logic and commands/queries
- **$ServiceName.Domain**: Domain entities and integration events
- **$ServiceName.Infrastructure**: Database, repositories, and external integrations
- **$ServiceName.Tests**: Integration tests with Testcontainers

## Documentation

See [specs/003-employee-service-migration](../Maliev.EmployeeService/specs/003-employee-service-migration/) for full specification.

## License

MIT
"@
        Set-Content -Path "README.md" -Value $readme
        Write-Host "✓ README.md created" -ForegroundColor Green

        # Initial commit
        Write-Host "Creating initial commit..."
        git add .
        git commit -m "Initial project structure

- Add .NET 10.0 solution and projects
- Add nuget.config for GitHub Packages
- Add README.md
- Add .gitignore and .dockerignore"
        git push origin main
        Write-Host "✓ Initial commit pushed" -ForegroundColor Green

    } catch {
        Write-Host "✗ Failed to initialize project: $_" -ForegroundColor Red
        throw
    } finally {
        Set-Location $ParentDir
    }
}

# Main execution
Write-Host "Starting repository setup..." -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "=== DRY RUN MODE ===" -ForegroundColor Yellow
    Write-Host ""
}

# Phase 1: Create repositories (T001-T005)
Write-Host "Phase 1: Creating GitHub repositories..." -ForegroundColor Magenta
foreach ($service in $services) {
    try {
        New-ServiceRepository -ServiceName $service.Name -Description $service.Description
    } catch {
        Write-Host "Failed to create repository: $($service.Name)" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Phase 2: Cloning and scaffolding projects..." -ForegroundColor Magenta

# Phase 2: Clone and scaffold (T006-T053)
foreach ($service in $services) {
    try {
        Initialize-ServiceProject -ServiceName $service.Name
        Write-Host "✓ $($service.Name) initialized successfully" -ForegroundColor Green
        Write-Host ""
    } catch {
        Write-Host "Failed to initialize project: $($service.Name)" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✓ $($services.Count) repositories created" -ForegroundColor Green
Write-Host "  ✓ $($services.Count) projects scaffolded" -ForegroundColor Green
Write-Host "  ✓ All projects have: Solution, 5 projects, nuget.config, README, .gitignore, .dockerignore" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review the generated projects in: $ParentDir" -ForegroundColor White
Write-Host "  2. Set NUGET_USERNAME and NUGET_PASSWORD environment variables" -ForegroundColor White
Write-Host "  3. Proceed with Phase 2: Foundational tasks" -ForegroundColor White
Write-Host ""
