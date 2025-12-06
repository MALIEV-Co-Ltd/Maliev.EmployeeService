using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Mapping;

/// <summary>
/// Extension methods for mapping domain entities to DTOs
/// </summary>
public static class DomainToDtoMapper
{
    /// <summary>
    /// Maps an Employee entity to EmployeeProfileDto
    /// </summary>
    public static EmployeeProfileDto ToEmployeeProfileDto(
        this Employee employee,
        IEnumerable<EmergencyContact>? emergencyContacts = null)
    {
        return new EmployeeProfileDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,

            // Legal Name
            FirstName = employee.LegalName.FirstName,
            LastName = employee.LegalName.LastName,
            MiddleName = employee.LegalName.MiddleName,
            FullName = employee.FullName,

            // Personal Information
            PreferredName = employee.PreferredName,
            DateOfBirth = employee.DateOfBirth,
            Age = employee.Age,
            Nationality = employee.Nationality,

            // Contact Information
            WorkEmail = employee.ContactInformation.WorkEmail,
            PersonalEmail = employee.ContactInformation.PersonalEmail,
            MobilePhone = employee.ContactInformation.MobilePhone,

            // Employment Information
            EmploymentType = employee.EmploymentType.ToString(),
            EmploymentStatus = employee.EmploymentStatus.ToString(),
            JobTitle = employee.JobTitle,
            WorkLocation = employee.WorkLocation,

            // Employment Dates
            StartDate = employee.StartDate,
            ProbationEndDate = employee.ProbationEndDate,
            TerminationDate = employee.TerminationDate,
            TenureInYears = employee.TenureInYears,

            // Department and Manager
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager?.FullName,

            // Emergency Contacts
            EmergencyContacts = emergencyContacts?
                .Select(ec => ec.ToEmergencyContactDto())
                .OrderBy(ec => ec.PriorityOrder)
                .ToList() ?? new List<EmergencyContactDto>()
        };
    }

    /// <summary>
    /// Maps an EmergencyContact entity to EmergencyContactDto
    /// </summary>
    public static EmergencyContactDto ToEmergencyContactDto(this EmergencyContact emergencyContact)
    {
        return new EmergencyContactDto
        {
            Id = emergencyContact.Id,
            EmployeeId = emergencyContact.EmployeeId,
            ContactName = emergencyContact.ContactName,
            Relationship = emergencyContact.Relationship,
            PhoneNumber = emergencyContact.PhoneNumber,
            Email = emergencyContact.Email,
            PriorityOrder = emergencyContact.PriorityOrder
        };
    }

    /// <summary>
    /// Maps an Employee entity to EmployeeSearchItemDto
    /// </summary>
    public static EmployeeSearchItemDto ToEmployeeSearchItemDto(this Employee employee)
    {
        return new EmployeeSearchItemDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.LegalName?.FullName ?? "Unknown",
            PreferredName = employee.PreferredName,
            Email = employee.ContactInformation?.WorkEmail ?? string.Empty,
            Title = employee.JobTitle,
            DepartmentName = employee.Department?.Name,
            DepartmentId = employee.DepartmentId,
            ManagerName = employee.Manager?.LegalName?.FullName,
            ManagerId = employee.ManagerId,
            EmploymentStatus = employee.EmploymentStatus.ToString(),
            EmploymentType = employee.EmploymentType.ToString(),
            HireDate = employee.StartDate,
            TerminationDate = employee.TerminationDate
        };
    }
}
