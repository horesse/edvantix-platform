using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

/// <summary>Enrolls one or more students in one call — the handler checks
/// <c>Capacity</c> against the resulting total and rejects the whole batch (not row-by-row) if it
/// would be exceeded, so a partial enrollment never happens silently.</summary>
public sealed record EnrollStudentsCommand(
    Guid StudyGroupId,
    IReadOnlyList<Guid> StudentIds,
    DateOnly? EnrolledOn = null,
    Guid? TariffId = null,
    decimal DiscountPercent = 0) : ICommand<IReadOnlyList<Guid>>;
