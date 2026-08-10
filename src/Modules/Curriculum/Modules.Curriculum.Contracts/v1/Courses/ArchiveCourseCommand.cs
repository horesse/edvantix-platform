using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record ArchiveCourseCommand(Guid CourseId) : ICommand<Unit>;
