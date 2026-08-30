using FSH.Framework.Shared.Quota;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;
using FSH.Modules.StudyGroups.Services;

namespace StudyGroups.Tests.Services;

public sealed class StudyGroupCountQuotaGaugeProviderTests
{
    private const string Tenant = "tenant-acme";

    [Fact]
    public async Task Counts_forming_and_active_excludes_finished_and_cancelled()
    {
        using var db = TestStudyGroupsDbContextFactory.Create(Tenant);

        db.StudyGroups.Add(NewGroup());                        // Forming
        db.StudyGroups.Add(Activated(NewGroup()));             // Active
        db.StudyGroups.Add(Cancelled(NewGroup()));             // Cancelled -> excluded
        db.StudyGroups.Add(Finished(Activated(NewGroup())));   // Finished -> excluded
        await db.SaveChangesAsync();

        var sut = new StudyGroupCountQuotaGaugeProvider(db);
        sut.Resource.ShouldBe(QuotaResource.StudyGroups);
        (await sut.GetCurrentAsync(Tenant)).ShouldBe(2);
    }

    [Fact]
    public async Task Ignores_other_tenants()
    {
        using var db = TestStudyGroupsDbContextFactory.Create(Tenant);
        db.StudyGroups.Add(NewGroup());
        await db.SaveChangesAsync();

        (await new StudyGroupCountQuotaGaugeProvider(db).GetCurrentAsync("someone-else")).ShouldBe(0);
    }

    private static StudyGroup NewGroup() => StudyGroup.Create(
        code: $"G-{Guid.NewGuid():N}",
        name: "Test group",
        courseId: Guid.CreateVersion7(),
        primaryTeacherId: Guid.CreateVersion7(),
        format: GroupFormat.Online,
        capacity: 10,
        startDate: DateOnly.FromDateTime(DateTime.UtcNow),
        endDate: null,
        meetingUrl: null,
        roomId: null,
        notes: null);

    private static StudyGroup Activated(StudyGroup g)
    {
        g.Enroll(Guid.CreateVersion7(), DateOnly.FromDateTime(DateTime.UtcNow), tariffId: null, discountPercent: 0);
        g.Activate();
        return g;
    }

    private static StudyGroup Cancelled(StudyGroup g)
    {
        g.Cancel("test");
        return g;
    }

    private static StudyGroup Finished(StudyGroup g)
    {
        g.Finish(DateOnly.FromDateTime(DateTime.UtcNow));
        return g;
    }
}
