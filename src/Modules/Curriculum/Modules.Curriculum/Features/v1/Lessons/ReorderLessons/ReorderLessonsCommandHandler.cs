using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.ReorderLessons;

public sealed class ReorderLessonsCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<ReorderLessonsCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderLessonsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lessons = await dbContext.Lessons
            .Where(l => l.CourseModuleId == command.CourseModuleId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in command.OrderedLessonIds)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == id);
            if (lesson is null)
            {
                continue;
            }
            lesson.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in lessons.Where(l => !seen.Contains(l.Id)).OrderBy(l => l.SortOrder))
        {
            trailing.SetSortOrder(order++);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
