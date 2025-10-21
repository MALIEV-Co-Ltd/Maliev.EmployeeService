using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Maliev.EmployeeService.Api.Attributes;

/// <summary>
/// Custom authorization attribute that logs all access attempts to sensitive resources
/// Combines policy-based authorization with automatic audit logging
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AuditedAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _policy;
    private readonly string _resourceType;
    private readonly string _auditPurpose;

    /// <summary>
    /// Creates an audited authorization attribute
    /// </summary>
    /// <param name="policy">Authorization policy name (e.g., "RequireHROrAdmin")</param>
    /// <param name="resourceType">Type of resource being accessed (e.g., "Compensation", "Performance Review")</param>
    /// <param name="auditPurpose">Business purpose for accessing this resource</param>
    public AuditedAuthorizeAttribute(string policy, string resourceType, string auditPurpose)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        _auditPurpose = auditPurpose;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get required services
        var authorizationService = context.HttpContext.RequestServices
            .GetRequiredService<IAuthorizationService>();
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<AuditedAuthorizeAttribute>>();

        var user = context.HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var endpoint = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        try
        {
            // Perform policy-based authorization
            var authResult = await authorizationService.AuthorizeAsync(user, _policy);

            if (!authResult.Succeeded)
            {
                // Log unauthorized access attempt
                logger.LogWarning(
                    "SECURITY: Unauthorized {ResourceType} access attempt | User: {UserName} (ID: {UserId}) | " +
                    "IP: {IpAddress} | Endpoint: {Endpoint} | Purpose: {Purpose} | Policy: {Policy} | Reason: {Reason}",
                    _resourceType,
                    userName,
                    userId,
                    ipAddress,
                    endpoint,
                    _auditPurpose,
                    _policy,
                    string.Join(", ", authResult.Failure?.FailureReasons.Select(r => r.Message) ?? Enumerable.Empty<string>()));

                context.Result = new ForbidResult();
                return;
            }

            // Log successful authorized access
            logger.LogInformation(
                "AUDIT: {ResourceType} access granted | User: {UserName} (ID: {UserId}) | " +
                "IP: {IpAddress} | Endpoint: {Endpoint} | Purpose: {Purpose} | Policy: {Policy}",
                _resourceType,
                userName,
                userId,
                ipAddress,
                endpoint,
                _auditPurpose,
                _policy);

            // Set audit purpose in HttpContext for downstream audit logging
            context.HttpContext.Request.Headers["X-Audit-Purpose"] = _auditPurpose;
        }
        catch (Exception ex)
        {
            // Log authorization failure
            logger.LogError(ex,
                "SECURITY: Authorization check failed for {ResourceType} | User: {UserName} (ID: {UserId}) | " +
                "IP: {IpAddress} | Endpoint: {Endpoint}",
                _resourceType,
                userName,
                userId,
                ipAddress,
                endpoint);

            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// Specialized authorization attribute for compensation data access
/// Pre-configured with compensation-specific audit logging
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireCompensationAccessAttribute : AuditedAuthorizeAttribute
{
    /// <summary>
    /// Creates a compensation access authorization attribute
    /// </summary>
    /// <param name="purpose">Specific business purpose for accessing compensation data</param>
    public RequireCompensationAccessAttribute(string? purpose = null)
        : base("RequireHROrAdmin", "Compensation Data", purpose ?? "Compensation Administration")
    {
    }
}
