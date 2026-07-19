using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

public class GetEmployeeByEmailQueryHandler
{
    private readonly IEmployeeRepository _repository;

    public GetEmployeeByEmailQueryHandler(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEmployeeByEmailResult> HandleAsync(GetEmployeeByEmailQuery request, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByEmailAsync(request.Email, cancellationToken);

        if (employee == null)
            return new GetEmployeeByEmailResult(null);

        var dto = new EmployeeLookupDto
        {
            Id = employee.Id,
            PrincipalId = employee.PrincipalId,
            Email = employee.ContactInformation.WorkEmail,
            FirstName = employee.LegalName.FirstName,
            LastName = employee.LegalName.LastName,
            DepartmentName = null, // Can fetch if needed via navigation prop
            JobTitle = employee.JobTitle,
            ManagerId = employee.ManagerId
        };

        return new GetEmployeeByEmailResult(dto);
    }
}
