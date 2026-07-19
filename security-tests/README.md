# Security Testing with OWASP ZAP

This directory contains OWASP ZAP (Zed Attack Proxy) security testing configuration and scripts for the Maliev Employee Service.

## Prerequisites

1. **Install OWASP ZAP**:
   - Download from https://www.zaproxy.org/download/
   - Windows: Install to `C:\Program Files\ZAP\`
   - Linux: `sudo apt install zaproxy` or download from website
   - macOS: `brew install --cask owasp-zap`

2. **Start the Employee Service**:
   ```bash
   cd Maliev.EmployeeService.Api
   dotnet run
   ```

3. **Obtain JWT Token**:
   - Authenticate against the auth service
   - Copy the JWT token for use in scans

## Running Security Scans

### Quick Scan (Recommended for Development)

Quick scan performs spider and passive scanning only (5-10 minutes):

**Windows PowerShell:**
```powershell
cd security-tests
.\run-zap-scan.ps1 -QuickScan -JwtToken "your-jwt-token"
```

**Linux/macOS:**
```bash
cd security-tests
chmod +x run-zap-scan.sh
QUICK_SCAN=true JWT_TOKEN="your-jwt-token" ./run-zap-scan.sh
```

### Full Scan (Recommended for Pre-Production)

Full scan includes active scanning for vulnerabilities (30-45 minutes):

**Windows PowerShell:**
```powershell
cd security-tests
.\run-zap-scan.ps1 -JwtToken "your-jwt-token"
```

**Linux/macOS:**
```bash
cd security-tests
JWT_TOKEN="your-jwt-token" ./run-zap-scan.sh
```

### Docker-based Scan

Run ZAP in Docker (no local installation required):

```bash
docker run -v $(pwd):/zap/wrk:rw -t owasp/zap2docker-stable zap-baseline.py \
  -t http://host.docker.internal:8080/employeeservice \
  -g gen.conf \
  -r zap-report.html
```

## Scan Configuration

The `owasp-zap-config.yaml` file defines:

- **Context**: URL patterns to include/exclude
- **Authentication**: Bearer token configuration
- **Spider Settings**: Crawl depth and parameters
- **Active Scan Rules**: SQL injection, XSS, CSRF, etc.
- **Alert Filters**: Known false positives
- **Reporting**: Output formats (HTML, JSON, XML, Markdown)

### Customizing Scan Rules

Edit `owasp-zap-config.yaml` to adjust:

```yaml
policies:
  - name: "API-Scan"
    rules:
      - id: 40018 # SQL Injection
        strength: "High"
        threshold: "Low"
```

**Strength Levels:** Low, Medium, High, Insane
**Threshold Levels:** Off, Low, Medium, High

## Understanding Results

### Severity Levels

- **High**: Critical vulnerabilities requiring immediate fix
- **Medium**: Important vulnerabilities requiring timely fix
- **Low**: Minor vulnerabilities for improvement
- **Informational**: Best practice recommendations

### Common Findings

1. **SQL Injection (40018)**
   - Verify all database queries use parameterized queries
   - Check: Entity Framework Core queries

2. **Cross-Site Scripting (40012, 40014)**
   - Verify input sanitization is working
   - Check: InputSanitizationMiddleware.cs

3. **Missing Security Headers (10021, 10020)**
   - Verify SecurityHeadersMiddleware is configured
   - Check: Program.cs middleware pipeline

4. **Weak Authentication (10105)**
   - Verify JWT token validation
   - Check: JwtSettings configuration

5. **Information Disclosure (10023)**
   - Disable debug error messages in production
   - Check: appsettings.json DetailedErrors setting

## Success Criteria (T408)

For T408 completion, the following criteria must be met:

1. **No High Severity Issues**:
   - ✅ Zero SQL injection vulnerabilities
   - ✅ Zero XSS vulnerabilities
   - ✅ Zero authentication bypass issues
   - ✅ Zero remote code execution vulnerabilities

2. **Medium Severity Issues Documented**:
   - ✅ All medium issues have mitigation plan
   - ✅ False positives are documented

3. **Security Headers Present**:
   - ✅ X-Content-Type-Options: nosniff
   - ✅ X-Frame-Options: DENY
   - ✅ Content-Security-Policy configured
   - ✅ Strict-Transport-Security enabled

4. **Input Validation**:
   - ✅ All user inputs are sanitized
   - ✅ No script injection possible
   - ✅ No SQL injection possible

## Fixing Vulnerabilities

### SQL Injection

```csharp
// ❌ Bad - vulnerable to SQL injection
var query = $"SELECT * FROM Employees WHERE Id = {employeeId}";

// ✅ Good - parameterized query (EF Core)
var employee = await _context.Employees
    .Where(e => e.Id == employeeId)
    .FirstOrDefaultAsync();
```

### Cross-Site Scripting (XSS)

```csharp
// ✅ Input sanitization is already implemented
// See: Maliev.EmployeeService.Api/Middleware/InputSanitizationMiddleware.cs

// Additional validation in DTOs
public class CreateEmployeeDto
{
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s]+$")]
    public string FirstName { get; set; }
}
```

### Security Headers

```csharp
// ✅ Already implemented in Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});
```

## Integration with CI/CD

Add security scanning to GitHub Actions:

```yaml
- name: OWASP ZAP Security Scan
  run: |
    docker run -v $(pwd):/zap/wrk:rw \
      -t owasp/zap2docker-stable \
      zap-baseline.py -t http://localhost:8080/employeeservice \
      -r zap-report.html || true

- name: Upload ZAP Report
  uses: actions/upload-artifact@v3
  with:
    name: zap-security-report
    path: zap-report.html
```

## Automated Scanning Schedule

For continuous security:

1. **Development**: Run quick scan before PR
2. **Staging**: Run full scan on release candidate
3. **Production**: Run passive scan monthly

## Troubleshooting

### Service Not Accessible

```bash
# Check if service is running
curl http://localhost:8080/employeeservice/liveness

# Check logs
dotnet run --project Maliev.EmployeeService.Api
```

### Authentication Issues

```bash
# Test JWT token
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://localhost:8080/employeeservice/api/employees/profile
```

### ZAP Not Found

**Windows:**
```powershell
# Update ZAP path
.\run-zap-scan.ps1 -ZapPath "C:\Path\To\ZAP\zap.bat"
```

**Linux/macOS:**
```bash
# Update ZAP path
ZAP_PATH="/usr/share/zaproxy/zap.sh" ./run-zap-scan.sh
```

## Additional Resources

- [OWASP ZAP Documentation](https://www.zaproxy.org/docs/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [API Security Best Practices](https://owasp.org/www-project-api-security/)
- [OWASP ZAP Automation Framework](https://www.zaproxy.org/docs/desktop/addons/automation-framework/)

## Next Steps After Testing

1. Review generated reports in `security-reports/` directory
2. Create GitHub issues for High and Medium severity findings
3. Implement fixes and re-run scan to verify
4. Document any accepted risks with business justification
5. Schedule regular security scans (monthly minimum)
