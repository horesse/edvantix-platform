using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.ReorderLessonMaterials;

public sealed class ReorderLessonMaterialsCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<ReorderLessonMaterialsCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderLessonMaterialsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var materials = await dbContext.LessonMaterials
            .Where(m => m.LessonId == command.LessonId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in command.OrderedMaterialIds)
        {
            var material = materials.FirstOrDefault(m => m.Id == id);
            if (material is null)
            {
                continue;
            }
            material.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in materials.Where(m => !seen.Contains(m.Id)).OrderBy(m => m.SortOrder))
        {
            trailing.SetSortOrder(order++);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
