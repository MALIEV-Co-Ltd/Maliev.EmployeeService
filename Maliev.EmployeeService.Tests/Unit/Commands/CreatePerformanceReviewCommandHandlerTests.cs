using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for CreatePerformanceReviewCommandHandler
/// Tests authorization, validation, and performance review creation
/// </summary>
public class CreatePerformanceReviewCommandHandlerTests
{
    private readonly Mock<IPerformanceReviewRepository> _mockPerformanceReviewRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly CreatePerformanceReviewCommandHandler _handler;

    public CreatePerformanceReviewCommandHandlerTests()
    {
        _mockPerformanceReviewRepository = new Mock<IPerformanceReviewRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new CreatePerformanceReviewCommandHandler(
            _mockPerformanceReviewRepository.Object,
            _mockEmployeeRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldCreatePerformanceReview()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId, // Set reviewer as manager
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddMonths(-12),
            DateTime.UtcNow,
            "Employee self-assessment: Met all key objectives this year");

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        _mockPerformanceReviewRepository.Setup(x => x.GetByEmployeeAndPeriodAsync(
                employeeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        _mockPerformanceReviewRepository.Setup(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.PerformanceReviewId);
        Assert.Null(result.ErrorMessage);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(
            It.Is<PerformanceReview>(pr =>
                pr.EmployeeId == employeeId &&
                pr.ReviewerId == reviewerId &&
                pr.ReviewCycle == ReviewCycle.Annual &&
                pr.SelfAssessment == "Employee self-assessment: Met all key objectives this year" &&
                pr.Status == "Draft"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow,
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PerformanceReviewId);
        Assert.Contains("not found", result.ErrorMessage);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInactivEmployee_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Terminated,
            StartDate = DateTime.UtcNow.AddYears(-2),
            TerminationDate = DateTime.UtcNow.AddMonths(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddMonths(-12),
            DateTime.UtcNow,
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PerformanceReviewId);
        Assert.Contains("Cannot create performance review for inactive employee", result.ErrorMessage);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDateRange_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        // ReviewPeriodEnd is BEFORE ReviewPeriodStart - invalid
        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.SemiAnnual,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(-6),
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PerformanceReviewId);
        Assert.Contains("Review period end date must be after start date", result.ErrorMessage);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithQuarterlyReview_ShouldCreateSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Quarterly,
            DateTime.UtcNow.AddMonths(-3),
            DateTime.UtcNow,
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        _mockPerformanceReviewRepository.Setup(x => x.GetByEmployeeAndPeriodAsync(
                employeeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        _mockPerformanceReviewRepository.Setup(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.PerformanceReviewId);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(
            It.Is<PerformanceReview>(pr =>
                pr.ReviewCycle == ReviewCycle.Quarterly &&
                pr.Status == "Draft"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithSemiAnnualReview_ShouldCreateSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.SemiAnnual,
            DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow,
            "Self-assessment for H1");

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        _mockPerformanceReviewRepository.Setup(x => x.GetByEmployeeAndPeriodAsync(
                employeeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        _mockPerformanceReviewRepository.Setup(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(
            It.Is<PerformanceReview>(pr =>
                pr.ReviewCycle == ReviewCycle.SemiAnnual),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCurrentUserAsCreatedBy()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(currentUserId);

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddMonths(-12),
            DateTime.UtcNow,
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        _mockPerformanceReviewRepository.Setup(x => x.GetByEmployeeAndPeriodAsync(
                employeeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        _mockPerformanceReviewRepository.Setup(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(
            It.Is<PerformanceReview>(pr => pr.CreatedBy == currentUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullSelfAssessment_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP007",
            EmploymentStatus = EmploymentStatus.Active,
            ManagerId = reviewerId,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new Employee
        {
            Id = reviewerId,
            EmployeeNumber = "MGR007",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            reviewerId,
            ReviewCycle.Annual,
            DateTime.UtcNow.AddMonths(-12),
            DateTime.UtcNow,
            null);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewer);

        _mockPerformanceReviewRepository.Setup(x => x.GetByEmployeeAndPeriodAsync(
                employeeId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PerformanceReview>());

        _mockPerformanceReviewRepository.Setup(x => x.AddAsync(It.IsAny<PerformanceReview>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);

        _mockPerformanceReviewRepository.Verify(x => x.AddAsync(
            It.Is<PerformanceReview>(pr => pr.SelfAssessment == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
