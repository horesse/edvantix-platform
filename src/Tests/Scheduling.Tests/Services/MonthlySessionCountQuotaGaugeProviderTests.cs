using FSH.Framework.Shared.Quota;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;

namespace Scheduling.Tests.Services;

public sealed class MonthlySessionCountQuotaGaugeProviderTests
{
    private const string Tenant = "tenant-acme";
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Counts_sessions_in_current_month_excluding_cancelled_and_other_months()
    {
        using var db = TestSchedulingDbContextFactory.Create(Tenant);

        db.Sessions.Add(SessionAt(new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero)));   // this month
        db.Sessions.Add(SessionAt(new DateTimeOffset(2026, 6, 30, 20, 0, 0, TimeSpan.Zero))); // this month, near edge
        db.Sessions.Add(Cancel(SessionAt(new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero)))); // cancelled -> excluded
        db.Sessions.Add(SessionAt(new DateTimeOffset(2026, 5, 31, 23, 0, 0, TimeSpan.Zero))); // previous month
        db.Sessions.Add(SessionAt(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)));   // next month
        await db.SaveChangesAsync();

        var sut = new MonthlySessionCountQuotaGaugeProvider(db, new FixedTimeProvider(Now));
        sut.Resource.ShouldBe(QuotaResource.MonthlySessions);
        (await sut.GetCurrentAsync(Tenant)).ShouldBe(2);
    }

    [Fact]
    public async Task Ignores_other_tenants()
    {
        using var db = TestSchedulingDbContextFactory.Create(Tenant);
        db.Sessions.Add(SessionAt(new DateTimeOffset(2026, 6, 5, 9, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();

        var sut = new MonthlySessionCountQuotaGaugeProvider(db, new FixedTimeProvider(Now));
        (await sut.GetCurrentAsync("someone-else")).ShouldBe(0);
    }

    private static Session SessionAt(DateTimeOffset startUtc) => Session.Create(
        studyGroupId: Guid.CreateVersion7(),
        lessonId: null,
        teacherId: Guid.CreateVersion7(),
        roomId: null,
        startUtc: startUtc,
        endUtc: startUtc.AddHours(1),
        topic: null,
        meetingUrl: null);

    private static Session Cancel(Session s)
    {
        s.Cancel("test");
        return s;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
