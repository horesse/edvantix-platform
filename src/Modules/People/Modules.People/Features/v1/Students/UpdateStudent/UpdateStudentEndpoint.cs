using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.UpdateStudent;

public static class UpdateStudentEndpoint
{
    internal static RouteHandlerBuilder MapUpdateStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/students/{studentId:guid}",
                async (Guid studentId, UpdateStudentCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { StudentId = studentId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateStudent")
            .WithSummary("Update a student")
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
