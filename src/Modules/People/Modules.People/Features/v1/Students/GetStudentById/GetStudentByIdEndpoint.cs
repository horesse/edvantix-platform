using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.GetStudentById;

public static class GetStudentByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/students/{studentId:guid}",
                (Guid studentId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetStudentByIdQuery(studentId), ct))
            .WithName("GetStudentById")
            .WithSummary("Get a student by id")
            .RequirePermission(PeoplePermissions.Students.View);
    }
}
