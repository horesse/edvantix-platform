using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.StudyGroups;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.StudyGroups.GetGroupCourseProgress;

/// <summary>Lives in Scheduling, not Curriculum — see <c>CourseProgressDto</c> remarks. The group's
/// <c>CourseId</c> and the course's lesson list come from Curriculum (<see cref="ICourseQueryService"/>
/// via <see cref="IStudyGroupQueryService"/>); "passed" is the count of distinct
/// <c>Session.LessonId</c> among <see cref="SessionStatus.Held"/> sessions of the group, intersected
/// with the current course lesson set so a stale link to a lesson that has since been moved to
/// another course (or deleted) is not counted above <c>TotalLessons</c>.</summary>
public sealed class GetGroupCourseProgressQueryHandler(
    SchedulingDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    ICourseQueryService courseQueryService)
    : IQueryHandler<GetGroupCourseProgressQuery, CourseProgressDto>
{
    public async ValueTask<CourseProgressDto> Handle(
        GetGroupCourseProgressQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var group = await studyGroupQueryService.GetBriefAsync(query.StudyGroupId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {query.StudyGroupId} not found.");

        var lessons = await courseQueryService.GetLessonsInOrderAsync(group.CourseId, cancellationToken)
            .ConfigureAwait(false);
        var courseLessonIds = lessons.Select(l => l.Id).ToHashSet();

        var heldLessonIds = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StudyGroupId == query.StudyGroupId
                && s.Status == SessionStatus.Held
                && s.LessonId != null)
            .Select(s => s.LessonId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var passedLessons = heldLessonIds.Count(courseLessonIds.Contains);

        return new CourseProgressDto(query.StudyGroupId, group.CourseId, passedLessons, courseLessonIds.Count);
    }
}
