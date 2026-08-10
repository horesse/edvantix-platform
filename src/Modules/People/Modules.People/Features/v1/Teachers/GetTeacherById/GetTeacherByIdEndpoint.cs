using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.GetTeacherById;

public static class GetTeacherByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetTeacherByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/teachers/{teacherId:guid}",
                (Guid teacherId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetTeacherByIdQuery(teacherId), ct))
            .WithName("GetTeacherById")
            .WithSummary("Get a teacher by id")
            .RequirePermission(PeoplePermissions.Teachers.View);
    }
}
