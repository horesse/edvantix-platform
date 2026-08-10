using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.CourseModules;

public sealed record CreateCourseModuleCommand(Guid CourseId, string Title, string? Description) : ICommand<Guid>;
