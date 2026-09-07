using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.GetScheduleIcs;

/// <summary>Builds the anonymous VCALENDAR feed. Caller resolved from the subscription token; tenant
/// already resolved from <c>?tenant=</c>. Only <see cref="SessionStatus.Planned"/> /
/// <see cref="SessionStatus.Held"/> sessions are emitted — a cancelled or rescheduled occurrence
/// just drops out of the feed on the next poll. A rescheduled session keeps its predecessor's UID
/// (<c>RescheduledFromId</c>) so calendar clients move the event instead of duplicating it.</summary>
public sealed class GetScheduleIcsQueryHandler(
    IIcalSubscriptionService subscriptions,
    IMyScheduleReader scheduleReader,
    IStudyGroupQueryService studyGroupQueryService,
    SchedulingDbContext dbContext,
    TimeProvider timeProvider)
    : IQueryHandler<GetScheduleIcsQuery, string?>
{
    private static readonly TimeSpan LookBack = TimeSpan.FromDays(7);
    private static readonly TimeSpan LookAhead = TimeSpan.FromDays(60);

    public async ValueTask<string?> Handle(GetScheduleIcsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var userId = await subscriptions.ResolveUserIdAsync(query.Token, cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var from = query.From ?? now - LookBack;
        var to = query.To ?? now + LookAhead;

        var sessions = await scheduleReader
            .ReadAsync(userId.Value.ToString(), from, to, cancellationToken)
            .ConfigureAwait(false);

        var visible = sessions
            .Where(s => s.Status is SessionStatus.Planned or SessionStatus.Held)
            .ToList();

        var groupNames = new Dictionary<Guid, string>();
        foreach (var groupId in visible.Select(s => s.StudyGroupId).Distinct())
        {
            var brief = await studyGroupQueryService.GetBriefAsync(groupId, cancellationToken).ConfigureAwait(false);
            if (brief is not null)
            {
                groupNames[groupId] = $"{brief.Code} — {brief.Name}";
            }
        }

        var roomIds = visible.Where(s => s.RoomId is not null).Select(s => s.RoomId!.Value).Distinct().ToList();
        var roomNames = roomIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Rooms
                .AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken)
                .ConfigureAwait(false);

        var events = new List<IcalEvent>(visible.Count);
        foreach (var s in visible)
        {
            var summary = s.Topic
                ?? (groupNames.TryGetValue(s.StudyGroupId, out var name) ? name : "Занятие");

            string? location = null;
            if (s.RoomId is { } rid && roomNames.TryGetValue(rid, out var room))
            {
                location = room;
            }
            else if (s.MeetingUrl is not null)
            {
                location = "Онлайн";
            }

            events.Add(new IcalEvent(
                Uid: $"{s.RescheduledFromId ?? s.Id:N}@edvantix",
                StartUtc: s.StartUtc,
                EndUtc: s.EndUtc,
                Summary: summary,
                Location: location,
                Description: s.MeetingUrl,
                Cancelled: false,
                LastModifiedUtc: s.UpdatedAtUtc ?? s.CreatedAtUtc));
        }

        return IcalWriter.Build("Моё расписание · Edvantix", events, now);
    }
}
