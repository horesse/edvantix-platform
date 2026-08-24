namespace FSH.Modules.StudyGroups.Contracts.Dtos;

/// <summary>Enrollment shape Payments needs for tariff accrual — narrower than
/// <see cref="GroupEnrollmentDto"/> (no <c>Id</c>/<c>LeaveReason</c>), added specifically for
/// <c>IStudyGroupQueryService.GetActiveEnrollmentsWithTariffAsync</c>.</summary>
public sealed record GroupEnrollmentAccrualDto(
    Guid StudentId,
    DateOnly EnrolledOn,
    DateOnly? LeftOn,
    Guid? TariffId,
    decimal DiscountPercent);
