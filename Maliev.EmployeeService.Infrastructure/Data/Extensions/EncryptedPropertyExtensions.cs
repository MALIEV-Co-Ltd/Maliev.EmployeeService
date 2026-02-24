using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Maliev.EmployeeService.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for configuring encrypted properties using EF Core value converters
/// This approach uses value converters to handle encryption/decryption transparently
/// - Entity properties remain plaintext (in-memory use)
/// - Database columns store encrypted values
/// - EF Core change tracking compares plaintext values (no concurrency issues)
/// Phase 16 - Addressing EncryptionInterceptor concurrency issues with randomized IV
/// </summary>
public static class EncryptedPropertyExtensions
{
    /// <summary>
    /// Configures an encrypted string property using value converter
    /// - Entity property stays plaintext for in-memory operations
    /// - Database column stores encrypted value
    /// - EF Core tracks plaintext values (avoiding randomized IV concurrency issues)
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="builder">Entity type builder</param>
    /// <param name="propertyExpression">Expression selecting the string property to encrypt</param>
    /// <param name="encryptionService">Encryption service instance</param>
    /// <param name="maxLength">Maximum length for the encrypted column (default 500)</param>
    /// <returns>Property builder for further configuration</returns>
    public static PropertyBuilder<string?> HasEncryption<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, string?>> propertyExpression,
        IEncryptionService encryptionService,
        int maxLength = 500) where TEntity : class
    {
        var propertyBuilder = builder.Property(propertyExpression);

        // Create value converter for encryption/decryption
        var converter = new ValueConverter<string?, string?>(
            // Convert from plaintext to encrypted when saving to database
            plaintext => ConvertToEncrypted(plaintext, encryptionService),
            // Convert from encrypted to plaintext when loading from database
            encrypted => ConvertToPlaintext(encrypted, encryptionService)
        );

        propertyBuilder
            .HasConversion(converter)
            .HasMaxLength(maxLength)
            .IsConcurrencyToken(false); // Exclude from WHERE clause in UPDATE statements (randomized IV causes concurrency issues)

        return propertyBuilder;
    }

    private static string? ConvertToEncrypted(string? plaintext, IEncryptionService encryptionService)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        // Don't re-encrypt if already encrypted
        if (encryptionService.IsEncrypted(plaintext))
            return plaintext;

        return encryptionService.Encrypt(plaintext);
    }

    private static string? ConvertToPlaintext(string? encrypted, IEncryptionService encryptionService)
    {
        if (string.IsNullOrEmpty(encrypted))
            return encrypted;

        // Only decrypt if actually encrypted
        if (!encryptionService.IsEncrypted(encrypted))
            return encrypted;

        return encryptionService.Decrypt(encrypted);
    }
}
