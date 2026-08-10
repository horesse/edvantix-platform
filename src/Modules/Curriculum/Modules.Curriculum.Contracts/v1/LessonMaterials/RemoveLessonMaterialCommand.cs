using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

public sealed record RemoveLessonMaterialCommand(Guid MaterialId) : ICommand<Unit>;
