using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.UpdateSession;

public static class UpdateSessionEndpoint
{
    internal static RouteHandlerBuilder MapUpdateSessionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/sessions/{sessionId:guid}",
                async (Guid sessionId, UpdateSessionBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateSessionCommand(
                            sessionId,
                            body.LessonId,
                            body.TeacherId,
                            body.RoomId,
                            body.StartUtc,
                            body.EndUtc,
                            body.Topic,
                            body.MeetingUrl,
                            body.TeacherComment,
                            body.Force),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateSession")
            .WithSummary("Update a session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Sessions.Update);

    public sealed record UpdateSessionBody(
        Guid? LessonId,
        Guid TeacherId,
        Guid? RoomId,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        string? Topic,
        string? MeetingUrl,
        string? TeacherComment,
        bool Force);
}
