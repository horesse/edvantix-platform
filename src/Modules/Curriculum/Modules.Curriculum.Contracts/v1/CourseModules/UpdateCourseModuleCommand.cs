using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.CourseModules;

public sealed record UpdateCourseModuleCommand(Guid CourseModuleId, string Title, string? Description) : ICommand<Unit>;
