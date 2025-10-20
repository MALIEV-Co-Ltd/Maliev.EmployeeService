namespace Maliev.EmployeeService.Application.Events;

/// <summary>
/// Event received from Career Service when a candidate is accepted for employment
/// This event triggers automatic employee record creation and onboarding workflow
/// </summary>
public class CandidateAcceptedEvent
{
    /// <summary>
    /// Unique identifier for the candidate in Career Service
    /// </summary>
    public Guid CandidateId { get; set; }

    /// <summary>
    /// Full name of the applicant
    /// </summary>
    public string ApplicantName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the applicant
    /// </summary>
    public string ApplicantEmail { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the applicant
    /// </summary>
    public string ApplicantPhone { get; set; } = string.Empty;

    /// <summary>
    /// Job position ID from Career Service
    /// </summary>
    public Guid JobPositionId { get; set; }

    /// <summary>
    /// Expected start date for the new employee
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// LinkedIn profile URL (optional)
    /// </summary>
    public string? LinkedInProfile { get; set; }

    /// <summary>
    /// Department ID where the employee will be assigned
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Manager ID who will supervise the new employee
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Work location ID from Career Service
    /// </summary>
    public int? WorkLocationId { get; set; }
}
