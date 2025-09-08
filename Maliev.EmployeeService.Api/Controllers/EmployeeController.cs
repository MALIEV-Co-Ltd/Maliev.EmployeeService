using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("employees/v{version:apiVersion}")]
public class EmployeeController : ControllerBase
{
    [HttpGet("validate")]
    public IActionResult Validate()
    {
        return Ok();
    }
}