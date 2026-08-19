using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.GetStudentEnrollments;

/// <summary>All groups a student has ever been enrolled in — including <c>Left</c>/<c>Completed</c>
/// rows, unlike <see cref="GetGroupEnrollments.GetGroupEnrollmentsQueryHandler"/>'s per-group view,
/// this is a cross-group history for the student's People profile.</summary>
public sealed class GetStudentEnrollmentsQueryHandler(StudyGroupsDbContext dbContext)
    : IQueryHandler<GetStudentEnrollmentsQuery, IReadOnlyList<GroupEnrollmentDto>>
{
    public async ValueTask<IReadOnlyList<GroupEnrollmentDto>> Handle(
        GetStudentEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var enrollments = await dbContext.GroupEnrollments
            .AsNoTracking()
            .Where(e => e.StudentId == query.StudentId)
            .OrderByDescending(e => e.EnrolledOn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return enrollments.Select(e => e.ToDto()).ToList();
    }
}
