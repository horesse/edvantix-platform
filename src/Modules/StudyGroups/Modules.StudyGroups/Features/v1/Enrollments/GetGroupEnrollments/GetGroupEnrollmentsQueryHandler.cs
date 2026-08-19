using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.GetGroupEnrollments;

public sealed class GetGroupEnrollmentsQueryHandler(StudyGroupsDbContext dbContext)
    : IQueryHandler<GetGroupEnrollmentsQuery, IReadOnlyList<GroupEnrollmentDto>>
{
    public async ValueTask<IReadOnlyList<GroupEnrollmentDto>> Handle(
        GetGroupEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var enrollments = await dbContext.GroupEnrollments
            .AsNoTracking()
            .Where(e => e.StudyGroupId == query.StudyGroupId)
            .OrderBy(e => e.EnrolledOn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return enrollments.Select(e => e.ToDto()).ToList();
    }
}
