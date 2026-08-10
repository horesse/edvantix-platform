using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

/// <summary>Moves an Archived student back to Active — business-status restore, distinct from
/// the soft-delete trash restore (see Student.Reactivate vs Student.Restore).</summary>
public sealed record RestoreStudentCommand(Guid StudentId) : ICommand<Unit>;
