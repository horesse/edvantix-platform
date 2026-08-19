using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.MarkAttendance;

public static class MarkAttendanceEndpoint
{
    internal static RouteHandlerBuilder MapMarkAttendanceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/sessions/{sessionId:guid}/attendance",
                async (Guid sessionId, IReadOnlyList<AttendanceMarkDto> marks, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new MarkAttendanceCommand(sessionId, marks), ct);
                    return Results.NoContent();
                })
            .WithName("MarkAttendance")
            .WithSummary("Bulk-mark attendance for a session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            // Attendance.MarkAny (amending after a billing period has closed) is not enforced yet —
            // Payments doesn't exist in this codebase to check "was this session invoiced" against.
            // See docs/02 Модули/Scheduling.md → "Права".
            .RequirePermission(SchedulingPermissions.Attendance.Mark);
}
