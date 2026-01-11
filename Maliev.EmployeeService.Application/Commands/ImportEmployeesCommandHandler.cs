using System.Text.Json;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for ImportEmployeesCommand
/// Processes CSV import with validation and error tracking
/// User Story 12 - Bulk Operations
/// </summary>
public class ImportEmployeesCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IBulkJobRepository _bulkJobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportEmployeesCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IBulkJobRepository bulkJobRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _bulkJobRepository = bulkJobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        ImportEmployeesCommand command,
        CancellationToken cancellationToken = default)
    {
        // Create bulk job to track the operation
        var job = new BulkJob
        {
            JobType = "EmployeeImport",
            Status = BulkJobStatus.Processing,
            InitiatedByPrincipalId = command.InitiatedByPrincipalId,
            StartedAt = DateTime.UtcNow
        };

        await _bulkJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var errors = new List<string>();
        var successCount = 0;
        var failCount = 0;

        try
        {
            // Parse CSV
            var lines = command.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                errors.Add("CSV must contain at least a header row and one data row");
                job.Status = BulkJobStatus.Failed;
                job.TotalRecords = 0;
                job.Errors = JsonSerializer.Serialize(errors);
                job.CompletedAt = DateTime.UtcNow;
                _bulkJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return job.JobId;
            }

            var header = ParseCsvLine(lines[0]);
            var dataRows = lines.Skip(1).ToList();
            job.TotalRecords = dataRows.Count;
            _bulkJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Validate header
            var requiredColumns = new[] { "EmployeeNumber", "FirstName", "LastName", "JobTitle",
                "Department", "EmploymentType", "EmploymentStatus", "StartDate" };

            var missingColumns = requiredColumns.Where(col => !header.Contains(col, StringComparer.OrdinalIgnoreCase)).ToList();
            if (missingColumns.Any())
            {
                errors.Add($"Missing required columns: {string.Join(", ", missingColumns)}");
                job.Status = BulkJobStatus.Failed;
                job.Errors = JsonSerializer.Serialize(errors);
                job.CompletedAt = DateTime.UtcNow;
                _bulkJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return job.JobId;
            }

            // Get all departments for lookup
            var departments = (await _departmentRepository.GetAllAsync(cancellationToken))
                .ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

            // Get all existing employees for manager lookup
            var existingEmployees = (await _employeeRepository.GetAllAsync(cancellationToken))
                .ToDictionary(e => e.EmployeeNumber, e => e);

            // Process each row
            var rowNumber = 1;
            foreach (var line in dataRows)
            {
                rowNumber++;
                try
                {
                    var values = ParseCsvLine(line);
                    var rowData = header.Zip(values, (h, v) => new { Header = h, Value = v })
                        .ToDictionary(x => x.Header, x => x.Value, StringComparer.OrdinalIgnoreCase);

                    // Validate required fields
                    var validationErrors = ValidateRow(rowData, rowNumber);
                    if (validationErrors.Any())
                    {
                        errors.AddRange(validationErrors);
                        failCount++;
                        if (!command.SkipInvalidRows)
                        {
                            continue; // Skip this row but continue processing if SkipInvalidRows is true
                        }
                        continue;
                    }

                    // Skip if dry run
                    if (command.DryRun)
                    {
                        successCount++;
                        continue;
                    }

                    // Check if employee already exists
                    var employeeNumber = rowData["EmployeeNumber"];
                    if (existingEmployees.ContainsKey(employeeNumber))
                    {
                        errors.Add($"Row {rowNumber}: Employee {employeeNumber} already exists");
                        failCount++;
                        continue;
                    }

                    // Find department
                    var departmentName = rowData["Department"];
                    if (!departments.TryGetValue(departmentName, out var department))
                    {
                        errors.Add($"Row {rowNumber}: Department '{departmentName}' not found");
                        failCount++;
                        continue;
                    }

                    // Create employee
                    var employee = new Employee
                    {
                        EmployeeNumber = employeeNumber,
                        LegalName = new LegalName
                        {
                            FirstName = rowData["FirstName"],
                            LastName = rowData["LastName"],
                            MiddleName = rowData.TryGetValue("MiddleName", out var middleName) ? middleName : null
                        },
                        PreferredName = rowData.TryGetValue("PreferredName", out var preferredName) ? preferredName : null,
                        JobTitle = rowData["JobTitle"],
                        DepartmentId = department.Id,
                        EmploymentType = Enum.Parse<EmploymentType>(rowData["EmploymentType"], true),
                        EmploymentStatus = Enum.Parse<EmploymentStatus>(rowData["EmploymentStatus"], true),
                        StartDate = DateTime.Parse(rowData["StartDate"])
                    };

                    // Optional fields
                    if (rowData.TryGetValue("WorkEmail", out var workEmail) && !string.IsNullOrEmpty(workEmail))
                    {
                        employee.ContactInformation = new ContactInformation
                        {
                            WorkEmail = workEmail,
                            PersonalEmail = rowData.TryGetValue("PersonalEmail", out var personalEmail) ? personalEmail : null,
                            MobilePhone = rowData.TryGetValue("MobilePhone", out var mobilePhone) ? mobilePhone : null
                        };
                    }

                    if (rowData.TryGetValue("Nationality", out var nationality))
                    {
                        employee.Nationality = nationality;
                    }

                    if (rowData.TryGetValue("DateOfBirth", out var dobStr) && DateTime.TryParse(dobStr, out var dob))
                    {
                        employee.DateOfBirth = dob;
                    }

                    await _employeeRepository.AddAsync(employee, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                    failCount++;
                }
            }

            // Save changes if not dry run
            if (!command.DryRun)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Update job status
            job.SuccessfulRecords = successCount;
            job.FailedRecords = failCount;
            job.Errors = errors.Any() ? JsonSerializer.Serialize(errors) : null;
            job.Status = failCount == 0 ? BulkJobStatus.Completed :
                        successCount > 0 ? BulkJobStatus.CompletedWithErrors : BulkJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultData = command.DryRun
                ? $"Dry run completed. {successCount} rows would be imported, {failCount} errors found."
                : $"Import completed. {successCount} employees imported, {failCount} errors.";

            _bulkJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return job.JobId;
        }
        catch (Exception ex)
        {
            job.Status = BulkJobStatus.Failed;
            job.Errors = JsonSerializer.Serialize(new[] { $"Import failed: {ex.Message}" });
            job.CompletedAt = DateTime.UtcNow;
            _bulkJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private List<string> ValidateRow(Dictionary<string, string> rowData, int rowNumber)
    {
        var errors = new List<string>();

        // Required fields
        if (string.IsNullOrWhiteSpace(rowData.GetValueOrDefault("EmployeeNumber")))
            errors.Add($"Row {rowNumber}: EmployeeNumber is required");

        if (string.IsNullOrWhiteSpace(rowData.GetValueOrDefault("FirstName")))
            errors.Add($"Row {rowNumber}: FirstName is required");

        if (string.IsNullOrWhiteSpace(rowData.GetValueOrDefault("LastName")))
            errors.Add($"Row {rowNumber}: LastName is required");

        if (string.IsNullOrWhiteSpace(rowData.GetValueOrDefault("JobTitle")))
            errors.Add($"Row {rowNumber}: JobTitle is required");

        if (string.IsNullOrWhiteSpace(rowData.GetValueOrDefault("Department")))
            errors.Add($"Row {rowNumber}: Department is required");

        // Enum validations
        if (!Enum.TryParse<EmploymentType>(rowData.GetValueOrDefault("EmploymentType"), true, out _))
            errors.Add($"Row {rowNumber}: Invalid EmploymentType. Must be one of: {string.Join(", ", Enum.GetNames<EmploymentType>())}");

        if (!Enum.TryParse<EmploymentStatus>(rowData.GetValueOrDefault("EmploymentStatus"), true, out _))
            errors.Add($"Row {rowNumber}: Invalid EmploymentStatus. Must be one of: {string.Join(", ", Enum.GetNames<EmploymentStatus>())}");

        // Date validation
        if (!DateTime.TryParse(rowData.GetValueOrDefault("StartDate"), out _))
            errors.Add($"Row {rowNumber}: Invalid StartDate format. Expected: yyyy-MM-dd");

        return errors;
    }

    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of field
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add last field
        values.Add(currentValue.ToString().Trim());

        return values;
    }
}
