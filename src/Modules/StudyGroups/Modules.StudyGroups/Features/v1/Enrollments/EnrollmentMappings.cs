using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments;

internal static class EnrollmentMappings
{
    public static GroupEnrollmentDto ToDto(this GroupEnrollment e) => new(
        e.Id,
        e.StudyGroupId,
        e.StudentId,
        e.EnrolledOn,
        e.LeftOn,
        e.Status,
        e.LeaveReason,
        e.TariffId,
        e.DiscountPercent);
}
