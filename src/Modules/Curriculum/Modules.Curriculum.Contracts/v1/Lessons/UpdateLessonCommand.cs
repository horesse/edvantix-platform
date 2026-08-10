using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Lessons;

public sealed record UpdateLessonCommand(
    Guid LessonId,
    string Title,
    string? Objectives,
    string? Content,
    int DurationMinutes) : ICommand<Unit>;
