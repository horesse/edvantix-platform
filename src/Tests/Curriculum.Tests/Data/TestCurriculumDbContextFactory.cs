using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Curriculum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Curriculum.Tests.Data;

/// <summary>EF Core InMemory-backed <see cref="CurriculumDbContext"/> for handler/initializer-level
/// unit tests — same pattern as <c>Payments.Tests.Features.TestPaymentsDbContextFactory</c>. Not a
/// substitute for the Docker-backed integration tests (tenant isolation, real Postgres SQL
/// translation), just fast coverage for LINQ-heavy code.</summary>
public static class TestCurriculumDbContextFactory
{
    public static CurriculumDbContext Create(string? tenantId = "tenant-acme")
    {
        var options = new DbContextOptionsBuilder<CurriculumDbContext>()
            .UseInMemoryDatabase($"curriculum-{Guid.NewGuid():N}")
            .Options;

        var settings = Options.Create(new DatabaseOptions());
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        var tenantAccessor = new TestTenantAccessor();
        if (tenantId is not null)
        {
            ((IMultiTenantContextSetter)tenantAccessor).MultiTenantContext =
                new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId));
        }

        return new CurriculumDbContext(tenantAccessor, options, settings, environment);
    }

    private sealed class TestTenantAccessor : IMultiTenantContextAccessor<AppTenantInfo>, IMultiTenantContextSetter
    {
        private IMultiTenantContext<AppTenantInfo> _context = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo());

        public IMultiTenantContext<AppTenantInfo> MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextSetter.MultiTenantContext
        {
            set => _context = (IMultiTenantContext<AppTenantInfo>)value;
        }
    }
}
