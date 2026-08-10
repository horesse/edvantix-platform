using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.CreateTeacher;

public static class CreateTeacherEndpoint
{
    internal static RouteHandlerBuilder MapCreateTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/teachers",
                async (CreateTeacherCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateTeacher")
            .WithSummary("Create a teacher")
            .RequirePermission(PeoplePermissions.Teachers.Create)
            .WithIdempotency();
    }
}
