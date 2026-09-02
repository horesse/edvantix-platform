using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Payments.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.GetLessonMaterials;

public sealed class GetLessonMaterialsQueryHandler(
    CurriculumDbContext dbContext,
    IMaterialsAccessService materialsAccess,
    ICurrentUser currentUser)
    : IQueryHandler<GetLessonMaterialsQuery, IReadOnlyList<LessonMaterialDto>>
{
    public async ValueTask<IReadOnlyList<LessonMaterialDto>> Handle(
        GetLessonMaterialsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // EDX-015 — a student (or their guardian) overdue past the grace window loses the list too,
        // not just the file downloads. No-op for most schools: the tenant flag is OFF by default,
        // so this is one cached tenant-settings read. Staff/teachers are never restricted.
        var access = await materialsAccess
            .GetForUserAsync(currentUser.GetUserId(), cancellationToken)
            .ConfigureAwait(false);
        if (access.Restricted)
        {
            throw new ForbiddenException("Доступ к материалам ограничен из-за задолженности по оплате.");
        }

        var materials = await dbContext.LessonMaterials
            .AsNoTracking()
            .Where(m => m.LessonId == query.LessonId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return materials.Select(m => m.ToDto()).ToList();
    }
}
