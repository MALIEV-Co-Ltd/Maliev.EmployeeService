# PowerShell script to run OWASP ZAP security scan
# Prerequisites:
# - OWASP ZAP installed
# - Service running on localhost:8080
# - Valid JWT token for authentication

param(
    [string]$ZapPath = "C:\Program Files\ZAP\Zed Attack Proxy\zap.bat",
    [string]$TargetUrl = "http://localhost:8080/employeeservice",
    [string]$JwtToken = "",
    [string]$ReportDir = ".\security-reports",
    [switch]$QuickScan = $false
)

# Colors for output
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Cyan "==================================="
Write-ColorOutput Cyan "OWASP ZAP Security Scan"
Write-ColorOutput Cyan "==================================="
Write-Output ""

# Validate ZAP installation
if (-not (Test-Path $ZapPath)) {
    Write-ColorOutput Red "Error: OWASP ZAP not found at $ZapPath"
    Write-Output "Please install OWASP ZAP from https://www.zaproxy.org/download/"
    exit 1
}

# Create report directory if it doesn't exist
if (-not (Test-Path $ReportDir)) {
    New-Item -ItemType Directory -Path $ReportDir | Out-Null
    Write-ColorOutput Green "Created report directory: $ReportDir"
}

# Check if service is running
try {
    $response = Invoke-WebRequest -Uri "$TargetUrl/liveness" -UseBasicParsing -TimeoutSec 5
    Write-ColorOutput Green "Service is running and accessible"
} catch {
    Write-ColorOutput Red "Error: Service is not accessible at $TargetUrl"
    Write-Output "Please ensure the Employee Service is running"
    exit 1
}

# Validate JWT token
if ([string]::IsNullOrEmpty($JwtToken)) {
    Write-ColorOutput Yellow "Warning: No JWT token provided. Some endpoints may not be accessible."
    Write-Output "Provide token with -JwtToken parameter for complete testing"
}

# Set environment variables
$env:JWT_TOKEN = $JwtToken
$env:TARGET_URL = $TargetUrl
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$env:TIMESTAMP = $timestamp

Write-Output ""
Write-ColorOutput Cyan "Scan Configuration:"
Write-Output "  Target URL: $TargetUrl"
Write-Output "  Report Directory: $ReportDir"
Write-Output "  Scan Mode: $(if ($QuickScan) {'Quick'} else {'Full'})"
Write-Output "  Timestamp: $timestamp"
Write-Output ""

if ($QuickScan) {
    # Quick scan - spider + passive scan only
    Write-ColorOutput Yellow "Running Quick Scan (Spider + Passive Scan)..."

    & $ZapPath -cmd `
        -quickurl $TargetUrl `
        -quickout "$ReportDir\zap-quickscan-$timestamp.html" `
        -quickprogress

} else {
    # Full scan using automation framework
    Write-ColorOutput Yellow "Running Full Scan (Spider + Passive + Active Scan)..."

    & $ZapPath -cmd `
        -autorun ".\owasp-zap-config.yaml" `
        -autogenmin ".\security-reports\zap-report-min-$timestamp.html" `
        -autogenmax ".\security-reports\zap-report-full-$timestamp.html" `
        -autogenxml ".\security-reports\zap-report-$timestamp.xml" `
        -autogenmd ".\security-reports\zap-report-$timestamp.md"
}

Write-Output ""

if ($LASTEXITCODE -eq 0) {
    Write-ColorOutput Green "==================================="
    Write-ColorOutput Green "Scan completed successfully!"
    Write-ColorOutput Green "==================================="
    Write-Output ""
    Write-Output "Reports generated in: $ReportDir"
    Write-Output "  - HTML Report: zap-report-$timestamp.html"
    Write-Output "  - JSON Report: zap-report-$timestamp.json"
    Write-Output "  - XML Report: zap-report-$timestamp.xml"
    Write-Output "  - Markdown Report: zap-report-$timestamp.md"
    Write-Output ""
    Write-ColorOutput Yellow "Next Steps:"
    Write-Output "  1. Review the HTML report for detailed findings"
    Write-Output "  2. Address High and Medium severity issues"
    Write-Output "  3. Re-run scan to verify fixes"
    Write-Output ""
} else {
    Write-ColorOutput Red "==================================="
    Write-ColorOutput Red "Scan failed with exit code $LASTEXITCODE"
    Write-ColorOutput Red "==================================="
    Write-Output ""
    Write-Output "Check ZAP logs for details"
    exit $LASTEXITCODE
}
