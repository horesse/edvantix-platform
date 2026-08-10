using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Lessons;

public sealed record CreateLessonCommand(
    Guid CourseModuleId,
    string Title,
    string? Objectives,
    string? Content,
    int DurationMinutes) : ICommand<Guid>;
