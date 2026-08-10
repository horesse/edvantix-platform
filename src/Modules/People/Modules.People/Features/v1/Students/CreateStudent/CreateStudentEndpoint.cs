using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.CreateStudent;

public static class CreateStudentEndpoint
{
    internal static RouteHandlerBuilder MapCreateStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students",
                async (CreateStudentCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateStudent")
            .WithSummary("Create a student")
            .RequirePermission(PeoplePermissions.Students.Create)
            .WithIdempotency();
    }
}
