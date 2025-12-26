using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Maliev.EmployeeService.Infrastructure.Data;

public class EmployeeDbContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
{
    public EmployeeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EmployeeDbContext>();
        
        // Use a dummy connection string for migration generation
        optionsBuilder.UseNpgsql("Host=localhost;Database=employee;Username=postgres;Password=postgres");

        // Simple dummy implementations for design-time
        var encryptionService = new DummyEncryptionService();
        var currentUserService = new DummyCurrentUserService();
        var httpContextAccessor = new DummyHttpContextAccessor();
        var auditLogInterceptor = new AuditLogInterceptor(currentUserService, httpContextAccessor);
        var databaseMetricsInterceptor = new DatabaseMetricsInterceptor();

        return new EmployeeDbContext(
            optionsBuilder.Options,
            encryptionService,
            auditLogInterceptor,
            databaseMetricsInterceptor);
    }

    private class DummyEncryptionService : IEncryptionService
    {
        public string Encrypt(string plainText) => plainText;
        public string Decrypt(string cipherText) => cipherText;
        public bool IsEncrypted(string value) => false;
    }

    private class DummyCurrentUserService : ICurrentUserService
    {
        public Guid? PrincipalId => null;
        public Task<Guid?> GetEmployeeIdAsync(CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Guid? EmployeeId => null;
        public string? Email => null;
        public IEnumerable<string> Roles => new List<string>();
        public Role PrimaryRole => Role.Employee;
        public bool IsInRole(string role) => false;
        public bool IsAuthenticated => false;
    }

    private class DummyHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}