using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record RestoreCourseCommand(Guid CourseId) : ICommand<Guid>;
