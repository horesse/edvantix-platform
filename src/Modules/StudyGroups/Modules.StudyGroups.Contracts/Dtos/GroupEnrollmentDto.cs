namespace FSH.Modules.StudyGroups.Contracts.Dtos;

public sealed record GroupEnrollmentDto(
    Guid Id,
    Guid StudyGroupId,
    Guid StudentId,
    DateOnly EnrolledOn,
    DateOnly? LeftOn,
    EnrollmentStatus Status,
    string? LeaveReason,
    Guid? TariffId,
    decimal DiscountPercent);
