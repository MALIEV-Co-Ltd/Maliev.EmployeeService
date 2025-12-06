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

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAuthorizationHandler"/> class
    /// </summary>
    /// <param name="_currentUserService">The current user service</param>
    /// <param name="logger">The logger instance</param>
    public ResourceAuthorizationHandler(
        ICurrentUserService _currentUserService,
        ILogger<ResourceAuthorizationHandler> logger)
    {
        this._currentUserService = _currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement for resource access
    /// </summary>
    /// <param name="context">The authorization context</param>
    /// <param name="requirement">The resource access requirement</param>
    /// <param name="resource">The resource access context</param>
    /// <returns>A task representing the asynchronous operation</returns>
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
    /// <summary>
    /// Gets the required permission for resource access
    /// </summary>
    public string RequiredPermission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAccessRequirement"/> class
    /// </summary>
    /// <param name="requiredPermission">The required permission</param>
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
    /// <summary>
    /// Gets or sets the ID of the employee owning the resource
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the manager of the employee
    /// </summary>
    public Guid? ManagerId { get; set; }
}
