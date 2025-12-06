using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for mandatory training auto-assignment on employee creation (T288)
/// Tests the complete workflow from CreateEmployeeCommand to automatic training assignment
/// </summary>
public class MandatoryTrainingAutoAssignmentTests : PostgreSqlIntegrationTestBase
{


    [Fact]
    public async Task CreateEmployee_WithFullTimeEmploymentType_AutoAssignsMandatoryTraining()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            Description = "Engineering Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);

        var requirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Safety Training,Code of Conduct,Data Privacy",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.MandatoryTrainingRequirements.Add(requirement);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            WorkEmail = "john.doe@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Software Engineer",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act - Create employee
        var result = await createEmployeeHandler.HandleAsync(createCommand);

        // Act - Auto-assign mandatory training
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);

        Assert.NotNull(assignedCourses);
        Assert.Equal(3, assignedCourses.Count());
        Assert.Contains("Safety Training", assignedCourses);
        Assert.Contains("Code of Conduct", assignedCourses);
        Assert.Contains("Data Privacy", assignedCourses);
    }

    [Fact]
    public async Task CreateEmployee_WithContractorType_AssignsDifferentMandatoryTraining()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Consulting",
            Description = "Consulting Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);

        var contractorRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.Contractor,
            JobRole = null,
            RequiredCourses = "Contractor Safety,NDA Training",
            DeadlineDaysFromStart = 7,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.MandatoryTrainingRequirements.Add(contractorRequirement);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "CON001",
            FirstName = "Jane",
            LastName = "Smith",
            WorkEmail = "jane.smith@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            EmploymentType = EmploymentType.Contractor,
            JobTitle = "Consultant",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act
        var result = await createEmployeeHandler.HandleAsync(createCommand);
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.Equal(2, assignedCourses.Count());
        Assert.Contains("Contractor Safety", assignedCourses);
        Assert.Contains("NDA Training", assignedCourses);
        Assert.DoesNotContain("Safety Training", assignedCourses);
        Assert.DoesNotContain("Code of Conduct", assignedCourses);
    }

    [Fact]
    public async Task CreateEmployee_WithManagerRole_AssignsRoleSpecificAndGeneralTraining()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Management",
            Description = "Management Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);

        var generalRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Safety Training,Code of Conduct",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var managerRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = null,
            JobRole = "Manager",
            RequiredCourses = "Leadership Training,Performance Management",
            DeadlineDaysFromStart = 60,
            IsActive = true,
            Priority = 2,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        Context.MandatoryTrainingRequirements.AddRange(generalRequirement, managerRequirement);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "MGR001",
            FirstName = "Bob",
            LastName = "Manager",
            WorkEmail = "bob.manager@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Manager",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act
        var result = await createEmployeeHandler.HandleAsync(createCommand);
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.Equal(4, assignedCourses.Count());
        Assert.Contains("Safety Training", assignedCourses);
        Assert.Contains("Code of Conduct", assignedCourses);
        Assert.Contains("Leadership Training", assignedCourses);
        Assert.Contains("Performance Management", assignedCourses);
    }

    [Fact]
    public async Task CreateEmployee_WithNoMatchingRequirements_ReturnsEmptyList()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Temporary",
            Description = "Temporary Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "TMP001",
            FirstName = "Temp",
            LastName = "Worker",
            WorkEmail = "temp.worker@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            EmploymentType = EmploymentType.PartTime,
            JobTitle = "Intern",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act
        var result = await createEmployeeHandler.HandleAsync(createCommand);
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.NotNull(assignedCourses);
        Assert.Empty(assignedCourses);
    }

    [Fact]
    public async Task CreateEmployee_WithInactiveRequirement_DoesNotAssignInactiveTraining()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Operations",
            Description = "Operations Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);

        var activeRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Active Training",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var inactiveRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Inactive Training",
            DeadlineDaysFromStart = 30,
            IsActive = false, // Inactive
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        Context.MandatoryTrainingRequirements.AddRange(activeRequirement, inactiveRequirement);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "OPS001",
            FirstName = "Alice",
            LastName = "Operator",
            WorkEmail = "alice.operator@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-28),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Operator",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act
        var result = await createEmployeeHandler.HandleAsync(createCommand);
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.Single(assignedCourses);
        Assert.Contains("Active Training", assignedCourses);
        Assert.DoesNotContain("Inactive Training", assignedCourses);
    }

    [Fact]
    public async Task CreateEmployee_WithMultipleRequirementsSamePriority_AssignsAllCourses()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Security",
            Description = "Security Department",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        Context.Departments.Add(department);

        var requirement1 = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Basic Security",
            DeadlineDaysFromStart = 7,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var requirement2 = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = null,
            JobRole = "Security Officer",
            RequiredCourses = "Advanced Security,Emergency Response",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        Context.MandatoryTrainingRequirements.AddRange(requirement1, requirement2);
        await Context.SaveChangesAsync();

        // Initialize repositories and dependencies
        var employeeRepository = new Maliev.EmployeeService.Infrastructure.Repositories.EmployeeRepository(Context);
        var departmentRepository = new Maliev.EmployeeService.Infrastructure.Repositories.DepartmentRepository(Context);
        var mandatoryTrainingRepository = new Maliev.EmployeeService.Infrastructure.Repositories.MandatoryTrainingRepository(Context);
        var unitOfWork = new Maliev.EmployeeService.Infrastructure.Data.UnitOfWork(Context);
        var fakeCareerServiceClient = new FakeCareerServiceClient();
        var fakeCurrentUserService = new FakeCurrentUserService();
        var fakeEventPublisher = new FakeEventPublisher();

        // Initialize AssignMandatoryTrainingCommandHandler first
        var mockLogger2 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AssignMandatoryTrainingCommandHandler>>();
        var assignTrainingHandler = new AssignMandatoryTrainingCommandHandler(
            employeeRepository,
            mandatoryTrainingRepository,
            mockLogger2.Object);

        // Initialize CreateEmployeeCommandHandler with AssignMandatoryTrainingCommandHandler
        var mockLogger1 = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<CreateEmployeeCommandHandler>>();
        var createEmployeeHandler = new CreateEmployeeCommandHandler(
            employeeRepository,
            departmentRepository,
            fakeCareerServiceClient,
            unitOfWork,
            fakeCurrentUserService,
            fakeEventPublisher,
            mockLogger1.Object,
            assignTrainingHandler);

        var createCommand = new CreateEmployeeCommand(new CreateEmployeeDto
        {
            EmployeeNumber = "SEC001",
            FirstName = "Charlie",
            LastName = "Security",
            WorkEmail = "charlie.security@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-32),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Security Officer",
            DepartmentId = department.Id,
            StartDate = DateTime.UtcNow.Date,
            WorkLocationId = 1
        });

        // Act
        var result = await createEmployeeHandler.HandleAsync(createCommand);
        var assignCommand = new AssignMandatoryTrainingCommand { EmployeeId = result.EmployeeId!.Value };
        var assignedCourses = await assignTrainingHandler.HandleAsync(assignCommand);

        // Assert
        Assert.Equal(3, assignedCourses.Count());
        Assert.Contains("Basic Security", assignedCourses);
        Assert.Contains("Advanced Security", assignedCourses);
        Assert.Contains("Emergency Response", assignedCourses);
    }

}

