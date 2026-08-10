using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.UpdateTeacher;

public static class UpdateTeacherEndpoint
{
    internal static RouteHandlerBuilder MapUpdateTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/teachers/{teacherId:guid}",
                async (Guid teacherId, UpdateTeacherCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { TeacherId = teacherId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateTeacher")
            .WithSummary("Update a teacher")
            .RequirePermission(PeoplePermissions.Teachers.Update);
    }
}
