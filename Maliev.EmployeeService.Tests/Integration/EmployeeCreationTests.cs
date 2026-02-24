using System.Net;
using System.Net.Http.Json;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.TestHost;

namespace Maliev.EmployeeService.Tests.Integration;

[Collection("IntegrationTests")]
public class EmployeeCreationTests : WebApplicationTestBase
{
    private readonly Mock<IIAMClient> _iamClientMock = new();

    public EmployeeCreationTests(EmployeeServiceTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateEmployee_WithPrincipalAuthEnabled_ShouldCallIAMAndSetPrincipalId()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("Engineering");

        var dto = new CreateEmployeeDto
        {
            EmployeeNumber = "EMP-IAM-001",
            FirstName = "IAM",
            LastName = "User",
            WorkEmail = "iam.user@maliev.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            StartDate = DateTime.UtcNow.AddDays(1),
            EmploymentType = EmploymentType.FullTime,
            DepartmentId = department.Id,
            JobTitle = "Security Engineer"
        };

        var principalId = Guid.NewGuid();
        _iamClientMock.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePrincipalResponse { PrincipalId = principalId });

        // Use custom client with mocked service
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _iamClientMock.Object);

                var mockEncryptionService = new Mock<IEncryptionService>();
                mockEncryptionService.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
                mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
                services.AddSingleton(mockEncryptionService.Object);

                var mockCareerService = new Mock<ICareerServiceClient>();
                mockCareerService.Setup(x => x.GetWorkLocationByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Maliev.EmployeeService.Application.DTOs.CareerService.WorkLocationDto
                    {
                        LocationId = 1,
                        LocationName = "Test Location",
                        IsActive = true
                    });
                services.AddScoped(_ => mockCareerService.Object);

                // Enable feature flag for this test
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:PrincipalBasedAuthEnabled"] = "true"
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(configuration);
            });
        }).CreateClient();

        var token = _factory.CreateTestJwtToken(
            Guid.NewGuid().ToString(),
            new[] { "roles.employee.system-administrator" },
            new[] { "employee.profiles.create" });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/employee/v1/hr/employees", dto);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
        var employeeId = (Guid)result!.GetProperty("id").GetGuid();

        // Verify PrincipalId in DB
        var employee = await _factory.GetDbContext().Employees.FindAsync(employeeId);
        Assert.NotNull(employee);
        Assert.Equal(principalId, employee!.PrincipalId);

        _iamClientMock.Verify(x => x.CreatePrincipalAsync(
            It.Is<CreatePrincipalRequest>(r => r.Email == dto.WorkEmail && r.LinkedService == "EmployeeService"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEmployee_WhenIAMFails_ShouldReturnErrorAndNotCreateEmployee()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("Engineering");
        var dto = new CreateEmployeeDto
        {
            EmployeeNumber = "EMP-IAM-FAIL",
            FirstName = "Fail",
            LastName = "User",
            WorkEmail = "fail.user@maliev.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            StartDate = DateTime.UtcNow.AddDays(1),
            EmploymentType = EmploymentType.FullTime,
            DepartmentId = department.Id
        };

        _iamClientMock.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("IAM Service Unavailable"));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _iamClientMock.Object);

                var mockEncryptionService = new Mock<IEncryptionService>();
                mockEncryptionService.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
                mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
                services.AddSingleton(mockEncryptionService.Object);

                var mockCareerService = new Mock<ICareerServiceClient>();
                mockCareerService.Setup(x => x.GetWorkLocationByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Maliev.EmployeeService.Application.DTOs.CareerService.WorkLocationDto
                    {
                        LocationId = 1,
                        LocationName = "Test Location",
                        IsActive = true
                    });
                services.AddScoped(_ => mockCareerService.Object);

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:PrincipalBasedAuthEnabled"] = "true"
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(configuration);
            });
        }).CreateClient();

        var token = _factory.CreateTestJwtToken(
            Guid.NewGuid().ToString(),
            new[] { "roles.employee.system-administrator" },
            new[] { "employee.profiles.create" });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/employee/v1/hr/employees", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var message = result.GetProperty("message").GetString();
        Assert.Contains("Failed to create employee identity in IAM", message);

        // Verify NO employee created in DB
        var employeeCount = await _factory.GetDbContext().Employees.CountAsync(e => e.EmployeeNumber == dto.EmployeeNumber);
        Assert.Equal(0, employeeCount);
    }
}
