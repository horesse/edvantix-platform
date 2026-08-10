using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Contracts.Events;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.PublishCourse;

public sealed class PublishCourseCommandHandler(
    CurriculumDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<PublishCourseCommand, Unit>
{
    public async ValueTask<Unit> Handle(PublishCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        // "Курс без разделов недопустим" (docs/02 Модули/Curriculum.md → Инварианты) —
        // checked here rather than in the domain method, which has no DB access.
        bool hasModules = await dbContext.CourseModules
            .AnyAsync(m => m.CourseId == course.Id, cancellationToken)
            .ConfigureAwait(false);
        if (!hasModules)
        {
            throw new CustomException(
                "Cannot publish a course without any modules.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        course.Publish();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new CoursePublishedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "Curriculum", course.Id, course.Title, course.SubjectId),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
