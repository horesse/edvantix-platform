using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.StudyGroups.IntegrationEventHandlers;

/// <summary>
/// A student who is archived in People can no longer be actively enrolled anywhere — every
/// <see cref="EnrollmentStatus.Active"/>/<see cref="EnrollmentStatus.Paused"/> enrollment for them
/// is closed (see docs/02 Модули/StudyGroups.md → «Подписки»). Tenant context for the DbContext is
/// installed by the event bus (<c>IEventTenantScope</c>, see eventing.md) before this handler
/// resolves — no manual Finbuckle setup needed here.
/// </summary>
public sealed class StudentArchivedIntegrationEventHandler(
    StudyGroupsDbContext dbContext,
    ILogger<StudentArchivedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentArchivedIntegrationEvent>
{
    public async Task HandleAsync(StudentArchivedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var affectedGroups = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .Where(g => g.Status == StudyGroupStatus.Forming || g.Status == StudyGroupStatus.Active)
            .Where(g => g.Enrollments.Any(e => e.StudentId == @event.StudentId
                && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Paused)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (affectedGroups.Count == 0)
        {
            return;
        }

        var leftOn = DateOnly.FromDateTime(@event.ArchivedOn.UtcDateTime);
        int closedCount = 0;
        foreach (var group in affectedGroups)
        {
            foreach (var enrollment in group.Enrollments
                .Where(e => e.StudentId == @event.StudentId
                    && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Paused))
                .ToList())
            {
                group.Unenroll(enrollment.Id, leftOn, "Student archived");
                closedCount++;
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[StudyGroups] closed {Count} enrollment(s) for archived student {StudentId}",
                closedCount, @event.StudentId);
        }
    }
}
