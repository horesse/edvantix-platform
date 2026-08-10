using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.CreateCourseModule;

public sealed class CreateCourseModuleCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<CreateCourseModuleCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCourseModuleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool courseExists = await dbContext.Courses
            .AnyAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false);
        if (!courseExists)
        {
            throw new NotFoundException($"Course {command.CourseId} not found.");
        }

        int nextOrder = await dbContext.CourseModules
            .Where(m => m.CourseId == command.CourseId)
            .Select(m => (int?)m.SortOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) is { } max ? max + 1 : 0;

        var module = CourseModule.Create(command.CourseId, command.Title, command.Description, nextOrder);
        dbContext.CourseModules.Add(module);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return module.Id;
    }
}
