using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

public sealed record ReorderLessonMaterialsCommand(
    Guid LessonId,
    IReadOnlyList<Guid> OrderedMaterialIds) : ICommand<Unit>;
