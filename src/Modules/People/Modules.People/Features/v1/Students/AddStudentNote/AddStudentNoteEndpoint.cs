using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.AddStudentNote;

public static class AddStudentNoteEndpoint
{
    internal static RouteHandlerBuilder MapAddStudentNoteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/notes",
                async (Guid studentId, AddStudentNoteRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    var id = await mediator.Send(new AddStudentNoteCommand(studentId, request.Text), ct);
                    return Results.Ok(id);
                })
            .WithName("AddStudentNote")
            .WithSummary("Add an internal note to a student")
            .RequirePermission(PeoplePermissions.Students.ViewNotes);
    }
}

public sealed record AddStudentNoteRequest(string Text);
