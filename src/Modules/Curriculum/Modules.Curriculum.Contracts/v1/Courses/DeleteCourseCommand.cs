using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record DeleteCourseCommand(Guid CourseId) : ICommand<Unit>;
