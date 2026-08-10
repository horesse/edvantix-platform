using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record GetCourseByIdQuery(Guid CourseId) : IQuery<CourseDetailDto>;
