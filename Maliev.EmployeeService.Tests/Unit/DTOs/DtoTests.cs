using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Enums;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.DTOs;

public class HeadcountReportDtoTests
{
    [Fact]
    public void HeadcountReportDto_ShouldInitializeCollections()
    {
        var dto = new HeadcountReportDto();

        Assert.NotNull(dto.ByDepartment);
        Assert.NotNull(dto.ByEmploymentType);
        Assert.NotNull(dto.ByTenureBand);
        Assert.NotNull(dto.ByLocation);
    }

    [Fact]
    public void DepartmentHeadcountDto_ShouldInitializeProperties()
    {
        var dto = new DepartmentHeadcountDto
        {
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            Headcount = 10,
            ManagerCount = 2,
            IndividualContributorCount = 8
        };

        Assert.Equal("Engineering", dto.DepartmentName);
        Assert.Equal(10, dto.Headcount);
    }

    [Fact]
    public void HeadcountReportDto_ShouldAllowSettingProperties()
    {
        var dto = new HeadcountReportDto
        {
            TotalHeadcount = 100,
            AsOfDate = DateTime.UtcNow,
            ByEmploymentType = new Dictionary<string, int>
            {
                { "FullTime", 80 },
                { "PartTime", 20 }
            },
            ByTenureBand = new Dictionary<string, int>
            {
                { "0-1 years", 30 },
                { "1-2 years", 25 },
                { "2-3 years", 20 },
                { "3-5 years", 15 },
                { "5-10 years", 7 },
                { "10+ years", 3 }
            }
        };

        Assert.Equal(100, dto.TotalHeadcount);
        Assert.Equal(2, dto.ByEmploymentType.Count);
        Assert.Equal(6, dto.ByTenureBand.Count);
    }
}

public class EmployeeProfileDtoTests
{
    [Fact]
    public void EmployeeProfileDto_ShouldInitializeEmergencyContacts()
    {
        var dto = new EmployeeProfileDto();

        Assert.NotNull(dto.EmergencyContacts);
    }

    [Fact]
    public void EmployeeProfileDto_ShouldAllowSettingEmergencyContacts()
    {
        var dto = new EmployeeProfileDto
        {
            EmergencyContacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto
                {
                    ContactName = "Jane Doe",
                    Relationship = "Spouse",
                    PhoneNumber = "+1234567890",
                    PriorityOrder = 1
                }
            }
        };

        Assert.Single(dto.EmergencyContacts);
        Assert.Equal("Jane Doe", dto.EmergencyContacts[0].ContactName);
    }
}

public class EmergencyContactDtoTests
{
    [Fact]
    public void EmergencyContactDto_ShouldInitializeProperties()
    {
        var dto = new EmergencyContactDto
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            ContactName = "John Doe",
            Relationship = "Father",
            PhoneNumber = "+0987654321",
            Email = "john@email.com",
            PriorityOrder = 1
        };

        Assert.Equal("John Doe", dto.ContactName);
        Assert.Equal("Father", dto.Relationship);
        Assert.Equal(1, dto.PriorityOrder);
    }
}

public class TeamDtoTests
{
    [Fact]
    public void TeamDto_ShouldInitializeProperties()
    {
        var dto = new TeamDto
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            Description = "Main engineering team",
            TeamType = "Project",
            MemberCount = 10
        };

        Assert.Equal("Engineering Team", dto.Name);
        Assert.Equal("Project", dto.TeamType);
        Assert.Equal(10, dto.MemberCount);
    }

    [Fact]
    public void TeamDetailsDto_ShouldInitializeMembers()
    {
        var dto = new TeamDetailsDto
        {
            Id = Guid.NewGuid(),
            Name = "Engineering Team",
            Members = new List<TeamMemberAssignmentDto>
            {
                new TeamMemberAssignmentDto
                {
                    EmployeeId = Guid.NewGuid(),
                    FullName = "John Doe",
                    JobTitle = "Developer"
                }
            }
        };

        Assert.Single(dto.Members);
        Assert.Equal("John Doe", dto.Members[0].FullName);
    }
}

public class TeamMemberDtoTests
{
    [Fact]
    public void TeamMemberDto_ShouldInitializeProperties()
    {
        var dto = new TeamMemberDto
        {
            EmployeeId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            FullName = "John Doe",
            PreferredName = "Johnny",
            JobTitle = "Software Engineer",
            DepartmentName = "Engineering",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            WorkLocation = "Bangkok",
            StartDate = DateTime.UtcNow.AddYears(-2)
        };

        Assert.Equal("John Doe", dto.FullName);
        Assert.Equal("Software Engineer", dto.JobTitle);
        Assert.Equal(EmploymentStatus.Active, dto.EmploymentStatus);
    }
}

public class TeamMemberAssignmentDtoTests
{
    [Fact]
    public void TeamMemberAssignmentDto_ShouldInitializeProperties()
    {
        var dto = new TeamMemberAssignmentDto
        {
            EmployeeId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            FullName = "John Doe",
            JobTitle = "Developer",
            DepartmentName = "Engineering",
            IsPrimary = true,
            WorkEmail = "john@company.com"
        };

        Assert.Equal("John Doe", dto.FullName);
        Assert.True(dto.IsPrimary);
    }
}
