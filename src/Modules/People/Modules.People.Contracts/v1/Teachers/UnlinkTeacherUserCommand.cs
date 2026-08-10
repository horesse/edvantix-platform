using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record UnlinkTeacherUserCommand(Guid TeacherId) : ICommand<Unit>;
