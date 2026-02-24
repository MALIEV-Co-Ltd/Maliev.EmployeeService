using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

public class UpdateEmployeeCommandHandler
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UpdateEmployeeCommandResult> HandleAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(command.EmployeeId, cancellationToken);

        if (employee == null)
            return new UpdateEmployeeCommandResult(false, "Employee not found");

        if (command.Title != null)
            employee.JobTitle = command.Title;

        if (command.Phone != null)
            employee.ContactInformation.MobilePhone = command.Phone;

        if (command.Status != null)
        {
            if (Enum.TryParse<EmploymentStatus>(command.Status, ignoreCase: true, out var status))
            {
                employee.EmploymentStatus = status;
            }
            else
            {
                return new UpdateEmployeeCommandResult(false, $"Invalid employment status: {command.Status}");
            }
        }

        employee.ModifiedDate = DateTime.UtcNow;

        _repository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} updated by HR", command.EmployeeId);
        return new UpdateEmployeeCommandResult(true);
    }
}
