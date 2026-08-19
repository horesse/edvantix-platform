using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;

namespace Scheduling.Tests.Services;

public sealed class SessionConflictCheckerTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 1, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 9, 1, 19, 0, 0, TimeSpan.Zero);

    #region Teacher conflict

    [Fact]
    public async Task CheckAsync_Should_ReportTeacherConflict_When_SameTeacherOverlaps()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var teacherId = Guid.NewGuid();
        var existing = await SeedSessionAsync(db, teacherId: teacherId, studyGroupId: Guid.NewGuid());

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, teacherId, roomId: null, studyGroupId: Guid.NewGuid(), Start, End);

        conflicts.ShouldContain(c => c.Type == SessionConflictType.Teacher && c.ConflictingSessionId == existing.Id);
    }

    [Fact]
    public async Task CheckAsync_Should_NotReportConflict_When_TimesDoNotOverlap()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var teacherId = Guid.NewGuid();
        await SeedSessionAsync(db, teacherId: teacherId, studyGroupId: Guid.NewGuid());

        var checker = new SessionConflictChecker(db);
        var laterStart = End; // starts exactly when the existing session ends — no overlap
        var laterEnd = End.AddHours(1);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, teacherId, roomId: null, studyGroupId: Guid.NewGuid(), laterStart, laterEnd);

        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckAsync_Should_NotReportConflict_When_ExistingSessionIsCancelled()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var teacherId = Guid.NewGuid();
        var existing = await SeedSessionAsync(db, teacherId: teacherId, studyGroupId: Guid.NewGuid());
        existing.Cancel("no reason");
        await db.SaveChangesAsync();

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, teacherId, roomId: null, studyGroupId: Guid.NewGuid(), Start, End);

        conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckAsync_Should_NotReportConflict_When_ExcludingTheConflictingSessionItself()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var teacherId = Guid.NewGuid();
        var existing = await SeedSessionAsync(db, teacherId: teacherId, studyGroupId: Guid.NewGuid());

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: existing.Id, teacherId, roomId: null, studyGroupId: Guid.NewGuid(), Start, End);

        conflicts.ShouldBeEmpty();
    }

    #endregion

    #region Room conflict

    [Fact]
    public async Task CheckAsync_Should_ReportRoomConflict_When_SameNonVirtualRoomOverlaps()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var room = Room.Create("101", 10, null, isVirtual: false);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var existing = await SeedSessionAsync(db, teacherId: Guid.NewGuid(), studyGroupId: Guid.NewGuid(), roomId: room.Id);

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, Guid.NewGuid(), room.Id, studyGroupId: Guid.NewGuid(), Start, End);

        conflicts.ShouldContain(c => c.Type == SessionConflictType.Room && c.ConflictingSessionId == existing.Id);
    }

    [Fact]
    public async Task CheckAsync_Should_NotReportRoomConflict_When_RoomIsVirtual()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var room = Room.Create("Zoom", 100, null, isVirtual: true);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        await SeedSessionAsync(db, teacherId: Guid.NewGuid(), studyGroupId: Guid.NewGuid(), roomId: room.Id);

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, Guid.NewGuid(), room.Id, studyGroupId: Guid.NewGuid(), Start, End);

        conflicts.ShouldBeEmpty();
    }

    #endregion

    #region Study group conflict

    [Fact]
    public async Task CheckAsync_Should_ReportStudyGroupConflict_When_SameGroupOverlaps()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var studyGroupId = Guid.NewGuid();
        var existing = await SeedSessionAsync(db, teacherId: Guid.NewGuid(), studyGroupId: studyGroupId);

        var checker = new SessionConflictChecker(db);
        var conflicts = await checker.CheckAsync(
            excludeSessionId: null, Guid.NewGuid(), roomId: null, studyGroupId, Start, End);

        conflicts.ShouldContain(c => c.Type == SessionConflictType.StudyGroup && c.ConflictingSessionId == existing.Id);
    }

    #endregion

    private static async Task<Session> SeedSessionAsync(
        SchedulingDbContext db, Guid teacherId, Guid studyGroupId, Guid? roomId = null)
    {
        var session = Session.Create(studyGroupId, null, teacherId, roomId, Start, End, null, null);
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}
