using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record ArchiveStudentCommand(Guid StudentId) : ICommand<Unit>;
