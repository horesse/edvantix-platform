using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;
using FSH.Modules.StudyGroups.Features.v1.Enrollments;
using FSH.Modules.StudyGroups.Features.v1.Teachers;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups;

internal static class StudyGroupMappings
{
    /// <summary><paramref name="activeEnrollmentCount"/> is supplied by the caller — list queries
    /// compute it via a grouped subquery instead of loading each group's full
    /// <see cref="StudyGroup.Enrollments"/> collection (see SearchStudyGroupsQueryHandler).</summary>
    public static StudyGroupDto ToDto(this StudyGroup g, int activeEnrollmentCount) => new(
        g.Id,
        g.Code,
        g.Name,
        g.CourseId,
        g.PrimaryTeacherId,
        g.Format,
        g.Capacity,
        activeEnrollmentCount,
        g.StartDate,
        g.EndDate,
        g.Status,
        g.ChatChannelId,
        g.MeetingUrl,
        g.RoomId,
        g.Notes,
        g.CreatedAtUtc);

    /// <summary>The group must already have <see cref="StudyGroup.Enrollments"/>/
    /// <see cref="StudyGroup.Teachers"/> loaded (<c>Include</c>) — <see cref="StudyGroup.ActiveEnrollmentCount"/>
    /// reads the in-memory collection.</summary>
    public static StudyGroupDetailDto ToDetailDto(this StudyGroup g) => new(
        g.Id,
        g.Code,
        g.Name,
        g.CourseId,
        g.PrimaryTeacherId,
        g.Format,
        g.Capacity,
        g.ActiveEnrollmentCount,
        g.StartDate,
        g.EndDate,
        g.Status,
        g.ChatChannelId,
        g.MeetingUrl,
        g.RoomId,
        g.Notes,
        g.CreatedAtUtc,
        g.Enrollments.Select(e => e.ToDto()).ToList(),
        g.Teachers.Select(t => t.ToDto()).ToList());
}
