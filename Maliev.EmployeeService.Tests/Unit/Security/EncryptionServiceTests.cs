using FluentAssertions;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Security;

/// <summary>
/// Unit tests for EncryptionService
/// Tests AES-256 encryption/decryption for salary and sensitive data protection
/// </summary>
public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        // Setup configuration with testing environment
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Testing" }
            })
            .Build();

        _encryptionService = new EncryptionService(configuration);
    }

    [Fact]
    public void Encrypt_WithValidSalary_ShouldReturnEncryptedBase64String()
    {
        // Arrange
        var salary = "85000.00";

        // Act
        var encrypted = _encryptionService.Encrypt(salary);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(salary);
        _encryptionService.IsEncrypted(encrypted).Should().BeTrue();
    }

    [Fact]
    public void Decrypt_WithEncryptedSalary_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalSalary = "120000.50";
        var encrypted = _encryptionService.Encrypt(originalSalary);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(originalSalary);
    }

    [Fact]
    public void EncryptDecrypt_WithLargeSalary_ShouldMaintainPrecision()
    {
        // Arrange
        var largeSalary = "9999999.99";
        var encrypted = _encryptionService.Encrypt(largeSalary);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(largeSalary);
    }

    [Theory]
    [InlineData("50000.00")]
    [InlineData("125000.75")]
    [InlineData("0.01")]
    [InlineData("1000000.00")]
    public void EncryptDecrypt_WithVariousSalaries_ShouldRoundTrip(string salary)
    {
        // Act
        var encrypted = _encryptionService.Encrypt(salary);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(salary);
    }

    [Fact]
    public void Encrypt_CalledTwice_ShouldProduceDifferentCipherText()
    {
        // Arrange
        var salary = "75000.00";

        // Act
        var encrypted1 = _encryptionService.Encrypt(salary);
        var encrypted2 = _encryptionService.Encrypt(salary);

        // Assert - Different IV ensures different ciphertext
        encrypted1.Should().NotBe(encrypted2);

        // But both should decrypt to the same value
        _encryptionService.Decrypt(encrypted1).Should().Be(salary);
        _encryptionService.Decrypt(encrypted2).Should().Be(salary);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var encrypted = _encryptionService.Encrypt(emptyString);

        // Assert
        encrypted.Should().Be(emptyString);
    }

    [Fact]
    public void Encrypt_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? nullValue = null;

        // Act
        var encrypted = _encryptionService.Encrypt(nullValue!);

        // Assert
        encrypted.Should().BeNull();
    }

    [Fact]
    public void Decrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var decrypted = _encryptionService.Decrypt(emptyString);

        // Assert
        decrypted.Should().Be(emptyString);
    }

    [Fact]
    public void Decrypt_WithPlainText_ShouldReturnSameValue()
    {
        // Arrange - A plain text value that's not encrypted
        var plainText = "75000.00";

        // Act
        var decrypted = _encryptionService.Decrypt(plainText);

        // Assert - Should return as-is since it's not encrypted
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void IsEncrypted_WithEncryptedValue_ShouldReturnTrue()
    {
        // Arrange
        var salary = "60000.00";
        var encrypted = _encryptionService.Encrypt(salary);

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(encrypted);

        // Assert
        isEncrypted.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_WithPlainText_ShouldReturnFalse()
    {
        // Arrange
        var plainText = "50000.00";

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(plainText);

        // Assert
        isEncrypted.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(emptyString);

        // Assert
        isEncrypted.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithNull_ShouldReturnFalse()
    {
        // Arrange
        string? nullValue = null;

        // Act
        var isEncrypted = _encryptionService.IsEncrypted(nullValue!);

        // Assert
        isEncrypted.Should().BeFalse();
    }

    [Fact]
    public void Encrypt_WithSensitiveData_ShouldNotContainOriginalValue()
    {
        // Arrange
        var sensitiveData = "SSN:123-45-6789";

        // Act
        var encrypted = _encryptionService.Encrypt(sensitiveData);

        // Assert
        encrypted.Should().NotContain("123");
        encrypted.Should().NotContain("45");
        encrypted.Should().NotContain("6789");
        encrypted.Should().NotContain("SSN");
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_ShouldPreserveData()
    {
        // Arrange
        var dataWithSpecialChars = "Bonus: 5,000.00 THB + 10% commission!";

        // Act
        var encrypted = _encryptionService.Encrypt(dataWithSpecialChars);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(dataWithSpecialChars);
    }

    [Fact]
    public void EncryptDecrypt_WithUnicodeCharacters_ShouldPreserveData()
    {
        // Arrange - Thai Baht currency symbol and Thai text
        var unicodeData = "เงินเดือน: ฿85,000.00";

        // Act
        var encrypted = _encryptionService.Encrypt(unicodeData);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(unicodeData);
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldHandleCorrectly()
    {
        // Arrange
        var longText = new string('A', 5000); // 5KB of data

        // Act
        var encrypted = _encryptionService.Encrypt(longText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(longText);
        decrypted.Length.Should().Be(5000);
    }
}
