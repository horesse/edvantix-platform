using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GenerateSessions;

public sealed class GenerateSessionsCommandHandler(
    SchedulingDbContext dbContext,
    IScheduleGeneratorService generatorService,
    IStudyGroupQueryService studyGroupQueryService,
    [FromKeyedServices(typeof(SchedulingDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<GenerateSessionsCommand, GenerationResultDto>
{
    public async ValueTask<GenerationResultDto> Handle(GenerateSessionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await dbContext.ScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == command.ScheduleTemplateId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ScheduleTemplate {command.ScheduleTemplateId} not found.");

        // "Разрешить генерацию" (docs/02 Модули/Scheduling.md → «Подписки») is enforced here,
        // synchronously and dynamically — re-read on every call, not cached from
        // StudyGroupActivatedIntegrationEvent into a flag that could go stale. A dedicated handler
        // for that event would have nothing to do: there's no state to reconcile, unlike
        // StudyGroupFinishedIntegrationEventHandler, which DOES need to stop a template that was
        // already active (see IntegrationEventHandlers/).
        var group = await studyGroupQueryService.GetBriefAsync(template.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"StudyGroup {template.StudyGroupId} not found.");

        if (group.Status != StudyGroupStatus.Active)
        {
            throw new CustomException(
                $"Cannot generate sessions for a study group in status {group.Status}.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        var plan = await generatorService.PlanAsync(
                template, command.HorizonWeeks ?? SchedulingDefaults.DefaultHorizonWeeks, cancellationToken)
            .ConfigureAwait(false);

        // TeacherId is resolved the same way PlanAsync resolved it for conflict-checking (template
        // override, else the group's primary teacher) — reuses the group fetched above.
        var teacherId = template.TeacherId ?? group.PrimaryTeacherId;

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var createdIds = new List<Guid>(plan.ToCreate.Count);
        foreach (var occurrence in plan.ToCreate)
        {
            var session = Session.Create(
                template.StudyGroupId,
                lessonId: null,
                teacherId,
                roomId: template.RoomId,
                occurrence.StartUtc,
                occurrence.EndUtc,
                topic: null,
                meetingUrl: null,
                scheduleTemplateId: template.Id);

            dbContext.Sessions.Add(session);
            createdIds.Add(session.Id);

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
        }

        if (createdIds.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new GenerationResultDto(
            template.Id,
            createdIds,
            plan.Skipped.Select(s => new GenerationSkipDto(s.LocalDate, s.Reason, s.Conflicts)).ToList());
    }
}
