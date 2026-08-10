using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record UpdateCourseCommand(
    Guid CourseId,
    Guid SubjectId,
    string Title,
    string? Description,
    CourseLevel Level,
    int DurationHours,
    Guid? CoverFileId) : ICommand<Unit>;