// Fake implementations for testing
internal class FakeCurrentUserService : Maliev.EmployeeService.Application.Interfaces.ICurrentUserService
{
    public Guid? EmployeeId => Guid.NewGuid();
    public string? Email => "test@example.com";
    public IEnumerable<string> Roles => new[] { "HR" };
    public Role PrimaryRole => Role.HRGeneralist;
    public bool IsAuthenticated => true;

    public bool IsInRole(string role) => role == "HR";
}

internal class FakeCareerServiceClient : Maliev.EmployeeService.Application.Interfaces.ICareerServiceClient
{
    public Task<Application.DTOs.CareerService.WorkLocationDto?> GetWorkLocationByIdAsync(int locationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Application.DTOs.CareerService.WorkLocationDto?>(new Application.DTOs.CareerService.WorkLocationDto
        {
            LocationId = locationId,
            LocationName = "Test Location",
            IsActive = true
        });
    }

    public Task<Application.DTOs.CareerService.JobApplicationDto?> GetJobApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Application.DTOs.CareerService.JobApplicationDto>> GetJobApplicationsByPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateJobApplicationStatusAsync(Guid applicationId, string status, Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Application.DTOs.CareerService.JobPostingDto?> GetJobPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task NotifyEmployeeTerminatedAsync(Guid employeeId, Guid? jobPostingId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Application.DTOs.CareerService.SkillDto?> GetSkillByIdAsync(int skillId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal class FakeEventPublisher : Maliev.EmployeeService.Application.Interfaces.IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, string exchange, string routingKey, CancellationToken cancellationToken = default) where TEvent : class
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        return Task.CompletedTask;
    }
}
