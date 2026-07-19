using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

public class LegalNameTests
{
    [Fact]
    public void LegalName_WithFirstAndLastName_ShouldBeValid()
    {
        var name = new LegalName("John", "Doe");

        Assert.True(name.IsValid());
        Assert.Equal("John", name.FirstName);
        Assert.Equal("Doe", name.LastName);
    }

    [Fact]
    public void LegalName_WithMiddleName_ShouldBeValid()
    {
        var name = new LegalName("John", "Doe", "Michael");

        Assert.True(name.IsValid());
        Assert.Equal("John", name.FirstName);
        Assert.Equal("Doe", name.LastName);
        Assert.Equal("Michael", name.MiddleName);
    }

    [Fact]
    public void FullName_WithMiddleName_ShouldIncludeAllParts()
    {
        var name = new LegalName("John", "Doe", "Michael");

        Assert.Equal("John Michael Doe", name.FullName);
    }

    [Fact]
    public void FullName_WithoutMiddleName_ShouldCombineFirstAndLast()
    {
        var name = new LegalName("John", "Doe");

        Assert.Equal("John Doe", name.FullName);
    }

    [Fact]
    public void FullName_WithEmptyMiddleName_ShouldNotIncludeMiddleName()
    {
        var name = new LegalName("John", "Doe", null);

        Assert.Equal("John Doe", name.FullName);
    }

    [Fact]
    public void FullName_WithWhitespaceMiddleName_ShouldNotIncludeMiddleName()
    {
        var name = new LegalName("John", "Doe", "   ");

        Assert.Equal("John Doe", name.FullName);
    }

    [Fact]
    public void LegalName_WithoutFirstName_ShouldBeInvalid()
    {
        var name = new LegalName
        {
            FirstName = "",
            LastName = "Doe"
        };

        Assert.False(name.IsValid());
    }

    [Fact]
    public void LegalName_WithoutLastName_ShouldBeInvalid()
    {
        var name = new LegalName
        {
            FirstName = "John",
            LastName = ""
        };

        Assert.False(name.IsValid());
    }

    [Fact]
    public void LegalName_DefaultConstructor_ShouldBeInvalid()
    {
        var name = new LegalName();

        Assert.False(name.IsValid());
    }

    [Fact]
    public void LegalName_WithWhitespaceFirstName_ShouldBeInvalid()
    {
        var name = new LegalName
        {
            FirstName = "   ",
            LastName = "Doe"
        };

        Assert.False(name.IsValid());
    }

    [Fact]
    public void LegalName_WithWhitespaceLastName_ShouldBeInvalid()
    {
        var name = new LegalName
        {
            FirstName = "John",
            LastName = "   "
        };

        Assert.False(name.IsValid());
    }
}
