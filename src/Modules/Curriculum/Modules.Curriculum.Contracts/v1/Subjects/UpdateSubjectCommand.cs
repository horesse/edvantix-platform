using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Subjects;

public sealed record UpdateSubjectCommand(Guid SubjectId, string Name, Guid? ParentId) : ICommand<Unit>;
