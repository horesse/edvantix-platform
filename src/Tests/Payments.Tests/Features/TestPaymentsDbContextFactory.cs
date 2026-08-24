using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Payments.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Payments.Tests.Features;

/// <summary>EF Core InMemory-backed <see cref="PaymentsDbContext"/> for handler-level unit tests —
/// same pattern as <c>Scheduling.Tests.Services.TestSchedulingDbContextFactory</c>. Not a substitute
/// for the Docker-backed integration tests (tenant isolation, real Postgres SQL translation), just
/// fast coverage for LINQ-heavy handlers.</summary>
public static class TestPaymentsDbContextFactory
{
    public static PaymentsDbContext Create(string? tenantId = "tenant-acme")
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase($"payments-{Guid.NewGuid():N}")
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

        return new PaymentsDbContext(tenantAccessor, options, settings, environment);
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
