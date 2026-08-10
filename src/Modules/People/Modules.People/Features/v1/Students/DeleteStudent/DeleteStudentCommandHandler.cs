using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.DeleteStudent;

public sealed class DeleteStudentCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<DeleteStudentCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        // Interceptor rewrites this Remove() into a soft-delete update (IsDeleted/DeletedOnUtc/DeletedBy) —
        // see AuditableEntitySaveChangesInterceptor.
        dbContext.Students.Remove(student);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
