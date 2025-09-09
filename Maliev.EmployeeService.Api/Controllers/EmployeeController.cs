using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Maliev.EmployeeService.Api.Models;

namespace Maliev.EmployeeService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("employees/v{version:apiVersion}")]
public class EmployeeController : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] UserValidationRequest request)
    {
        if(request == null)
        {
            return BadRequest("Request body is null.");
        }
        
        return Ok();
    }
}