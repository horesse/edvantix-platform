using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record GetTeacherByIdQuery(Guid TeacherId) : IQuery<TeacherDto>;
