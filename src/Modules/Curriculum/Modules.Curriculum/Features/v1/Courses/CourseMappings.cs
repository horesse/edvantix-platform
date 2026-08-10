using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace FSH.Modules.Curriculum.Features.v1.Courses;

internal static class CourseMappings
{
    public static CourseDto ToDto(this Course course) => new(
        course.Id,
        course.SubjectId,
        course.Title,
        course.Slug,
        course.Description,
        course.Level,
        course.DurationHours,
        course.Status,
        course.CoverFileId,
        course.PublishedAtUtc,
        course.CreatedAtUtc);
}
