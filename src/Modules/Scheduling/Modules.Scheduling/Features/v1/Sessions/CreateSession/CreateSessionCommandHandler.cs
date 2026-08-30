using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Features.v1.Sessions;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CreateSession;

public sealed class CreateSessionCommandHandler(
    SchedulingDbContext dbContext,
    ISessionConflictChecker conflictChecker,
    IStudyGroupQueryService studyGroupQueryService,
    [FromKeyedServices(typeof(SchedulingDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ISessionRealtimeNotifier realtimeNotifier,
    IQuotaService quotas)
    : ICommandHandler<CreateSessionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await studyGroupQueryService.GetBriefAsync(command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"StudyGroup {command.StudyGroupId} not found.");

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            // Soft plan-limit block (402) on sessions scheduled this UTC month. Bulk template
            // generation (GenerateSessions) is intentionally not gated here.
            await quotas.EnsureHeadroomAsync(tenantId, QuotaResource.MonthlySessions, 1, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!command.Force)
        {
            var conflicts = await conflictChecker.CheckAsync(
                    excludeSessionId: null,
                    command.TeacherId,
                    command.RoomId,
                    command.StudyGroupId,
                    command.StartUtc,
                    command.EndUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            if (conflicts.Count > 0)
            {
                throw new CustomException(
                    "The session conflicts with an existing one. Pass force=true to override.",
                    conflicts.Select(c => $"{c.Type} conflicts with session {c.ConflictingSessionId} at {c.ConflictingSessionStartUtc:O}."),
                    HttpStatusCode.Conflict);
            }
        }

        var session = Session.Create(
            command.StudyGroupId,
            command.LessonId,
            command.TeacherId,
            command.RoomId,
            command.StartUtc,
            command.EndUtc,
            command.Topic,
            command.MeetingUrl);

        dbContext.Sessions.Add(session);

        await outboxStore.AddAsync(
            new SessionScheduledIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
                TenantId: tenantId,
                CorrelationId: Guid.NewGuid().ToString(),
                Source: "Scheduling",
                SessionId: session.Id,
                StudyGroupId: session.StudyGroupId,
                StartUtc: session.StartUtc),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await realtimeNotifier.NotifySessionChangedAsync(tenantId, session.ToDto(), cancellationToken).ConfigureAwait(false);
        return session.Id;
    }
}
