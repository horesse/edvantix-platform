using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.CreateSubject;

public sealed class CreateSubjectCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<CreateSubjectCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSubjectCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ParentId is { } parentId)
        {
            bool parentExists = await dbContext.Subjects
                .AnyAsync(s => s.Id == parentId, cancellationToken)
                .ConfigureAwait(false);
            if (!parentExists)
            {
                throw new NotFoundException($"Subject {parentId} not found.");
            }
        }

        int nextOrder = await dbContext.Subjects
            .Where(s => s.ParentId == command.ParentId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) is { } max ? max + 1 : 0;

        var subject = Subject.Create(command.Name, command.ParentId, nextOrder);

        bool slugTaken = await dbContext.Subjects
            .AnyAsync(s => s.Slug == subject.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A subject with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subject.Id;
    }
}
