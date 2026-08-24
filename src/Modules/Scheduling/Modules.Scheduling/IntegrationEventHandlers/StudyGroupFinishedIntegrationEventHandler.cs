using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.StudyGroups.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Scheduling.IntegrationEventHandlers;

/// <summary>
/// "Остановить генерацию" (docs/02 Модули/Scheduling.md → «Подписки»): deactivates every
/// <c>ScheduleTemplate</c> for the finished group so neither the daily <c>GenerateSessionsJob</c>
/// nor a manual <c>GenerateSessionsCommand</c> keeps producing sessions for a group that's done.
/// Unlike "разрешить генерацию" on activation (handled synchronously in
/// <c>GenerateSessionsCommandHandler</c> — no cached flag to go stale there), this one genuinely
/// needs to flip persisted state: an already-<c>IsActive</c> template has to be told to stop.
/// </summary>
public sealed class StudyGroupFinishedIntegrationEventHandler(
    SchedulingDbContext dbContext,
    ILogger<StudyGroupFinishedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudyGroupFinishedIntegrationEvent>
{
    public async Task HandleAsync(StudyGroupFinishedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var templates = await dbContext.ScheduleTemplates
            .Where(t => t.StudyGroupId == @event.StudyGroupId && t.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (templates.Count == 0)
        {
            return;
        }

        foreach (var template in templates)
        {
            template.Update(
                template.DayOfWeek,
                template.StartTime,
                template.DurationMinutes,
                template.RoomId,
                template.TeacherId,
                template.ValidFrom,
                template.ValidTo,
                isActive: false);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Scheduling] deactivated {Count} schedule template(s) for finished study group {StudyGroupId}",
                templates.Count, @event.StudyGroupId);
        }
    }
}
