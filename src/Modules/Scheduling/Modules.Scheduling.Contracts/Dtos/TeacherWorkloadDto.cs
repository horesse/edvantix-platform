namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>Summary for a teacher's profile card ("группы, нагрузка, расписание" — see
/// docs/03 Frontend/Карта экранов.md). Lives here, not in People.Contracts: computing it needs
/// <c>StudyGroups.Contracts</c> (active groups) and Scheduling's own <c>Session</c> rows, and People
/// is the foundational module — it must not depend on modules loaded after it (order 550 vs.
/// StudyGroups 610/Scheduling 620). Same reasoning already applied to
/// <c>GET /students/{id}/attendance</c>, which Scheduling maps despite "students" being People's
/// resource name.</summary>
public sealed record TeacherWorkloadDto(
    Guid TeacherId,
    DateOnly From,
    DateOnly To,
    int ActiveGroupsCount,
    int SessionsCount,
    decimal TotalHours);
