using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetStudyGroupById;

public sealed class GetStudyGroupByIdQueryHandler(StudyGroupsDbContext dbContext)
    : IQueryHandler<GetStudyGroupByIdQuery, StudyGroupDetailDto>
{
    public async ValueTask<StudyGroupDetailDto> Handle(GetStudyGroupByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var group = await dbContext.StudyGroups
            .AsNoTracking()
            .Include(g => g.Enrollments)
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == query.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {query.StudyGroupId} not found.");

        return group.ToDetailDto();
    }
}
