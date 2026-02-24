using Maliev.EmployeeService.Application.DTOs.CareerService;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// HTTP client for communicating with Career Service
/// </summary>
public interface ICareerServiceClient
{
    /// <summary>
    /// Gets a job application by ID from Career Service
    /// </summary>
    Task<JobApplicationDto?> GetJobApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all job applications for a specific job posting
    /// </summary>
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job application status (e.g., when applicant is hired)
    /// </summary>
    Task<bool> UpdateJobApplicationStatusAsync(Guid applicationId, string status, Guid? employeeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job posting by ID from Career Service
    /// </summary>
    Task<JobPostingDto?> GetJobPostingAsync(Guid jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies Career Service when an employee is terminated (to reopen job posting if needed)
    /// </summary>
    Task NotifyEmployeeTerminatedAsync(Guid employeeId, Guid? jobPostingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a skill by ID from Career Service skills catalog
    /// Used for validating employee skills during onboarding
    /// </summary>
    Task<SkillDto?> GetSkillByIdAsync(int skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a work location by ID from Career Service work locations catalog
    /// Used for validating employee work location during onboarding
    /// </summary>
    Task<WorkLocationDto?> GetWorkLocationByIdAsync(int locationId, CancellationToken cancellationToken = default);
}
