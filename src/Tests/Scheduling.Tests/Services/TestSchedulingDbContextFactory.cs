using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Scheduling.Tests.Services;

/// <summary>EF Core InMemory-backed <see cref="SchedulingDbContext"/> for service-level unit tests —
/// same pattern as <c>Webhooks.Tests.Services.WebhookFanoutHandlerTests</c>. Not a substitute for
/// the Docker-backed integration tests (tenant isolation, real Postgres SQL translation), just fast
/// coverage for LINQ-heavy services (<c>SessionConflictChecker</c>, <c>ScheduleGeneratorService</c>)
/// that would otherwise need a real database to exercise at all.</summary>
public static class TestSchedulingDbContextFactory
{
    public static SchedulingDbContext Create(string? tenantId = "tenant-acme")
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase($"scheduling-{Guid.NewGuid():N}")
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

        return new SchedulingDbContext(tenantAccessor, options, settings, environment);
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
