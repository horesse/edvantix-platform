namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>"Прошли N из M уроков" for a study group's course — shown on the group card and the
/// student profile's "Группы" tab (see docs/03 Frontend/Карта экранов.md). Lives here, not in
/// Curriculum.Contracts: <see cref="TotalLessons"/> is Curriculum's (lessons of the group's course)
/// but <see cref="PassedLessons"/> is Scheduling's own <c>Session</c> rows — the count of distinct
/// <c>Session.LessonId</c> among held sessions of the group. Computed on the fly, no stored
/// projection (see docs/02 Модули/Curriculum.md → "Прогресс по программе" for the threshold at
/// which a projection becomes worthwhile). Same cross-module ownership reasoning as
/// <see cref="TeacherWorkloadDto"/>.</summary>
public sealed record CourseProgressDto(
    Guid StudyGroupId,
    Guid CourseId,
    int PassedLessons,
    int TotalLessons);
