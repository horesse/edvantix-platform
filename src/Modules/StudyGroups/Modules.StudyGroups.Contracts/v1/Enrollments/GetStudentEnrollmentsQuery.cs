using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

/// <summary>All groups a student has ever been enrolled in, including finished/left ones —
/// backs the "history" tab on the student profile in People.</summary>
public sealed record GetStudentEnrollmentsQuery(Guid StudentId) : IQuery<IReadOnlyList<GroupEnrollmentDto>>;
