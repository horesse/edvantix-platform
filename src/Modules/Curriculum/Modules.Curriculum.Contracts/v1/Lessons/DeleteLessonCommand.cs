using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Lessons;

public sealed record DeleteLessonCommand(Guid LessonId) : ICommand<Unit>;
