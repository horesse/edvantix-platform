using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.TransferEnrollment;

public static class TransferEnrollmentEndpoint
{
    internal static RouteHandlerBuilder MapTransferEnrollmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/enrollments/{enrollmentId:guid}/transfer",
                async (Guid enrollmentId, TransferEnrollmentCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { EnrollmentId = enrollmentId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("TransferEnrollment")
            .WithSummary("Transfer a student's enrollment to another study group")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            // Separate right — a transfer touches money (tariff/discount carry over to the new
            // group), so it is not covered by plain Enrollments.Create/Delete (see
            // docs/01 Архитектура/Модель прав доступа.md → StudyGroups).
            .RequirePermission(StudyGroupsPermissions.Enrollments.Transfer)
            .WithIdempotency();
    }
}
