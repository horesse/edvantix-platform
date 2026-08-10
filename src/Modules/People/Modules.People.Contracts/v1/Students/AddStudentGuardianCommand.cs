using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record AddStudentGuardianCommand(
    Guid StudentId,
    Guid GuardianId,
    string Relation,
    bool IsPrimaryPayer = false) : ICommand<Guid>;
