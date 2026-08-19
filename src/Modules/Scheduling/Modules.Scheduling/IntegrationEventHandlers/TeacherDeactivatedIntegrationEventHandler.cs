using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.People.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Scheduling.IntegrationEventHandlers;

/// <summary>
/// "Пометить занятия без преподавателя" (docs/02 Модули/Scheduling.md → «Подписки»). Does not
/// reassign or cancel anything — same operational-flag-only philosophy as StudyGroups'
/// <c>TeacherDeactivatedIntegrationEventHandler</c>: appends a note to <c>Session.TeacherComment</c>
/// (reused rather than adding a new column) for every future <c>Planned</c> session still assigned
/// to the deactivated teacher, so it surfaces on the calendar as needing a substitute. Idempotent —
/// skips sessions already carrying the note (duplicate delivery, or the note was added twice).
/// </summary>
public sealed class TeacherDeactivatedIntegrationEventHandler(
    SchedulingDbContext dbContext,
    ILogger<TeacherDeactivatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<TeacherDeactivatedIntegrationEvent>
{
    private const string Note = "Teacher deactivated — needs reassignment.";

    public async Task HandleAsync(TeacherDeactivatedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var nowUtc = TimeProvider.System.GetUtcNow();
        var affected = await dbContext.Sessions
            .Where(s => s.TeacherId == @event.TeacherId
                && s.Status == SessionStatus.Planned
                && s.StartUtc >= nowUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int flaggedCount = 0;
        foreach (var session in affected)
        {
            if (session.TeacherComment?.Contains(Note, StringComparison.Ordinal) == true)
            {
                continue;
            }

            var comment = string.IsNullOrWhiteSpace(session.TeacherComment)
                ? Note
                : $"{session.TeacherComment} {Note}";

            session.Update(
                session.LessonId,
                session.TeacherId,
                session.RoomId,
                session.StartUtc,
                session.EndUtc,
                session.Topic,
                session.MeetingUrl,
                comment);
            flaggedCount++;
        }

        if (flaggedCount > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Scheduling] flagged {Count} future session(s) left without a teacher after teacher {TeacherId} was deactivated",
                flaggedCount, @event.TeacherId);
        }
    }
}
