using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.Scheduling.Services;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CreateSession;

public sealed class CreateSessionCommandHandler(
    SchedulingDbContext dbContext,
    ISessionConflictChecker conflictChecker,
    IStudyGroupQueryService studyGroupQueryService)
    : ICommandHandler<CreateSessionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await studyGroupQueryService.GetBriefAsync(command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"StudyGroup {command.StudyGroupId} not found.");

        if (!command.Force)
        {
            var conflicts = await conflictChecker.CheckAsync(
                    excludeSessionId: null,
                    command.TeacherId,
                    command.RoomId,
                    command.StudyGroupId,
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

        var session = Session.Create(
            command.StudyGroupId,
            command.LessonId,
            command.TeacherId,
            command.RoomId,
            command.StartUtc,
            command.EndUtc,
            command.Topic,
            command.MeetingUrl);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session.Id;
    }
}
