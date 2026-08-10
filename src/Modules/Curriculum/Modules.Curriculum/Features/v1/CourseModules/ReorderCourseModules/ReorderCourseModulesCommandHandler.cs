using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.ReorderCourseModules;

public sealed class ReorderCourseModulesCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<ReorderCourseModulesCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderCourseModulesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var modules = await dbContext.CourseModules
            .Where(m => m.CourseId == command.CourseId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in command.OrderedModuleIds)
        {
            var module = modules.FirstOrDefault(m => m.Id == id);
            if (module is null)
            {
                continue;
            }
            module.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in modules.Where(m => !seen.Contains(m.Id)).OrderBy(m => m.SortOrder))
        {
            trailing.SetSortOrder(order++);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
