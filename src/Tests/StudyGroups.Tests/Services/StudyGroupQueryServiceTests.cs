using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;
using FSH.Modules.StudyGroups.Services;

namespace StudyGroups.Tests.Services;

public sealed class StudyGroupQueryServiceTests
{
    private static StudyGroup CreateGroup(Guid primaryTeacherId, StudyGroupStatus status)
    {
        var group = StudyGroup.Create(
            code: $"G-{Guid.NewGuid():N}",
            name: "Test group",
            courseId: Guid.CreateVersion7(),
            primaryTeacherId: primaryTeacherId,
            format: GroupFormat.Online,
            capacity: 10,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: null,
            meetingUrl: null,
            roomId: null,
            notes: null);

        if (status == StudyGroupStatus.Active)
        {
            group.Enroll(Guid.CreateVersion7(), DateOnly.FromDateTime(DateTime.UtcNow), tariffId: null, discountPercent: 0);
            group.Activate();
        }

        return group;
    }

    [Fact]
    public async Task GetActiveGroupIdsForTeacherAsync_Includes_PrimaryTeacher_Group()
    {
        var teacherId = Guid.CreateVersion7();
        using var dbContext = TestStudyGroupsDbContextFactory.Create();
        var group = CreateGroup(teacherId, StudyGroupStatus.Active);
        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var sut = new StudyGroupQueryService(dbContext);
        var ids = await sut.GetActiveGroupIdsForTeacherAsync(teacherId);

        ids.ShouldBe([group.Id]);
    }

    [Fact]
    public async Task GetActiveGroupIdsForTeacherAsync_Includes_Roster_Teacher_Not_Just_Primary()
    {
        var primaryTeacherId = Guid.CreateVersion7();
        var assistantTeacherId = Guid.CreateVersion7();
        using var dbContext = TestStudyGroupsDbContextFactory.Create();
        var group = CreateGroup(primaryTeacherId, StudyGroupStatus.Active);
        group.AddTeacher(assistantTeacherId, TeacherRole.Assistant);
        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var sut = new StudyGroupQueryService(dbContext);
        var ids = await sut.GetActiveGroupIdsForTeacherAsync(assistantTeacherId);

        ids.ShouldBe([group.Id]);
    }

    [Fact]
    public async Task GetActiveGroupIdsForTeacherAsync_Deduplicates_When_Primary_Also_On_Roster()
    {
        var teacherId = Guid.CreateVersion7();
        using var dbContext = TestStudyGroupsDbContextFactory.Create();
        var group = CreateGroup(teacherId, StudyGroupStatus.Active);
        group.AddTeacher(teacherId, TeacherRole.Substitute);
        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var sut = new StudyGroupQueryService(dbContext);
        var ids = await sut.GetActiveGroupIdsForTeacherAsync(teacherId);

        ids.ShouldBe([group.Id]);
    }

    [Fact]
    public async Task GetActiveGroupIdsForTeacherAsync_Excludes_Non_Active_Group()
    {
        var teacherId = Guid.CreateVersion7();
        using var dbContext = TestStudyGroupsDbContextFactory.Create();
        var group = CreateGroup(teacherId, StudyGroupStatus.Forming);
        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var sut = new StudyGroupQueryService(dbContext);
        var ids = await sut.GetActiveGroupIdsForTeacherAsync(teacherId);

        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetActiveGroupIdsForTeacherAsync_Returns_Empty_For_Unrelated_Teacher()
    {
        using var dbContext = TestStudyGroupsDbContextFactory.Create();
        var group = CreateGroup(Guid.CreateVersion7(), StudyGroupStatus.Active);
        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var sut = new StudyGroupQueryService(dbContext);
        var ids = await sut.GetActiveGroupIdsForTeacherAsync(Guid.CreateVersion7());

        ids.ShouldBeEmpty();
    }
}
