using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Teachers.GetTeacherById;

public sealed class GetTeacherByIdQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetTeacherByIdQuery, TeacherDto>
{
    public async ValueTask<TeacherDto> Handle(GetTeacherByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var t = await dbContext.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {query.TeacherId} not found.");

        return ToDto(t);
    }

    internal static TeacherDto ToDto(Teacher t) => new(
        t.Id, t.LastName, t.FirstName, t.MiddleName, t.DisplayName, t.Phone, t.Email, t.UserId,
        t.Status, t.Bio, t.GetSpecializations(), t.HourlyRate, t.AvatarFileId);
}
