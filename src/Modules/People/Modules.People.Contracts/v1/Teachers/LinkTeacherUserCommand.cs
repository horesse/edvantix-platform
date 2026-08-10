using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record LinkTeacherUserCommand(Guid TeacherId, string UserId) : ICommand<Unit>;
