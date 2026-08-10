using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.RemoveLessonMaterial;

public sealed class RemoveLessonMaterialCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<RemoveLessonMaterialCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveLessonMaterialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await dbContext.LessonMaterials
            .FirstOrDefaultAsync(m => m.Id == command.MaterialId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Lesson material {command.MaterialId} not found.");

        dbContext.LessonMaterials.Remove(material);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
