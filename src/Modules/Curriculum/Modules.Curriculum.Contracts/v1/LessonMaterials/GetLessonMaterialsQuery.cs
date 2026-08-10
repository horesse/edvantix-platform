using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

public sealed record GetLessonMaterialsQuery(Guid LessonId) : IQuery<IReadOnlyList<LessonMaterialDto>>;
