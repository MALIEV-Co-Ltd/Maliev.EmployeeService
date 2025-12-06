using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

/// <summary>
/// Unit tests for TrainingRecord domain entity (T287)
/// Tests certification expiration status calculation logic (Valid, Expiring, Expired)
/// </summary>
public class TrainingRecordTests
{
    [Fact]
    public void UpdateStatus_NoExpirationDate_SetsStatusToValid()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Permanent Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-6),
            ExpirationDate = null,
            TrainingType = TrainingType.Voluntary,
            Status = CertificationStatus.Expired // Initial state
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Valid, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_ExpirationMoreThan60DaysAway_SetsStatusToValid()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Long Valid Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-6),
            ExpirationDate = DateTime.UtcNow.AddDays(90),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Expiring // Initial state
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Valid, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_ExpirationExactly60DaysAway_SetsStatusToExpiring()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Expiring Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-11),
            ExpirationDate = DateTime.UtcNow.AddDays(60),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expiration30DaysAway_SetsStatusToExpiring()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Soon Expiring Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-11),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expiration14DaysAway_SetsStatusToExpiring()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Urgently Expiring Course",
            CompletionDate = DateTime.UtcNow.AddYears(-1).AddDays(14),
            ExpirationDate = DateTime.UtcNow.AddDays(14),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expiration1DayAway_SetsStatusToExpiring()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Tomorrow Expiring Course",
            CompletionDate = DateTime.UtcNow.AddYears(-1).AddDays(1),
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_ExpirationToday_SetsStatusToExpiring()
    {
        // Arrange
        var expirationDate = DateTime.UtcNow.Date.AddHours(23); // Later today
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Expiring Today",
            CompletionDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = expirationDate,
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expired1DayAgo_SetsStatusToExpired()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Recently Expired",
            CompletionDate = DateTime.UtcNow.AddYears(-1).AddDays(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expired, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expired30DaysAgo_SetsStatusToExpired()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Long Expired",
            CompletionDate = DateTime.UtcNow.AddYears(-2),
            ExpirationDate = DateTime.UtcNow.AddDays(-30),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expired, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_Expired1YearAgo_SetsStatusToExpired()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Very Old Expired",
            CompletionDate = DateTime.UtcNow.AddYears(-3),
            ExpirationDate = DateTime.UtcNow.AddYears(-1),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expired, trainingRecord.Status);
    }

    [Theory]
    [InlineData(61)] // Just outside expiring threshold
    [InlineData(100)]
    [InlineData(365)]
    public void UpdateStatus_ExpirationBeyond60Days_RemainsValid(int daysUntilExpiration)
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Valid Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-6),
            ExpirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Expiring
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Valid, trainingRecord.Status);
    }

    [Theory]
    [InlineData(60)] // Boundary: exactly 60 days
    [InlineData(45)]
    [InlineData(30)]
    [InlineData(14)]
    [InlineData(1)]
    public void UpdateStatus_ExpirationWithin60Days_BecomesExpiring(int daysUntilExpiration)
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Expiring Course",
            CompletionDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }

    [Theory]
    [InlineData(-1)] // Just expired
    [InlineData(-14)]
    [InlineData(-30)]
    [InlineData(-365)]
    public void UpdateStatus_ExpirationInPast_BecomesExpired(int daysUntilExpiration)
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Expired Course",
            CompletionDate = DateTime.UtcNow.AddYears(-2),
            ExpirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration),
            TrainingType = TrainingType.Mandatory,
            Status = CertificationStatus.Valid
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expired, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_CalledMultipleTimes_UpdatesCorrectly()
    {
        // Arrange
        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Dynamic Status Course",
            CompletionDate = DateTime.UtcNow.AddMonths(-6),
            ExpirationDate = DateTime.UtcNow.AddDays(100),
            TrainingType = TrainingType.Mandatory
        };

        // Act & Assert - Initially Valid
        trainingRecord.UpdateStatus();
        Assert.Equal(CertificationStatus.Valid, trainingRecord.Status);

        // Simulate time passing - becomes Expiring
        trainingRecord.ExpirationDate = DateTime.UtcNow.AddDays(30);
        trainingRecord.UpdateStatus();
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);

        // Simulate more time passing - becomes Expired
        trainingRecord.ExpirationDate = DateTime.UtcNow.AddDays(-5);
        trainingRecord.UpdateStatus();
        Assert.Equal(CertificationStatus.Expired, trainingRecord.Status);
    }

    [Fact]
    public void UpdateStatus_BoundaryCase_ExactlyAtMidnight_CalculatesCorrectly()
    {
        // Arrange
        var midnightToday = DateTime.UtcNow.Date;
        var expirationAt60Days = midnightToday.AddDays(60);

        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            CourseName = "Boundary Test",
            CompletionDate = midnightToday.AddYears(-1),
            ExpirationDate = expirationAt60Days,
            TrainingType = TrainingType.Mandatory
        };

        // Act
        trainingRecord.UpdateStatus();

        // Assert
        Assert.Equal(CertificationStatus.Expiring, trainingRecord.Status);
    }
}
