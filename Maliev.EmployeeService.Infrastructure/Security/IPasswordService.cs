namespace Maliev.EmployeeService.Infrastructure.Security;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
