using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.GetLessonMaterials;

public sealed class GetLessonMaterialsQueryHandler(CurriculumDbContext dbContext)
    : IQueryHandler<GetLessonMaterialsQuery, IReadOnlyList<LessonMaterialDto>>
{
    public async ValueTask<IReadOnlyList<LessonMaterialDto>> Handle(
        GetLessonMaterialsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var materials = await dbContext.LessonMaterials
            .AsNoTracking()
            .Where(m => m.LessonId == query.LessonId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return materials.Select(m => m.ToDto()).ToList();
    }
}
