using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.StudyGroups;

/// <summary>Progress of a study group through its course program — "N of M lessons passed",
/// computed on the fly from held <c>Session</c>s with a non-null <c>LessonId</c>. Not paginated,
/// so no validator (same as <c>GetTeacherWorkloadQuery</c>). Mapped by Scheduling under
/// StudyGroups' resource name (<c>GET /study-groups/{id}/course-progress</c>), gated by Scheduling's
/// own <c>Sessions.View</c>.</summary>
public sealed record GetGroupCourseProgressQuery(Guid StudyGroupId) : IQuery<CourseProgressDto>;
