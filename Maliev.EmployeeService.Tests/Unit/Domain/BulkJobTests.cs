using Maliev.EmployeeService.Domain.Entities;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

public class BulkJobTests
{
    [Fact]
    public void BulkJob_DefaultConstructor_ShouldSetDefaultValues()
    {
        var job = new BulkJob
        {
            JobType = "TestJob",
            InitiatedByPrincipalId = Guid.NewGuid()
        };

        Assert.NotEqual(Guid.Empty, job.JobId);
        Assert.Equal(BulkJobStatus.Pending, job.Status);
        Assert.Equal(0, job.TotalRecords);
        Assert.Equal(0, job.SuccessfulRecords);
        Assert.Equal(0, job.FailedRecords);
    }

    [Fact]
    public void ProgressPercentage_WithNoRecords_ShouldReturnZero()
    {
        var job = new BulkJob
        {
            JobType = "TestJob",
            InitiatedByPrincipalId = Guid.NewGuid(),
            TotalRecords = 0
        };

        Assert.Equal(0, job.ProgressPercentage);
    }

    [Fact]
    public void ProgressPercentage_WithPartialRecords_ShouldReturnCorrectPercentage()
    {
        var job = new BulkJob
        {
            JobType = "TestJob",
            InitiatedByPrincipalId = Guid.NewGuid(),
            TotalRecords = 100,
            SuccessfulRecords = 50,
            FailedRecords = 25
        };

        Assert.Equal(75, job.ProgressPercentage);
    }

    [Fact]
    public void ProgressPercentage_WithAllRecordsProcessed_ShouldReturn100()
    {
        var job = new BulkJob
        {
            JobType = "TestJob",
            InitiatedByPrincipalId = Guid.NewGuid(),
            TotalRecords = 100,
            SuccessfulRecords = 80,
            FailedRecords = 20
        };

        Assert.Equal(100, job.ProgressPercentage);
    }

    [Fact]
    public void BulkJobStatus_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, (int)BulkJobStatus.Pending);
        Assert.Equal(1, (int)BulkJobStatus.Processing);
        Assert.Equal(2, (int)BulkJobStatus.Completed);
        Assert.Equal(3, (int)BulkJobStatus.CompletedWithErrors);
        Assert.Equal(4, (int)BulkJobStatus.Failed);
        Assert.Equal(5, (int)BulkJobStatus.Cancelled);
    }
}
