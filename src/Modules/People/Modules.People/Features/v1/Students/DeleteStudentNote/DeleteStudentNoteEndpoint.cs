using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.DeleteStudentNote;

public static class DeleteStudentNoteEndpoint
{
    internal static RouteHandlerBuilder MapDeleteStudentNoteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/students/{studentId:guid}/notes/{noteId:guid}",
                async (Guid studentId, Guid noteId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteStudentNoteCommand(studentId, noteId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteStudentNote")
            .WithSummary("Delete an internal note from a student")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Students.ViewNotes);
    }
}
