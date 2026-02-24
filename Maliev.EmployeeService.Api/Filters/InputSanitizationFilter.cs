using Maliev.EmployeeService.Application.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Maliev.EmployeeService.Api.Filters;

/// <summary>
/// Action filter that automatically sanitizes all string inputs in request DTOs to prevent XSS attacks
/// Phase 16 - T381: Input Sanitization
/// </summary>
public class InputSanitizationFilter : IActionFilter
{
    private readonly ILogger<InputSanitizationFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSanitizationFilter"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    public InputSanitizationFilter(ILogger<InputSanitizationFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes action arguments before action execution
    /// </summary>
    /// <param name="context">The action executing context</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Sanitize all action arguments
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value != null)
            {
                // Skip framework types and system types to avoid circular references
                var type = argument.Value.GetType();
                if (type.Namespace != null &&
                    (type.Namespace.StartsWith("System") ||
                     type.Namespace.StartsWith("Microsoft") ||
                     type.IsPrimitive ||
                     type == typeof(string) ||
                     type == typeof(Guid) ||
                     type == typeof(DateTime) ||
                     type == typeof(DateTimeOffset)))
                {
                    continue;
                }

                try
                {
                    // Check if the object contains XSS patterns before sanitization
                    var hasXss = CheckForXssPatterns(argument.Value);
                    if (hasXss)
                    {
                        _logger.LogWarning(
                            "Potential XSS attack detected in request. Action: {Action}, Parameter: {Parameter}",
                            context.ActionDescriptor.DisplayName,
                            argument.Key);
                    }

                    // Sanitize the object
                    InputSanitizer.SanitizeObject(argument.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sanitizing input for action {Action}, parameter {Parameter}",
                        context.ActionDescriptor.DisplayName,
                        argument.Key);
                }
            }
        }
    }

    /// <summary>
    /// Called after action execution (no operation required for this filter)
    /// </summary>
    /// <param name="context">The action executed context</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }

    /// <summary>
    /// Recursively checks if an object or its properties contain XSS patterns
    /// </summary>
    private bool CheckForXssPatterns(object obj)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return CheckForXssPatterns(obj, visited);
    }

    /// <summary>
    /// Recursively checks if an object or its properties contain XSS patterns
    /// Tracks visited objects to prevent infinite recursion on circular references
    /// </summary>
    private bool CheckForXssPatterns(object obj, HashSet<object> visited)
    {
        if (obj == null)
        {
            return false;
        }

        // Prevent infinite recursion on circular references
        if (!visited.Add(obj))
        {
            return false;
        }

        var type = obj.GetType();

        // Check string properties
        var stringProperties = type.GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead);

        foreach (var property in stringProperties)
        {
            var value = property.GetValue(obj) as string;
            if (InputSanitizer.ContainsXssPatterns(value))
            {
                return true;
            }
        }

        // Check nested objects
        var complexProperties = type.GetProperties()
            .Where(p => !p.PropertyType.IsPrimitive
                        && p.PropertyType != typeof(string)
                        && p.PropertyType != typeof(DateTime)
                        && p.PropertyType != typeof(DateTimeOffset)
                        && p.PropertyType != typeof(Guid)
                        && p.PropertyType != typeof(decimal)
                        && !p.PropertyType.IsEnum
                        && p.CanRead);

        foreach (var property in complexProperties)
        {
            var value = property.GetValue(obj);
            if (value != null)
            {
                if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    foreach (var item in enumerable)
                    {
                        if (CheckForXssPatterns(item, visited))
                        {
                            return true;
                        }
                    }
                }
                else if (CheckForXssPatterns(value, visited))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
