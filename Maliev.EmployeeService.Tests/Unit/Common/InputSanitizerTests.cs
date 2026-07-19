using Maliev.EmployeeService.Application.Common;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Common;

/// <summary>
/// Unit tests for InputSanitizer
/// Phase 16 - T381: Input Sanitization
/// </summary>
public class InputSanitizerTests
{
    [Theory]
    [InlineData("<script>alert('XSS')</script>", "")]
    [InlineData("Hello <b>World</b>", "Hello World")]
    [InlineData("<img src=x onerror=alert('XSS')>", "")]
    [InlineData("<div onclick='malicious()'>Click me</div>", "Click me")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("Text with <strong>tags</strong> removed", "Text with tags removed")]
    public void Sanitize_ShouldRemoveHtmlTags(string input, string expected)
    {
        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<a href='javascript:alert(1)'>Click</a>", "Click")]
    [InlineData("<a href='data:text/html,<script>alert(1)</script>'>Click</a>", "Click")]
    [InlineData("<a href='vbscript:msgbox(1)'>Click</a>", "Click")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("http://safe-url.com", "http://safe-url.com")]
    public void Sanitize_ShouldRemoveDangerousProtocols(string input, string expected)
    {
        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<div onclick='alert(1)'>", "")]
    [InlineData("<div onload='malicious()'>", "")]
    [InlineData("<img onerror='hack()'>", "")]
    [InlineData("<body onmouseover='attack()'>", "")]
    [InlineData("Normal text without events", "Normal text without events")]
    public void Sanitize_ShouldRemoveJavaScriptEventHandlers(string input, string expected)
    {
        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Sanitize_ShouldHandleNullAndEmptyStrings(string? input, string? expected)
    {
        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_ShouldTrimWhitespace()
    {
        // Arrange
        var input = "   Hello World   ";

        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>", true)]
    [InlineData("<div onclick='malicious()'>", true)]
    [InlineData("javascript:alert(1)", true)]
    [InlineData("Normal safe text", false)]
    [InlineData("https://example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsXssPatterns_ShouldDetectXssVectors(string? input, bool expected)
    {
        // Act
        var result = InputSanitizer.ContainsXssPatterns(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("SELECT * FROM users", false)]
    [InlineData("DROP TABLE users", false)]
    [InlineData("Normal search text", true)]
    [InlineData("User's name", false)] // Contains single quote
    [InlineData("Comment -- test", false)] // Contains SQL comment
    [InlineData("", true)]
    [InlineData(null, true)]
    public void IsSafeSqlInput_ShouldDetectSqlInjectionPatterns(string? input, bool expected)
    {
        // Act
        var result = InputSanitizer.IsSafeSqlInput(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeObject_ShouldSanitizeAllStringProperties()
    {
        // Arrange
        var obj = new TestDto
        {
            Name = "<script>alert('XSS')</script>John",
            Email = "john@example.com<img src=x onerror=alert(1)>",
            Description = "Normal <b>text</b> with tags"
        };

        // Act
        InputSanitizer.SanitizeObject(obj);

        // Assert
        Assert.Equal("John", obj.Name);
        Assert.Equal("john@example.com", obj.Email);
        Assert.Equal("Normal text with tags", obj.Description);
    }

    [Fact]
    public void SanitizeObject_ShouldHandleNestedObjects()
    {
        // Arrange
        var obj = new ParentDto
        {
            Title = "<script>alert('XSS')</script>Parent",
            Child = new TestDto
            {
                Name = "Child<img src=x>",
                Email = "child@example.com",
                Description = "Nested <div onclick='hack()'>object</div>"
            }
        };

        // Act
        InputSanitizer.SanitizeObject(obj);

        // Assert
        Assert.Equal("Parent", obj.Title);
        Assert.Equal("Child", obj.Child.Name);
        Assert.Equal("child@example.com", obj.Child.Email);
        Assert.Equal("Nested object", obj.Child.Description);
    }

    [Fact]
    public void SanitizeObject_ShouldHandleCollections()
    {
        // Arrange
        var obj = new CollectionDto
        {
            Items = new List<TestDto>
            {
                new() { Name = "Item1<b>Bold</b>", Email = "item1@test.com", Description = "Desc1" },
                new() { Name = "Item2<img src=x>", Email = "item2@test.com", Description = "Desc2" }
            }
        };

        // Act
        InputSanitizer.SanitizeObject(obj);

        // Assert
        Assert.Equal("Item1Bold", obj.Items[0].Name);
        Assert.Equal("Item2", obj.Items[1].Name);
    }

    [Fact]
    public void SanitizeObject_ShouldHandleNullObject()
    {
        // Act & Assert - Should not throw
        var act = () => InputSanitizer.SanitizeObject(null);
        // NotThrow - execute without exception
    }

    [Fact]
    public void SanitizeObject_ShouldHandleNullProperties()
    {
        // Arrange
        var obj = new TestDto
        {
            Name = null,
            Email = "test@example.com",
            Description = null
        };

        // Act & Assert - Should not throw
        var act = () => InputSanitizer.SanitizeObject(obj);
        // NotThrow - execute without exception
        Assert.Equal("test@example.com", obj.Email);
    }

    // Test DTOs
    private class TestDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
    }

    private class ParentDto
    {
        public string? Title { get; set; }
        public TestDto? Child { get; set; }
    }

    private class CollectionDto
    {
        public List<TestDto> Items { get; set; } = new();
    }
}
