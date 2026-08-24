using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.UpdateSession;

public sealed class UpdateSessionCommandHandler(
    SchedulingDbContext dbContext,
    ISessionConflictChecker conflictChecker,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ISessionRealtimeNotifier realtimeNotifier)
    : ICommandHandler<UpdateSessionCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {command.SessionId} not found.");

        if (!command.Force)
        {
            var conflicts = await conflictChecker.CheckAsync(
                    session.Id,
                    command.TeacherId,
                    command.RoomId,
                    session.StudyGroupId,
                    command.StartUtc,
                    command.EndUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            if (conflicts.Count > 0)
            {
                throw new CustomException(
                    "The session conflicts with an existing one. Pass force=true to override.",
                    conflicts.Select(c => $"{c.Type} conflicts with session {c.ConflictingSessionId} at {c.ConflictingSessionStartUtc:O}."),
                    HttpStatusCode.Conflict);
            }
        }

        session.Update(
            command.LessonId,
            command.TeacherId,
            command.RoomId,
            command.StartUtc,
            command.EndUtc,
            command.Topic,
            command.MeetingUrl,
            command.TeacherComment);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await realtimeNotifier.NotifySessionChangedAsync(tenantId, session.ToDto(), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
