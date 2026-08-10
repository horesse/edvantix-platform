using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace FSH.Modules.Curriculum.Features.v1.Lessons;

internal static class LessonMappings
{
    public static LessonDto ToDto(this Lesson lesson) => new(
        lesson.Id, lesson.CourseModuleId, lesson.Title, lesson.Objectives, lesson.Content,
        lesson.DurationMinutes, lesson.SortOrder);
}
