using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.RescheduleSession;

public sealed class RescheduleSessionCommandHandler(
    SchedulingDbContext dbContext,
    ISessionConflictChecker conflictChecker,
    [FromKeyedServices(typeof(SchedulingDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ISessionRealtimeNotifier realtimeNotifier)
    : ICommandHandler<RescheduleSessionCommand, Guid>
{
    public async ValueTask<Guid> Handle(RescheduleSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var oldSession = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {command.SessionId} not found.");

        var teacherId = command.TeacherId ?? oldSession.TeacherId;
        var roomId = command.RoomId ?? oldSession.RoomId;

        if (!command.Force)
        {
            var conflicts = await conflictChecker.CheckAsync(
                    excludeSessionId: oldSession.Id,
                    teacherId,
                    roomId,
                    oldSession.StudyGroupId,
                    command.NewStartUtc,
                    command.NewEndUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            if (conflicts.Count > 0)
            {
                throw new CustomException(
                    "The new slot conflicts with an existing session. Pass force=true to override.",
                    conflicts.Select(c => $"{c.Type} conflicts with session {c.ConflictingSessionId} at {c.ConflictingSessionStartUtc:O}."),
                    HttpStatusCode.Conflict);
            }
        }

        // Both writes land in the same SaveChangesAsync (EF Core wraps it in one transaction) — a
        // reschedule is never left half-applied (old session stuck Planned with a replacement
        // already visible, or vice versa).
        oldSession.MarkRescheduled();

        var newSession = Session.Create(
            oldSession.StudyGroupId,
            oldSession.LessonId,
            teacherId,
            roomId,
            command.NewStartUtc,
            command.NewEndUtc,
            oldSession.Topic,
            oldSession.MeetingUrl,
            scheduleTemplateId: oldSession.ScheduleTemplateId,
            rescheduledFromId: oldSession.Id);

        dbContext.Sessions.Add(newSession);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new SessionRescheduledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
                TenantId: tenantId,
                CorrelationId: Guid.NewGuid().ToString(),
                Source: "Scheduling",
                SessionId: oldSession.Id,
                NewSessionId: newSession.Id,
                StudyGroupId: oldSession.StudyGroupId,
                OldStartUtc: oldSession.StartUtc,
                NewStartUtc: newSession.StartUtc),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Two broadcasts: the old session's card should flip to "Rescheduled" in place, and the new
        // one should appear at its new slot.
        await realtimeNotifier.NotifySessionChangedAsync(tenantId, oldSession.ToDto(), cancellationToken).ConfigureAwait(false);
        await realtimeNotifier.NotifySessionChangedAsync(tenantId, newSession.ToDto(), cancellationToken).ConfigureAwait(false);

        return newSession.Id;
    }
}
