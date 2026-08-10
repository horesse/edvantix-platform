using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.ImportStudents;

public static class ImportStudentsEndpoint
{
    internal static RouteHandlerBuilder MapImportStudentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/import",
                async (IFormFile file, bool? dryRun, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(file);
                    using var reader = new StreamReader(file.OpenReadStream());
                    string csv = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                    var result = await mediator.Send(new ImportStudentsCommand(csv, dryRun ?? true), ct);
                    return Results.Ok(result);
                })
            .WithName("ImportStudents")
            .WithSummary("Bulk-import students from CSV — dry-run by default (?dryRun=false to commit)")
            .DisableAntiforgery()
            .RequirePermission(PeoplePermissions.Students.Create);
    }
}
