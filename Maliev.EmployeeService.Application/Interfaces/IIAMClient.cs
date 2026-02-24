using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Interfaces;

public interface IIAMClient
{
    Task<CreatePrincipalResponse> CreatePrincipalAsync(
        CreatePrincipalRequest request,
        CancellationToken cancellationToken = default);

    Task DeletePrincipalAsync(
        Guid principalId,
        CancellationToken cancellationToken = default);
}
