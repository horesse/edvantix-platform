using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.ReorderSubjects;

public sealed class ReorderSubjectsCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<ReorderSubjectsCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderSubjectsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var siblings = await dbContext.Subjects
            .Where(s => s.ParentId == command.ParentId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in command.OrderedSubjectIds)
        {
            var subject = siblings.FirstOrDefault(s => s.Id == id);
            if (subject is null)
            {
                continue;
            }
            subject.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in siblings.Where(s => !seen.Contains(s.Id)).OrderBy(s => s.SortOrder))
        {
            trailing.SetSortOrder(order++);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
