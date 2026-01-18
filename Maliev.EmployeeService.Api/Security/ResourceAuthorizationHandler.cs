using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Maliev.EmployeeService.Api.Security;

/// <summary>
/// Custom authorization handler for resource-based permissions
/// </summary>
public class ResourceAuthorizationHandler : AuthorizationHandler<ResourceAccessRequirement, ResourceAccessContext>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResourceAuthorizationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAuthorizationHandler"/> class.
    /// </summary>
    public ResourceAuthorizationHandler(
        ICurrentUserService _currentUserService,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ILogger<ResourceAuthorizationHandler> logger)
    {
        this._currentUserService = _currentUserService;
        _iamClient = iamClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement,
        ResourceAccessContext resource)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning("User is not authenticated");
            context.Fail();
            return;
        }

        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId))
        {
            _logger.LogWarning("Current user principal ID is null");
            context.Fail();
            return;
        }

        // Use IAM for permission validation
        var resourcePath = $"employee/{resource.EmployeeId}";
        var hasPermission = await _iamClient.CheckPermissionAsync(
            principalId,
            requirement.RequiredPermission,
            resourcePath);

        if (hasPermission)
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "IAM denied permission {Permission} for user {UserId} on resource {EmployeeId}",
            requirement.RequiredPermission,
            principalId,
            resource.EmployeeId);

        context.Fail();
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
