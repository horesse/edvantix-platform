using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Services;

/// <summary>
/// Reports the live count of active teachers for the <see cref="QuotaResource.ActiveTeachers"/>
/// gauge — <see cref="TeacherStatus.Active"/>, non-deleted. Filters bypassed + explicit tenant
/// predicate so a cross-tenant capture reads the right tenant (see
/// <see cref="ActiveStudentCountQuotaGaugeProvider"/>).
/// </summary>
internal sealed class ActiveTeacherCountQuotaGaugeProvider : IQuotaGaugeProvider
{
    private readonly PeopleDbContext _db;

    public ActiveTeacherCountQuotaGaugeProvider(PeopleDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public QuotaResource Resource => QuotaResource.ActiveTeachers;

    public async ValueTask<long> GetCurrentAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await _db.Teachers
            .IgnoreQueryFilters()
            .Where(t => EF.Property<string>(t, "TenantId") == tenantId)
            .Where(t => !t.IsDeleted && t.Status == TeacherStatus.Active)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
