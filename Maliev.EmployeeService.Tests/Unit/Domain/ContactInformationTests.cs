using Maliev.EmployeeService.Domain.ValueObjects;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Domain;

public class ContactInformationTests
{
    [Fact]
    public void ContactInformation_WithValidWorkEmail_ShouldBeValid()
    {
        var contact = new ContactInformation("john.doe@company.com");

        Assert.True(contact.IsValid());
        Assert.Equal("john.doe@company.com", contact.WorkEmail);
    }

    [Fact]
    public void ContactInformation_WithAllFields_ShouldBeValid()
    {
        var contact = new ContactInformation(
            workEmail: "john.doe@company.com",
            mobilePhone: "+1234567890",
            personalEmail: "john@personal.com"
        );

        Assert.True(contact.IsValid());
        Assert.Equal("john.doe@company.com", contact.WorkEmail);
        Assert.Equal("+1234567890", contact.MobilePhone);
        Assert.Equal("john@personal.com", contact.PersonalEmail);
    }

    [Fact]
    public void ContactInformation_WithoutWorkEmail_ShouldBeInvalid()
    {
        var contact = new ContactInformation
        {
            WorkEmail = "",
            MobilePhone = "+1234567890",
            PersonalEmail = "john@personal.com"
        };

        Assert.False(contact.IsValid());
    }

    [Fact]
    public void ContactInformation_WithOnlyWorkEmail_ShouldBeValid()
    {
        var contact = new ContactInformation("test@company.com");

        Assert.True(contact.IsValid());
        Assert.Null(contact.MobilePhone);
        Assert.Null(contact.PersonalEmail);
    }

    [Fact]
    public void ContactInformation_DefaultConstructor_ShouldBeInvalid()
    {
        var contact = new ContactInformation();

        Assert.False(contact.IsValid());
    }

    [Fact]
    public void ContactInformation_WithWhitespaceWorkEmail_ShouldBeInvalid()
    {
        var contact = new ContactInformation
        {
            WorkEmail = "   ",
            MobilePhone = "+1234567890"
        };

        Assert.False(contact.IsValid());
    }
}
