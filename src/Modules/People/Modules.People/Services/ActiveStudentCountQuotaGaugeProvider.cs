using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Services;

/// <summary>
/// Reports the live count of students that occupy a plan slot for the <see cref="QuotaResource.ActiveStudents"/>
/// gauge — every non-archived, non-deleted student (Lead / Active / Paused). Queries with filters
/// bypassed and an explicit tenant predicate so a root operator capturing another tenant's usage
/// still gets that tenant's count, mirroring <c>UserCountQuotaGaugeProvider</c> in Identity.
/// </summary>
internal sealed class ActiveStudentCountQuotaGaugeProvider : IQuotaGaugeProvider
{
    private readonly PeopleDbContext _db;

    public ActiveStudentCountQuotaGaugeProvider(PeopleDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public QuotaResource Resource => QuotaResource.ActiveStudents;

    public async ValueTask<long> GetCurrentAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await _db.Students
            .IgnoreQueryFilters()
            .Where(s => EF.Property<string>(s, "TenantId") == tenantId)
            .Where(s => !s.IsDeleted && s.Status != StudentStatus.Archived)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
