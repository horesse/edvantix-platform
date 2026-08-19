using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.StudyGroups.IntegrationEventHandlers;

/// <summary>
/// Flags forming/active groups that lose their only teacher when a teacher is deactivated in
/// People (see docs/02 Модули/StudyGroups.md → «Подписки», "пометить группы без преподавателя").
/// Does not reassign or block anything automatically — a school still has to pick a replacement,
/// there is no signal for who that should be — it appends an operational note so the group shows
/// up as needing attention on the group list/detail screen.
/// </summary>
public sealed class TeacherDeactivatedIntegrationEventHandler(
    StudyGroupsDbContext dbContext,
    ILogger<TeacherDeactivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<TeacherDeactivatedIntegrationEvent>
{
    public async Task HandleAsync(TeacherDeactivatedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var affectedGroups = await dbContext.StudyGroups
            .Include(g => g.Teachers)
            .Where(g => g.Status == StudyGroupStatus.Forming || g.Status == StudyGroupStatus.Active)
            .Where(g => g.PrimaryTeacherId == @event.TeacherId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (affectedGroups.Count == 0)
        {
            return;
        }

        int flaggedCount = 0;
        foreach (var group in affectedGroups)
        {
            // Only flag if nobody else on the roster can cover — a group with an active
            // assistant/substitute already has someone to fall back on.
            bool hasCover = group.Teachers.Any(t => t.TeacherId != @event.TeacherId);
            if (hasCover)
            {
                continue;
            }

            group.AddSystemFlag("Primary teacher deactivated — group has no active teacher.");
            flaggedCount++;
        }

        if (flaggedCount > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[StudyGroups] flagged {Count} group(s) left without a teacher after teacher {TeacherId} was deactivated",
                flaggedCount, @event.TeacherId);
        }
    }
}
