using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.ChangeEnrollmentTariff;

public static class ChangeEnrollmentTariffEndpoint
{
    internal static RouteHandlerBuilder MapChangeEnrollmentTariffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/enrollments/{enrollmentId:guid}/tariff",
                async (Guid enrollmentId, ChangeEnrollmentTariffCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { EnrollmentId = enrollmentId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("ChangeEnrollmentTariff")
            .WithSummary("Change the tariff/discount of an existing enrollment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            // Separate right — re-pricing a live enrollment touches money the same way
            // Enrollments.Transfer does, so it is not folded into plain Enrollments.Create
            // (see docs/01 Архитектура/Модель прав доступа.md → StudyGroups).
            .RequirePermission(StudyGroupsPermissions.Enrollments.Update);
    }
}
