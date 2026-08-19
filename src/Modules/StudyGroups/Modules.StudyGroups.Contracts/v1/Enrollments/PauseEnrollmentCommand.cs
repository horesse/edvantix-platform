using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

public sealed record PauseEnrollmentCommand(Guid EnrollmentId) : ICommand<Unit>;
