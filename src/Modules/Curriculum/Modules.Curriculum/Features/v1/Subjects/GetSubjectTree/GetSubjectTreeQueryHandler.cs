using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.GetSubjectTree;

public sealed class GetSubjectTreeQueryHandler(CurriculumDbContext dbContext)
    : IQueryHandler<GetSubjectTreeQuery, IReadOnlyList<SubjectNodeDto>>
{
    public async ValueTask<IReadOnlyList<SubjectNodeDto>> Handle(GetSubjectTreeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await dbContext.Subjects
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byParent = all.ToLookup(s => s.ParentId);

        IReadOnlyList<SubjectNodeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Select(s => new SubjectNodeDto(s.Id, s.Name, s.Slug, s.SortOrder, Build(s.Id)))
                .ToList();

        return Build(null);
    }
}
