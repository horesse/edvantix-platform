using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Services;

/// <summary>
/// Reports the live count of study groups that occupy a plan slot for the
/// <see cref="QuotaResource.StudyGroups"/> gauge — <see cref="StudyGroupStatus.Forming"/> or
/// <see cref="StudyGroupStatus.Active"/>, non-deleted. Finished / cancelled groups are historical
/// and don't count. Filters bypassed + explicit tenant predicate (see the People gauges).
/// </summary>
internal sealed class StudyGroupCountQuotaGaugeProvider : IQuotaGaugeProvider
{
    private readonly StudyGroupsDbContext _db;

    public StudyGroupCountQuotaGaugeProvider(StudyGroupsDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public QuotaResource Resource => QuotaResource.StudyGroups;

    public async ValueTask<long> GetCurrentAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await _db.StudyGroups
            .IgnoreQueryFilters()
            .Where(g => EF.Property<string>(g, "TenantId") == tenantId)
            .Where(g => !g.IsDeleted
                && (g.Status == StudyGroupStatus.Forming || g.Status == StudyGroupStatus.Active))
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
