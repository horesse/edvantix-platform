using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.GetLessonById;

public sealed class GetLessonByIdQueryHandler(CurriculumDbContext dbContext)
    : IQueryHandler<GetLessonByIdQuery, LessonDto>
{
    public async ValueTask<LessonDto> Handle(GetLessonByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var lesson = await dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.LessonId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Lesson {query.LessonId} not found.");

        return lesson.ToDto();
    }
}
