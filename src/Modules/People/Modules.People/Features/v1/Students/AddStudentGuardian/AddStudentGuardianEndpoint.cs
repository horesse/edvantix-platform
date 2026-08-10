using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.AddStudentGuardian;

public static class AddStudentGuardianEndpoint
{
    internal static RouteHandlerBuilder MapAddStudentGuardianEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/guardians",
                async (Guid studentId, AddStudentGuardianRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    var command = new AddStudentGuardianCommand(
                        studentId, request.GuardianId, request.Relation, request.IsPrimaryPayer);
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("AddStudentGuardian")
            .WithSummary("Link a guardian to a student")
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}

public sealed record AddStudentGuardianRequest(Guid GuardianId, string Relation, bool IsPrimaryPayer = false);
