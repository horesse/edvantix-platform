using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetSessionById;

public sealed class GetSessionByIdQueryHandler(
    SchedulingDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    ICourseQueryService courseQueryService)
    : IQueryHandler<GetSessionByIdQuery, SessionDetailDto>
{
    public async ValueTask<SessionDetailDto> Handle(GetSessionByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {query.SessionId} not found.");

        var attendance = await dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.SessionId == session.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string resolvedTopic = await ResolveTopicAsync(session, cancellationToken).ConfigureAwait(false);

        return new SessionDetailDto(
            session.Id,
            session.StudyGroupId,
            session.LessonId,
            session.TeacherId,
            session.RoomId,
            session.StartUtc,
            session.EndUtc,
            session.Status,
            resolvedTopic,
            session.MeetingUrl,
            session.CancelReason,
            session.RescheduledFromId,
            session.ScheduleTemplateId,
            session.TeacherComment,
            attendance.Select(a => a.ToDto()).ToList());
    }

    /// <summary>ADR-006 "Тема занятия": empty <c>Session.Topic</c> falls back to the linked program
    /// lesson's title, resolved through the group's course — Scheduling has no direct FK to
    /// Curriculum, only <c>LessonId</c>.</summary>
    private async Task<string> ResolveTopicAsync(Session session, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(session.Topic))
        {
            return session.Topic;
        }

        if (session.LessonId is not { } lessonId)
        {
            return string.Empty;
        }

        var group = await studyGroupQueryService.GetBriefAsync(session.StudyGroupId, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            return string.Empty;
        }

        var lessons = await courseQueryService.GetLessonsInOrderAsync(group.CourseId, cancellationToken).ConfigureAwait(false);
        return lessons.FirstOrDefault(l => l.Id == lessonId)?.Title ?? string.Empty;
    }
}
