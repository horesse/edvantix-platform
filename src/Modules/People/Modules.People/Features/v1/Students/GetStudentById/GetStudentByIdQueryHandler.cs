using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.GetStudentById;

public sealed class GetStudentByIdQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetStudentByIdQuery, StudentDetailDto>
{
    public async ValueTask<StudentDetailDto> Handle(GetStudentByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var s = await dbContext.Students
            .AsNoTracking()
            .Include(x => x.GuardianLinks)
            .Include(x => x.Notes)
            .FirstOrDefaultAsync(x => x.Id == query.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {query.StudentId} not found.");

        return ToDetailDto(s);
    }

    internal static StudentDetailDto ToDetailDto(Student s) => new(
        s.Id,
        s.LastName,
        s.FirstName,
        s.MiddleName,
        s.DisplayName,
        s.BirthDate,
        s.Phone,
        s.Email,
        s.UserId,
        s.Status,
        s.Source,
        s.AvatarFileId,
        s.ManagerUserId,
        s.EnrolledAtUtc,
        s.CreatedAtUtc,
        s.UpdatedAtUtc,
        s.GuardianLinks.Count(g => !g.IsDeleted),
        s.Notes.Count(n => !n.IsDeleted));
}
