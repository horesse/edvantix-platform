using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

/// <summary>Not in the original contract table (only Deactivate is documented) but the natural
/// counterpart — Teacher.Activate() exists on the aggregate, this just exposes it.</summary>
public sealed record ActivateTeacherCommand(Guid TeacherId) : ICommand<Unit>;
