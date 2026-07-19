using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.EmployeeService.Infrastructure.Scripts;
using Maliev.EmployeeService.Tests.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Scripts;

public class MigrationScriptTests : PostgreSqlIntegrationTestBase
{
    private readonly Mock<IIAMClient> _iamClientMock = new();
    private MigrateEmployeesToPrincipalsScript _script = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _script = new MigrateEmployeesToPrincipalsScript(
            Context,
            _iamClientMock.Object,
            NullLogger<MigrateEmployeesToPrincipalsScript>.Instance);
    }

    public MigrationScriptTests()
    {
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMigrateEmployeesWithoutPrincipalId()
    {
        // Arrange
        await InitializeTestAsync();

        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "MIG-001",
            LegalName = new LegalName("Emp", "One"),
            ContactInformation = new ContactInformation { WorkEmail = "emp1@example.com" },
            PrincipalId = Guid.Empty // Explicitly set to Empty for migration
        };

        Context.Employees.Add(emp1);
        await Context.SaveChangesAsync();

        var principalId1 = Guid.NewGuid();

        _iamClientMock.Setup(x => x.CreatePrincipalAsync(It.Is<CreatePrincipalRequest>(r => r.Email == emp1.ContactInformation.WorkEmail), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePrincipalResponse { PrincipalId = principalId1 });

        // Act
        await _script.ExecuteAsync();

        // Assert
        var migratedEmp1 = await Context.Employees.FindAsync(emp1.Id);
        Assert.Equal(principalId1, migratedEmp1!.PrincipalId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipAndLog_WhenIAMFails()
    {
        // Arrange
        await InitializeTestAsync();

        var empFail = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "MIG-FAIL",
            LegalName = new LegalName("Fail", "User"),
            ContactInformation = new ContactInformation { WorkEmail = "fail@example.com" },
            PrincipalId = Guid.Empty
        };

        Context.Employees.Add(empFail);
        await Context.SaveChangesAsync();

        _iamClientMock.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("IAM Error"));

        // Act
        await _script.ExecuteAsync();

        // Assert
        var resultFail = await Context.Employees.FindAsync(empFail.Id);
        Assert.Equal(Guid.Empty, resultFail!.PrincipalId);
    }
}
