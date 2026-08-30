using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

/// <summary>
/// Reports the number of sessions scheduled within the current UTC calendar month for the
/// <see cref="QuotaResource.MonthlySessions"/> gauge — cancelled sessions excluded. This is the
/// one gauge with a period boundary: it naturally resets on the first of each month. Filters
/// bypassed + explicit tenant predicate (see the People gauges).
/// </summary>
internal sealed class MonthlySessionCountQuotaGaugeProvider : IQuotaGaugeProvider
{
    private readonly SchedulingDbContext _db;
    private readonly TimeProvider _timeProvider;

    public MonthlySessionCountQuotaGaugeProvider(SchedulingDbContext db, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _db = db;
        _timeProvider = timeProvider;
    }

    public QuotaResource Resource => QuotaResource.MonthlySessions;

    public async ValueTask<long> GetCurrentAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var now = _timeProvider.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonthStart = monthStart.AddMonths(1);

        return await _db.Sessions
            .IgnoreQueryFilters()
            .Where(s => EF.Property<string>(s, "TenantId") == tenantId)
            .Where(s => s.Status != SessionStatus.Cancelled
                && s.StartUtc >= monthStart && s.StartUtc < nextMonthStart)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}
