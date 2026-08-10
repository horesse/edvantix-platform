using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record PublishCourseCommand(Guid CourseId) : ICommand<Unit>;
