using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Contracts.Events;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.ArchiveCourse;

public sealed class ArchiveCourseCommandHandler(
    CurriculumDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<ArchiveCourseCommand, Unit>
{
    public async ValueTask<Unit> Handle(ArchiveCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        course.Archive();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new CourseArchivedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "Curriculum", course.Id),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
