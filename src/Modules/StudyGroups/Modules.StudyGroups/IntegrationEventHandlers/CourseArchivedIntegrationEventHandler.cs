using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Curriculum.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.StudyGroups.IntegrationEventHandlers;

/// <summary>
/// "Запретить новые группы по курсу" (docs/02 Модули/StudyGroups.md → «Подписки») is already
/// enforced synchronously — <c>CreateStudyGroupCommandHandler</c> calls
/// <c>ICourseQueryService.IsPublishedAsync</c>, which returns false the instant the course is
/// archived, so no new group for it can be created regardless of this handler.
/// <para>
/// What a synchronous check at creation time cannot catch: a group already created (and still
/// <see cref="StudyGroupStatus.Forming"/>, not yet activated) for a course that gets archived
/// afterward — <c>StudyGroup.Activate</c> only checks enrollment count, not the course's current
/// status. This handler flags those groups so they show up as needing attention instead of being
/// silently activatable against an archived course.
/// </para>
/// </summary>
public sealed class CourseArchivedIntegrationEventHandler(
    StudyGroupsDbContext dbContext,
    ILogger<CourseArchivedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<CourseArchivedIntegrationEvent>
{
    public async Task HandleAsync(CourseArchivedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var affectedGroups = await dbContext.StudyGroups
            .Where(g => g.Status == StudyGroupStatus.Forming && g.CourseId == @event.CourseId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (affectedGroups.Count == 0)
        {
            return;
        }

        foreach (var group in affectedGroups)
        {
            group.AddSystemFlag("Course archived — reassign before activating this group.");
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[StudyGroups] flagged {Count} forming group(s) for archived course {CourseId}",
                affectedGroups.Count, @event.CourseId);
        }
    }
}
