using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Subjects;

public sealed record GetSubjectTreeQuery : IQuery<IReadOnlyList<SubjectNodeDto>>;
