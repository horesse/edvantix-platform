using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.CreateSubject;

public static class CreateSubjectEndpoint
{
    internal static RouteHandlerBuilder MapCreateSubjectEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/subjects",
                async (CreateSubjectCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateSubject")
            .WithSummary("Create a curriculum subject")
            .RequirePermission(CurriculumPermissions.Subjects.Create)
            .WithIdempotency();
}
