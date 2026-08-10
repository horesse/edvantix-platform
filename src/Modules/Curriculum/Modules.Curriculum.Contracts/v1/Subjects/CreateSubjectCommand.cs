using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Subjects;

public sealed record CreateSubjectCommand(string Name, Guid? ParentId) : ICommand<Guid>;
