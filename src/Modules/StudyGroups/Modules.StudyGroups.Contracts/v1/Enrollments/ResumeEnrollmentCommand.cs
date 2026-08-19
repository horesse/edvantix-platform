using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

public sealed record ResumeEnrollmentCommand(Guid EnrollmentId) : ICommand<Unit>;
