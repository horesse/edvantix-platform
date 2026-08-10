using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Students.AddStudentGuardian;

public sealed class AddStudentGuardianCommandHandler(
    PeopleDbContext dbContext,
    IOutboxStore outboxStore,
    HybridCache cache,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<AddStudentGuardianCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddStudentGuardianCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .Include(s => s.GuardianLinks)
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        var guardian = await dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == command.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {command.GuardianId} not found.");

        var link = student.AddGuardianLink(command.GuardianId, command.Relation, command.IsPrimaryPayer);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new GuardianLinkedToStudentIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "People", command.GuardianId, student.Id, link.IsPrimaryPayer),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // WardStudentIds on the guardian's cached PeopleScope just changed.
        if (guardian.UserId is { } guardianUserId)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.User(guardianUserId), cancellationToken).ConfigureAwait(false);
        }

        return link.Id;
    }
}
