using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Teachers;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Features.v1.Teachers.GetTeacherWorkload;
using FSH.Modules.StudyGroups.Contracts;
using NSubstitute;
using Scheduling.Tests.Services;

namespace Scheduling.Tests.Features;

public sealed class GetTeacherWorkloadQueryHandlerTests
{
    [Fact]
    public async Task Handle_Throws_NotFound_When_Teacher_Does_Not_Exist()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        var peopleLookupService = Substitute.For<IPeopleLookupService>();
        peopleLookupService.GetTeacherBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PersonBriefDto?)null);

        var handler = new GetTeacherWorkloadQueryHandler(db, studyGroupQueryService, peopleLookupService);

        await Should.ThrowAsync<NotFoundException>(
            () => handler.Handle(new GetTeacherWorkloadQuery(Guid.NewGuid(), null, null), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_Counts_Sessions_And_Hours_Within_Period_For_The_Teacher()
    {
        var teacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 7);

        await using var db = TestSchedulingDbContextFactory.Create();

        // In range, this teacher, Planned — counted (1 hour).
        db.Sessions.Add(Session.Create(
            Guid.NewGuid(), null, teacherId, null,
            new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            null, null));

        // In range, this teacher, but Cancelled — excluded.
        var cancelled = Session.Create(
            Guid.NewGuid(), null, teacherId, null,
            new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 3, 11, 30, 0, TimeSpan.Zero),
            null, null);
        cancelled.Cancel("test");
        db.Sessions.Add(cancelled);

        // In range, but a different teacher — excluded.
        db.Sessions.Add(Session.Create(
            Guid.NewGuid(), null, otherTeacherId, null,
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 13, 0, 0, TimeSpan.Zero),
            null, null));

        // Out of range — excluded.
        db.Sessions.Add(Session.Create(
            Guid.NewGuid(), null, teacherId, null,
            new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 20, 11, 0, 0, TimeSpan.Zero),
            null, null));

        await db.SaveChangesAsync();

        var groupIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        studyGroupQueryService.GetActiveGroupIdsForTeacherAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns(groupIds);

        var peopleLookupService = Substitute.For<IPeopleLookupService>();
        peopleLookupService.GetTeacherBriefAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns(new PersonBriefDto(teacherId, "Test Teacher", null));

        var handler = new GetTeacherWorkloadQueryHandler(db, studyGroupQueryService, peopleLookupService);

        var result = await handler.Handle(new GetTeacherWorkloadQuery(teacherId, from, to), CancellationToken.None);

        result.TeacherId.ShouldBe(teacherId);
        result.ActiveGroupsCount.ShouldBe(2);
        result.SessionsCount.ShouldBe(1);
        result.TotalHours.ShouldBe(1m);
    }

    [Fact]
    public async Task Handle_Defaults_To_A_Week_Window_When_Period_Omitted()
    {
        var teacherId = Guid.NewGuid();
        await using var db = TestSchedulingDbContextFactory.Create();

        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        studyGroupQueryService.GetActiveGroupIdsForTeacherAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns([]);

        var peopleLookupService = Substitute.For<IPeopleLookupService>();
        peopleLookupService.GetTeacherBriefAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns(new PersonBriefDto(teacherId, "Test Teacher", null));

        var handler = new GetTeacherWorkloadQueryHandler(db, studyGroupQueryService, peopleLookupService);

        var result = await handler.Handle(new GetTeacherWorkloadQuery(teacherId, null, null), CancellationToken.None);

        (result.To.DayNumber - result.From.DayNumber).ShouldBe(7);
    }
}
