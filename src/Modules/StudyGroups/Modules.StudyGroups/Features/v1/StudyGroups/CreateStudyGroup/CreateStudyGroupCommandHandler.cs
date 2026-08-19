using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CreateStudyGroup;

public sealed class CreateStudyGroupCommandHandler(
    StudyGroupsDbContext dbContext,
    ICourseQueryService courseQueryService,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<CreateStudyGroupCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // "Курс должен быть Published" (docs/02 Модули/StudyGroups.md → Инварианты) — checked via
        // Curriculum's cross-module service, not a local FK (different modules, Contracts-only link).
        bool isPublished = await courseQueryService.IsPublishedAsync(command.CourseId, cancellationToken)
            .ConfigureAwait(false);
        if (!isPublished)
        {
            throw new CustomException(
                $"Course {command.CourseId} is not published; a study group cannot be created for it.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        bool codeTaken = await dbContext.StudyGroups
            .AnyAsync(g => g.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"A study group with code '{command.Code}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        var group = StudyGroup.Create(
            command.Code,
            command.Name,
            command.CourseId,
            command.PrimaryTeacherId,
            command.Format,
            command.Capacity,
            command.StartDate,
            command.EndDate,
            command.MeetingUrl,
            command.RoomId,
            command.Notes);

        dbContext.StudyGroups.Add(group);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new StudyGroupCreatedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "StudyGroups", group.Id, group.Name, group.CourseId, group.PrimaryTeacherId),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return group.Id;
    }
}
