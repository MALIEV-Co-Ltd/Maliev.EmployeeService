namespace Maliev.EmployeeService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Employee Service.
/// </summary>
public static class EmployeePermissions
{
    public const string EmployeeRead = "employee.employees.read";
    public const string EmployeeWrite = "employee.employees.write";

    public const string ProfileRead = "employee.profiles.read";
    public const string ProfileWrite = "employee.profiles.write";

    public const string DocumentCreate = "employee.documents.create";
    public const string DocumentRead = "employee.documents.read";
    public const string DocumentDelete = "employee.documents.delete";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { EmployeeRead, "Read employee data" },
        { EmployeeWrite, "Write employee data" },
        { ProfileRead, "Read employee profiles" },
        { ProfileWrite, "Write employee profiles" },
        { DocumentCreate, "Create employee documents" },
        { DocumentRead, "Read employee documents" },
        { DocumentDelete, "Delete employee documents" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
