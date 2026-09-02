using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.GetGuardianStudents;

public static class GetGuardianStudentsEndpoint
{
    internal static RouteHandlerBuilder MapGetGuardianStudentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/guardians/{guardianId:guid}/students",
                (Guid guardianId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetGuardianStudentsQuery(guardianId), ct))
            .WithName("GetGuardianStudents")
            .WithSummary("List the students a guardian is responsible for")
            .RequirePermission(PeoplePermissions.Students.View);
    }
}
