using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Teachers;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Teachers.GetTeacherWorkload;

/// <summary>Lives in Scheduling, not People — see <c>TeacherWorkloadDto</c> remarks. "Active groups"
/// comes from <c>IStudyGroupQueryService</c>; "sessions"/"hours" are Scheduling's own <c>Session</c>
/// rows, whose <see cref="Session.TeacherId"/> is always the resolved effective teacher (template
/// override or the group's <c>PrimaryTeacherId</c>, baked in at generation time — see
/// <c>ScheduleGeneratorService</c>), so no per-session fallback resolution is needed here.</summary>
public sealed class GetTeacherWorkloadQueryHandler(
    SchedulingDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    IPeopleLookupService peopleLookupService)
    : IQueryHandler<GetTeacherWorkloadQuery, TeacherWorkloadDto>
{
    public async ValueTask<TeacherWorkloadDto> Handle(GetTeacherWorkloadQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        _ = await peopleLookupService.GetTeacherBriefAsync(query.TeacherId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {query.TeacherId} not found.");

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var from = query.From ?? today;
        var to = query.To ?? from.AddDays(SchedulingDefaults.DefaultWorkloadWindowDays);

        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var groupIds = await studyGroupQueryService.GetActiveGroupIdsForTeacherAsync(query.TeacherId, cancellationToken)
            .ConfigureAwait(false);

        var sessions = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.TeacherId == query.TeacherId
                && s.StartUtc >= fromUtc && s.StartUtc <= toUtc
                && s.Status != SessionStatus.Cancelled && s.Status != SessionStatus.Rescheduled)
            .Select(s => new { s.StartUtc, s.EndUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal totalHours = sessions.Sum(s => (decimal)(s.EndUtc - s.StartUtc).TotalHours);

        return new TeacherWorkloadDto(query.TeacherId, from, to, groupIds.Count, sessions.Count, totalHours);
    }
}
