using System.Text.RegularExpressions;

namespace Maliev.EmployeeService.Application.Common;

/// <summary>
/// Service for sanitizing user input to prevent XSS attacks
/// Phase 16 - T381: Input Sanitization
/// </summary>
public static class InputSanitizer
{
    // Script and style tags with their contents
    private static readonly Regex ScriptStylePattern = new(
        @"<(script|style)[^>]*>.*?</(script|style)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
        TimeSpan.FromMilliseconds(100));

    // Other HTML tags
    private static readonly Regex HtmlTagPattern = new(
        @"<[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    // JavaScript event handlers (onclick, onload, etc.)
    private static readonly Regex JavaScriptEventPattern = new(
        @"on\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    // JavaScript protocol in URLs (javascript:, data:, vbscript:)
    private static readonly Regex DangerousProtocolPattern = new(
        @"(javascript|data|vbscript):",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    // SQL injection patterns (single quotes, comment markers, etc.)
    private static readonly Regex SqlInjectionPattern = new(
        @"('|(--)|(/\*)|(\*/)|(\bexec\b)|(\bexecute\b)|(\bselect\b)|(\binsert\b)|(\bupdate\b)|(\bdelete\b)|(\bdrop\b)|(\bcreate\b)|(\balter\b))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Sanitizes a string by removing potentially dangerous content
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>Sanitized string with dangerous content removed</returns>
    public static string? Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sanitized = input;

        // Remove script and style blocks entirely (including content)
        sanitized = ScriptStylePattern.Replace(sanitized, string.Empty);

        // Remove other HTML tags
        sanitized = HtmlTagPattern.Replace(sanitized, string.Empty);

        // Remove JavaScript event handlers
        sanitized = JavaScriptEventPattern.Replace(sanitized, string.Empty);

        // Remove dangerous protocols
        sanitized = DangerousProtocolPattern.Replace(sanitized, string.Empty);

        // Trim whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }

    /// <summary>
    /// Validates that a string does not contain SQL injection patterns
    /// Note: This is a defense-in-depth measure. The primary protection is parameterized queries via EF Core.
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <returns>True if the input appears safe, false if SQL injection patterns are detected</returns>
    public static bool IsSafeSqlInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        return !SqlInjectionPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if input contains potential XSS vectors
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <returns>True if XSS patterns are detected</returns>
    public static bool ContainsXssPatterns(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return ScriptStylePattern.IsMatch(input)
               || HtmlTagPattern.IsMatch(input)
               || JavaScriptEventPattern.IsMatch(input)
               || DangerousProtocolPattern.IsMatch(input);
    }

    /// <summary>
    /// Sanitizes all string properties in an object recursively
    /// </summary>
    /// <param name="obj">The object to sanitize</param>
    public static void SanitizeObject(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        var type = obj.GetType();

        // Get all string properties
        var stringProperties = type.GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var property in stringProperties)
        {
            var value = property.GetValue(obj) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                var sanitized = Sanitize(value);
                property.SetValue(obj, sanitized);
            }
        }

        // Handle nested objects (non-primitive types)
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
                // Handle collections
                if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    foreach (var item in enumerable)
                    {
                        SanitizeObject(item);
                    }
                }
                else
                {
                    SanitizeObject(value);
                }
            }
        }
    }
}
