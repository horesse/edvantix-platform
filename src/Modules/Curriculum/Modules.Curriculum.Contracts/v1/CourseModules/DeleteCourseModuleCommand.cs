using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.CourseModules;

public sealed record DeleteCourseModuleCommand(Guid CourseModuleId) : ICommand<Unit>;
