using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Subjects;

public sealed record DeleteSubjectCommand(Guid SubjectId) : ICommand<Unit>;
