using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.GetStudentNotes;

public static class GetStudentNotesEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentNotesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/students/{studentId:guid}/notes",
                (Guid studentId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetStudentNotesQuery(studentId), ct))
            .WithName("GetStudentNotes")
            .WithSummary("List a student's internal notes")
            .RequirePermission(PeoplePermissions.Students.ViewNotes);
    }
}
