namespace Maliev.EmployeeService.Api.Models
{
    public class UserValidationRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}