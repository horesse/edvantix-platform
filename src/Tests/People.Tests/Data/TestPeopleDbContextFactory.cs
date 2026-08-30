using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace People.Tests.Data;

/// <summary>EF Core InMemory-backed <see cref="PeopleDbContext"/> for service-level unit tests —
/// same pattern as <c>StudyGroups.Tests.Services.TestStudyGroupsDbContextFactory</c>. Not a
/// substitute for the Docker-backed integration tests, just fast coverage for LINQ in the quota
/// gauge providers.</summary>
public static class TestPeopleDbContextFactory
{
    public static PeopleDbContext Create(string? tenantId = "tenant-acme")
    {
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseInMemoryDatabase($"people-{Guid.NewGuid():N}")
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

        return new PeopleDbContext(tenantAccessor, options, settings, environment);
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
