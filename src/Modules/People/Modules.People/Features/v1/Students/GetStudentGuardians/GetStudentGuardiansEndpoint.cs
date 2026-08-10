using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.GetStudentGuardians;

public static class GetStudentGuardiansEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentGuardiansEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/students/{studentId:guid}/guardians",
                (Guid studentId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetStudentGuardiansQuery(studentId), ct))
            .WithName("GetStudentGuardians")
            .WithSummary("List a student's guardians")
            .RequirePermission(PeoplePermissions.Students.View);
    }
}
