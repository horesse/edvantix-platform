using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Lessons;

public sealed record GetLessonByIdQuery(Guid LessonId) : IQuery<LessonDto>;
