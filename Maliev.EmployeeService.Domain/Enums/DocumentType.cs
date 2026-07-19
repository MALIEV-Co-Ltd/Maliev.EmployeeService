namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Types of documents that can be stored for employees
/// Used for categorization and access control
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Employment contract document
    /// </summary>
    EmploymentContract = 0,

    /// <summary>
    /// Job offer letter
    /// </summary>
    OfferLetter = 1,

    /// <summary>
    /// Identification document (passport, national ID, driver's license)
    /// </summary>
    IDDocument = 2,

    /// <summary>
    /// Training certificate or professional certification
    /// </summary>
    Certificate = 3,

    /// <summary>
    /// Performance review document
    /// </summary>
    PerformanceReview = 4,

    /// <summary>
    /// Disciplinary action record
    /// </summary>
    DisciplinaryRecord = 5,

    /// <summary>
    /// Resignation letter
    /// </summary>
    ResignationLetter = 6,

    /// <summary>
    /// Policy acknowledgment form
    /// </summary>
    PolicyAcknowledgment = 7,

    /// <summary>
    /// Work permit or visa document
    /// </summary>
    WorkPermit = 8,

    /// <summary>
    /// Other document types
    /// </summary>
    Other = 99
}
