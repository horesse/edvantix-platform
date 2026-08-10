using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Lessons;

public sealed record ReorderLessonsCommand(
    Guid CourseModuleId,
    IReadOnlyList<Guid> OrderedLessonIds) : ICommand<Unit>;
