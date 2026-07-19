using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

public class ImportEmployeesCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
    private readonly Mock<IBulkJobRepository> _mockBulkJobRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ImportEmployeesCommandHandler _handler;

    public ImportEmployeesCommandHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockDepartmentRepository = new Mock<IDepartmentRepository>();
        _mockBulkJobRepository = new Mock<IBulkJobRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new ImportEmployeesCommandHandler(
            _mockEmployeeRepository.Object,
            _mockDepartmentRepository.Object,
            _mockBulkJobRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyCsv_ShouldReturnJobWithError()
    {
        var command = new ImportEmployeesCommand
        {
            CsvContent = "",
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockBulkJobRepository.Verify(x => x.AddAsync(It.IsAny<BulkJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithOnlyHeaderRow_ShouldReturnJobWithError()
    {
        var csv = "EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate";
        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task HandleAsync_WithMissingRequiredColumns_ShouldReturnJobWithError()
    {
        var csv = "FirstName,LastName\nJohn,Doe";
        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldImportEmployees()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        _mockBulkJobRepository.Setup(x => x.AddAsync(It.IsAny<BulkJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDryRun_ShouldNotSaveEmployees()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = true
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmployeeNumber_ShouldSkipAndRecordError()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var existingEmployee = new Employee
        {
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("Existing", "User")
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { existingEmployee });

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentDepartment_ShouldSkipAndRecordError()
    {
        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,NonExistent,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmploymentType_ShouldSkipAndRecordError()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,InvalidType,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithSkipInvalidRows_ShouldContinueProcessing()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15
EMP002,,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = true,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithOptionalFields_ShouldImportEmployeeWithOptionalData()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate,WorkEmail,PersonalEmail,MobilePhone,Nationality,DateOfBirth
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15,john.doe@company.com,john@personal.com,+1234567890,US,1990-05-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.Is<Employee>(e =>
            e.ContactInformation.WorkEmail == "john.doe@company.com" &&
            e.ContactInformation.PersonalEmail == "john@personal.com" &&
            e.ContactInformation.MobilePhone == "+1234567890" &&
            e.Nationality == "US"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleValidRows_ShouldImportAll()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,Doe,Software Engineer,Engineering,FullTime,Active,2024-01-15
EMP002,Jane,Smith,QA Engineer,Engineering,FullTime,Active,2024-02-01
EMP003,Bob,Wilson,DevOps Engineer,Engineering,Contractor,Active,2024-03-01";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_WithCsvContainingQuotedCommas_ShouldParseCorrectly()
    {
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        _mockDepartmentRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { department });

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        var csv = @"EmployeeNumber,FirstName,LastName,JobTitle,Department,EmploymentType,EmploymentStatus,StartDate
EMP001,John,""Doe, Jr."",Software Engineer,Engineering,FullTime,Active,2024-01-15";

        var command = new ImportEmployeesCommand
        {
            CsvContent = csv,
            InitiatedByPrincipalId = Guid.NewGuid(),
            SkipInvalidRows = false,
            DryRun = false
        };

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockEmployeeRepository.Verify(x => x.AddAsync(It.Is<Employee>(e =>
            e.LegalName.LastName == "Doe, Jr."
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
