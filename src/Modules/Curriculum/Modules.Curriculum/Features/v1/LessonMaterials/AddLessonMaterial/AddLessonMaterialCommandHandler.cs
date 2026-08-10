using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.Events;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;

public sealed class AddLessonMaterialCommandHandler(
    CurriculumDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<AddLessonMaterialCommand, LessonMaterialDto>
{
    public async ValueTask<LessonMaterialDto> Handle(AddLessonMaterialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool lessonExists = await dbContext.Lessons
            .AnyAsync(l => l.Id == command.LessonId, cancellationToken)
            .ConfigureAwait(false);
        if (!lessonExists)
        {
            throw new NotFoundException($"Lesson {command.LessonId} not found.");
        }

        int nextOrder = await dbContext.LessonMaterials
            .Where(m => m.LessonId == command.LessonId)
            .Select(m => (int?)m.SortOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) is { } max ? max + 1 : 0;

        var material = LessonMaterial.Create(
            command.LessonId, command.Kind, command.Title, command.FileId, command.Url,
            command.VisibleToStudents, nextOrder);

        dbContext.LessonMaterials.Add(material);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new LessonMaterialAddedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "Curriculum", material.LessonId, material.Id, material.Kind),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return material.ToDto();
    }
}
