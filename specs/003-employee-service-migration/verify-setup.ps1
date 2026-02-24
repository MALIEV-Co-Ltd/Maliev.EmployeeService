# Verify Repository Setup
# This script verifies that Phase 1 setup completed successfully

param(
    [string]$ParentDir = "B:\maliev"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Repository Setup Verification ===" -ForegroundColor Cyan
Write-Host ""

$services = @(
    "Maliev.LeaveService",
    "Maliev.CompensationService",
    "Maliev.PerformanceService",
    "Maliev.LifecycleService",
    "Maliev.ComplianceService"
)

$allPass = $true

foreach ($service in $services) {
    $serviceDir = Join-Path $ParentDir $service
    Write-Host "Checking $service..." -ForegroundColor Yellow

    # Check directory exists
    if (-not (Test-Path $serviceDir)) {
        Write-Host "  ✗ Directory not found: $serviceDir" -ForegroundColor Red
        $allPass = $false
        continue
    }

    # Check solution file
    $slnFile = Join-Path $serviceDir "$service.sln"
    if (-not (Test-Path $slnFile)) {
        Write-Host "  ✗ Solution file not found" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "  ✓ Solution file exists" -ForegroundColor Green
    }

    # Check projects
    $expectedProjects = @("Api", "Application", "Domain", "Infrastructure", "Tests")
    foreach ($proj in $expectedProjects) {
        $projFile = Join-Path $serviceDir "$service.$proj" "$service.$proj.csproj"
        if (-not (Test-Path $projFile)) {
            Write-Host "  ✗ Project not found: $service.$proj" -ForegroundColor Red
            $allPass = $false
        } else {
            Write-Host "  ✓ Project exists: $service.$proj" -ForegroundColor Green
        }
    }

    # Check nuget.config
    $nugetConfig = Join-Path $serviceDir "nuget.config"
    if (-not (Test-Path $nugetConfig)) {
        Write-Host "  ✗ nuget.config not found" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "  ✓ nuget.config exists" -ForegroundColor Green
    }

    # Check README.md
    $readme = Join-Path $serviceDir "README.md"
    if (-not (Test-Path $readme)) {
        Write-Host "  ✗ README.md not found" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "  ✓ README.md exists" -ForegroundColor Green
    }

    # Check .gitignore
    $gitignore = Join-Path $serviceDir ".gitignore"
    if (-not (Test-Path $gitignore)) {
        Write-Host "  ✗ .gitignore not found" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "  ✓ .gitignore exists" -ForegroundColor Green
    }

    # Check .dockerignore
    $dockerignore = Join-Path $serviceDir ".dockerignore"
    if (-not (Test-Path $dockerignore)) {
        Write-Host "  ✗ .dockerignore not found" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "  ✓ .dockerignore exists" -ForegroundColor Green
    }

    Write-Host ""
}

Write-Host "=== Verification Results ===" -ForegroundColor Cyan
if ($allPass) {
    Write-Host "✓ All checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Setup is complete. You can proceed with:" -ForegroundColor Yellow
    Write-Host "  - Phase 2: Foundational tasks (ServiceDefaults integration)" -ForegroundColor White
    Write-Host "  - Phase 3+: User story implementation" -ForegroundColor White
    exit 0
} else {
    Write-Host "✗ Some checks failed. Please review the output above." -ForegroundColor Red
    exit 1
}
