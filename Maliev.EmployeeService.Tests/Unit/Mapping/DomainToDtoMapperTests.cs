using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Mapping;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Mapping;

public class DomainToDtoMapperTests
{
    [Fact]
    public void ToEmployeeProfileDto_WithFullEmployee_ShouldMapCorrectly()
    {
        var employee = CreateTestEmployee();

        var result = employee.ToEmployeeProfileDto();

        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.EmployeeNumber, result.EmployeeNumber);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("Johnny", result.PreferredName);
        Assert.Equal("US", result.Nationality);
        Assert.Equal("john.doe@company.com", result.WorkEmail);
        Assert.Equal("john@personal.com", result.PersonalEmail);
        Assert.Equal("+1234567890", result.MobilePhone);
        Assert.Equal("FullTime", result.EmploymentType);
        Assert.Equal("Active", result.EmploymentStatus);
        Assert.Equal("Software Engineer", result.JobTitle);
    }

    [Fact]
    public void ToEmployeeProfileDto_WithEmergencyContacts_ShouldMapCorrectly()
    {
        var employee = CreateTestEmployee();
        var emergencyContacts = new List<EmergencyContact>
        {
            new EmergencyContact
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ContactName = "Jane Doe",
                Relationship = "Spouse",
                PhoneNumber = "+0987654321",
                Email = "jane@email.com",
                PriorityOrder = 1
            }
        };

        var result = employee.ToEmployeeProfileDto(emergencyContacts);

        Assert.Single(result.EmergencyContacts);
        Assert.Equal("Jane Doe", result.EmergencyContacts[0].ContactName);
        Assert.Equal("Spouse", result.EmergencyContacts[0].Relationship);
    }

    [Fact]
    public void ToEmployeeProfileDto_WithNullEmergencyContacts_ShouldReturnEmptyList()
    {
        var employee = CreateTestEmployee();

        var result = employee.ToEmployeeProfileDto(null);

        Assert.Empty(result.EmergencyContacts);
    }

    [Fact]
    public void ToEmergencyContactDto_ShouldMapCorrectly()
    {
        var emergencyContact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            ContactName = "Jane Doe",
            Relationship = "Spouse",
            PhoneNumber = "+0987654321",
            Email = "jane@email.com",
            PriorityOrder = 1
        };

        var result = emergencyContact.ToEmergencyContactDto();

        Assert.Equal(emergencyContact.Id, result.Id);
        Assert.Equal(emergencyContact.EmployeeId, result.EmployeeId);
        Assert.Equal("Jane Doe", result.ContactName);
        Assert.Equal("Spouse", result.Relationship);
        Assert.Equal("+0987654321", result.PhoneNumber);
        Assert.Equal("jane@email.com", result.Email);
        Assert.Equal(1, result.PriorityOrder);
    }

    [Fact]
    public void ToEmployeeSearchItemDto_WithFullEmployee_ShouldMapCorrectly()
    {
        var employee = CreateTestEmployee();

        var result = employee.ToEmployeeSearchItemDto();

        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.EmployeeNumber, result.EmployeeNumber);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john.doe@company.com", result.Email);
        Assert.Equal("Software Engineer", result.Title);
    }

    [Fact]
    public void ToEmployeeSearchItemDto_WithNullLegalName_ShouldReturnEmptyOrSpace()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            ContactInformation = new ContactInformation("test@company.com")
        };

        var result = employee.ToEmployeeSearchItemDto();

        Assert.Equal(" ", result.FullName);
    }

    [Fact]
    public void ToEmployeeSearchItemDto_WithNullContactInformation_ShouldReturnEmptyEmail()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe")
        };

        var result = employee.ToEmployeeSearchItemDto();

        Assert.Equal(string.Empty, result.Email);
    }

    private static Employee CreateTestEmployee()
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe"),
            PreferredName = "Johnny",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "US",
            ContactInformation = new ContactInformation(
                workEmail: "john.doe@company.com",
                mobilePhone: "+1234567890",
                personalEmail: "john@personal.com"
            ),
            EmploymentType = EmploymentType.FullTime,
            EmploymentStatus = EmploymentStatus.Active,
            JobTitle = "Software Engineer",
            StartDate = DateTime.UtcNow.AddYears(-2)
        };
    }
}
