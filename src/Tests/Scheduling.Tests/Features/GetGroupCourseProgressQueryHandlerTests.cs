using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.StudyGroups;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Features.v1.StudyGroups.GetGroupCourseProgress;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using NSubstitute;
using Scheduling.Tests.Services;

namespace Scheduling.Tests.Features;

public sealed class GetGroupCourseProgressQueryHandlerTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Throws_NotFound_When_Group_Does_Not_Exist()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        studyGroupQueryService.GetBriefAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StudyGroupBriefDto?)null);
        var courseQueryService = Substitute.For<ICourseQueryService>();

        var handler = new GetGroupCourseProgressQueryHandler(db, studyGroupQueryService, courseQueryService);

        await Should.ThrowAsync<NotFoundException>(
            () => handler.Handle(new GetGroupCourseProgressQuery(GroupId), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_Counts_Distinct_Held_Lessons_Of_The_Course()
    {
        var lessonA = Guid.NewGuid();
        var lessonB = Guid.NewGuid();
        var lessonC = Guid.NewGuid();
        var foreignLesson = Guid.NewGuid();

        await using var db = TestSchedulingDbContextFactory.Create();

        // Held, lesson A — counts.
        db.Sessions.Add(HeldSession(lessonA));
        // Held, lesson A again — de-duplicated, still one.
        db.Sessions.Add(HeldSession(lessonA));
        // Held, lesson B — counts.
        db.Sessions.Add(HeldSession(lessonB));
        // Planned, lesson C — not held, excluded.
        db.Sessions.Add(Session.Create(
            GroupId, lessonC, Guid.NewGuid(), null,
            new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 4, 11, 0, 0, TimeSpan.Zero),
            null, null));
        // Held, no lesson (trial/consultation) — excluded.
        db.Sessions.Add(HeldSession(null));
        // Held, a lesson that is not part of this course — excluded from "passed".
        db.Sessions.Add(HeldSession(foreignLesson));
        // Held, but another group — excluded.
        var otherGroup = Session.Create(
            Guid.NewGuid(), lessonB, Guid.NewGuid(), null,
            new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 5, 11, 0, 0, TimeSpan.Zero),
            null, null);
        otherGroup.Hold();
        db.Sessions.Add(otherGroup);

        await db.SaveChangesAsync();

        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        studyGroupQueryService.GetBriefAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(GroupId, "SG-1", "Group 1", CourseId, Guid.NewGuid(), StudyGroupStatus.Active));

        var courseQueryService = Substitute.For<ICourseQueryService>();
        courseQueryService.GetLessonsInOrderAsync(CourseId, Arg.Any<CancellationToken>())
            .Returns(new List<LessonBriefDto>
            {
                new(lessonA, Guid.NewGuid(), "L1", 0),
                new(lessonB, Guid.NewGuid(), "L2", 1),
                new(lessonC, Guid.NewGuid(), "L3", 2),
                new(Guid.NewGuid(), Guid.NewGuid(), "L4", 3),
            });

        var handler = new GetGroupCourseProgressQueryHandler(db, studyGroupQueryService, courseQueryService);

        var result = await handler.Handle(new GetGroupCourseProgressQuery(GroupId), CancellationToken.None);

        result.StudyGroupId.ShouldBe(GroupId);
        result.CourseId.ShouldBe(CourseId);
        result.TotalLessons.ShouldBe(4);
        result.PassedLessons.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_Returns_Zero_Passed_When_No_Sessions_Held()
    {
        await using var db = TestSchedulingDbContextFactory.Create();

        var studyGroupQueryService = Substitute.For<IStudyGroupQueryService>();
        studyGroupQueryService.GetBriefAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(new StudyGroupBriefDto(GroupId, "SG-1", "Group 1", CourseId, Guid.NewGuid(), StudyGroupStatus.Forming));

        var courseQueryService = Substitute.For<ICourseQueryService>();
        courseQueryService.GetLessonsInOrderAsync(CourseId, Arg.Any<CancellationToken>())
            .Returns(new List<LessonBriefDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "L1", 0),
                new(Guid.NewGuid(), Guid.NewGuid(), "L2", 1),
            });

        var handler = new GetGroupCourseProgressQueryHandler(db, studyGroupQueryService, courseQueryService);

        var result = await handler.Handle(new GetGroupCourseProgressQuery(GroupId), CancellationToken.None);

        result.PassedLessons.ShouldBe(0);
        result.TotalLessons.ShouldBe(2);
    }

    private static Session HeldSession(Guid? lessonId)
    {
        var session = Session.Create(
            GroupId, lessonId, Guid.NewGuid(), null,
            new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 11, 0, 0, TimeSpan.Zero),
            null, null);
        session.Hold();
        return session;
    }
}
