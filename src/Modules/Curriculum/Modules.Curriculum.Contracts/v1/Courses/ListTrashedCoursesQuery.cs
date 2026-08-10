using FSH.Framework.Shared.Persistence;
using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

public sealed record ListTrashedCoursesQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<CourseDto>>;
