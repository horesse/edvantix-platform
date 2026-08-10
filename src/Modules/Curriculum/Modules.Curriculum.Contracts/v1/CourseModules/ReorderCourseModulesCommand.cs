using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.CourseModules;

public sealed record ReorderCourseModulesCommand(
    Guid CourseId,
    IReadOnlyList<Guid> OrderedModuleIds) : ICommand<Unit>;
