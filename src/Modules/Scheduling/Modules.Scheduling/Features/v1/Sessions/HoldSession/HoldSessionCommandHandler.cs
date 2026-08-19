using FSH.Framework.Core.Exceptions;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.HoldSession;

public sealed class HoldSessionCommandHandler(
    SchedulingDbContext dbContext,
    IStudyGroupQueryService studyGroupQueryService,
    ITenantSettingsService tenantSettingsService)
    : ICommandHandler<HoldSessionCommand, Unit>
{
    public async ValueTask<Unit> Handle(HoldSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {command.SessionId} not found.");

        bool wasAlreadyHeld = session.Status == SessionStatus.Held;
        session.Hold();

        if (!wasAlreadyHeld)
        {
            await SeedAttendanceAsync(session, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private async Task SeedAttendanceAsync(Session session, CancellationToken cancellationToken)
    {
        var settings = await tenantSettingsService.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(session.StartUtc.UtcDateTime, timeZone));

        var studentIds = await studyGroupQueryService
            .GetActiveStudentIdsAsync(session.StudyGroupId, localDate, cancellationToken)
            .ConfigureAwait(false);

        if (studentIds.Count == 0)
        {
            return;
        }

        var existingStudentIds = await dbContext.Attendances
            .Where(a => a.SessionId == session.Id)
            .Select(a => a.StudentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existing = existingStudentIds.ToHashSet();

        foreach (var studentId in studentIds)
        {
            if (existing.Contains(studentId))
            {
                continue;
            }

            dbContext.Attendances.Add(Attendance.CreateDefault(session.Id, studentId));
        }
    }
}
