using System.Security.Claims;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Maliev.EmployeeService.Infrastructure.Authentication;

/// <summary>
/// Implementation of current user service using HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? EmployeeId
    {
        get
        {
            var employeeIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst("employee_id")?.Value;

            return employeeIdClaim != null && Guid.TryParse(employeeIdClaim, out var id)
                ? id
                : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Email)?.Value;

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();

    public Role PrimaryRole
    {
        get
        {
            var roles = Roles.ToList();

            // Priority order: SystemAdministrator > HRSpecialist > HRGeneralist > Manager > Employee
            if (roles.Contains("SystemAdministrator")) return Role.SystemAdministrator;
            if (roles.Contains("HRSpecialist")) return Role.HRSpecialist;
            if (roles.Contains("HRGeneralist")) return Role.HRGeneralist;
            if (roles.Contains("Manager")) return Role.Manager;

            return Role.Employee; // Default
        }
    }

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
