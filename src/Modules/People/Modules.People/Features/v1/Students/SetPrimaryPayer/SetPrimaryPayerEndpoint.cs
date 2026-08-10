using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.SetPrimaryPayer;

public static class SetPrimaryPayerEndpoint
{
    internal static RouteHandlerBuilder MapSetPrimaryPayerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/guardians/{guardianId:guid}/primary-payer",
                async (Guid studentId, Guid guardianId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetPrimaryPayerCommand(studentId, guardianId), ct);
                    return Results.NoContent();
                })
            .WithName("SetPrimaryPayer")
            .WithSummary("Mark a linked guardian as the primary payer (demotes the previous one)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
