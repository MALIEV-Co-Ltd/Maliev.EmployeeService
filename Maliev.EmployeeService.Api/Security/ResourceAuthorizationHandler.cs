using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Maliev.EmployeeService.Api.Security;

/// <summary>
/// Custom authorization handler for resource-based permissions
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourceAccessRequirement, ResourceAccessContext>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ResourceAuthorizationHandler> _logger;

    public ResourceAuthorizationHandler(
        ICurrentUserService _currentUserService,
        ILogger<ResourceAuthorizationHandler> logger)
    {
        this._currentUserService = _currentUserService;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement,
        ResourceAccessContext resource)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            context.Fail();
            return Task.CompletedTask;
        }

        var currentUserId = _currentUserService.EmployeeId;
        if (currentUserId == null)
        {
            _logger.LogWarning("Current user ID is null");
            context.Fail();
            return Task.CompletedTask;
        }

        // Admin and HR have access to all resources
        if (_currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.HR))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Manager can access direct reports
        if (_currentUserService.IsInRole(Roles.Manager))
        {
            if (resource.ManagerId == currentUserId || resource.EmployeeId == currentUserId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // Employee can only access own resources
        if (_currentUserService.IsInRole(Roles.Employee))
        {
            if (resource.EmployeeId == currentUserId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        _logger.LogWarning(
            "User {UserId} does not have permission to access resource for employee {EmployeeId}",
            currentUserId,
            resource.EmployeeId);

        context.Fail();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorization requirement for resource access
/// </summary>
public class ResourceAccessRequirement : IAuthorizationRequirement
{
    public string RequiredPermission { get; }

    public ResourceAccessRequirement(string requiredPermission)
    {
        RequiredPermission = requiredPermission;
    }
}

/// <summary>
/// Context for resource access authorization containing employee and manager IDs
/// </summary>
public class ResourceAccessContext
{
    public Guid EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
}
