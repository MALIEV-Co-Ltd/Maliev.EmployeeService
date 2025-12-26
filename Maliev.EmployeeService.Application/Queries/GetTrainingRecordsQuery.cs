using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get training records for an employee
/// </summary>
public class GetTrainingRecordsQuery
{
    public Guid EmployeeId { get; set; }
    public TrainingType? FilterByType { get; set; }
}

/// <summary>
/// DTO for training record response
/// </summary>
public class TrainingRecordDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CertificateDocumentId { get; set; }
    public TrainingType TrainingType { get; set; }
    public string? Provider { get; set; }
    public CertificationStatus Status { get; set; }
    public int? DaysUntilExpiration { get; set; }
}

/// <summary>
/// Handler for GetTrainingRecordsQuery
/// </summary>
public class GetTrainingRecordsQueryHandler
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTrainingRecordsQueryHandler> _logger;

    public GetTrainingRecordsQueryHandler(
        ITrainingRepository trainingRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<GetTrainingRecordsQueryHandler> logger)
    {
        _trainingRepository = trainingRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<List<TrainingRecordDto>> HandleAsync(GetTrainingRecordsQuery query, CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have TrainingRead permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/training";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.TrainingRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view training records for this employee");
        }

        _logger.LogInformation("Getting training records for employee {EmployeeId}", query.EmployeeId);

        var trainingRecords = query.FilterByType.HasValue
            ? await _trainingRepository.GetByTypeAsync(query.EmployeeId, query.FilterByType.Value, cancellationToken)
            : await _trainingRepository.GetByEmployeeIdAsync(query.EmployeeId, cancellationToken);

        var result = trainingRecords.Select(tr =>
        {
            int? daysUntilExpiration = null;
            if (tr.ExpirationDate.HasValue)
            {
                daysUntilExpiration = (int)(tr.ExpirationDate.Value - DateTime.UtcNow).TotalDays;
            }

            return new TrainingRecordDto
            {
                Id = tr.Id,
                CourseName = tr.CourseName,
                CompletionDate = tr.CompletionDate,
                ExpirationDate = tr.ExpirationDate,
                CertificateDocumentId = tr.CertificateDocumentId,
                TrainingType = tr.TrainingType,
                Provider = tr.Provider,
                Status = tr.Status,
                DaysUntilExpiration = daysUntilExpiration
            };
        }).ToList();

        _logger.LogInformation("Retrieved {Count} training records for employee {EmployeeId}",
            result.Count, query.EmployeeId);

        return result;
    }
}
