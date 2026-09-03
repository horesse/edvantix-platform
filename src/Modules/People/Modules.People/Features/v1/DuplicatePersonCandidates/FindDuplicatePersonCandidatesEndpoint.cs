using FSH.Modules.People.Contracts.v1;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.DuplicatePersonCandidates;

public static class FindDuplicatePersonCandidatesEndpoint
{
    internal static RouteHandlerBuilder MapFindDuplicatePersonCandidatesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // Cross-entity read (students + teachers + guardians), so it sits under the non-resource
        // /people/ prefix alongside /people/me/scope rather than under any one collection.
        // No RequirePermission: it only echoes name/contacts the caller already typed into a
        // create dialog, all three People "View" rights are IsBasic, and it never blocks anything.
        return endpoints.MapGet("/people/duplicate-candidates",
                (string lastName, string firstName, string? phone, string? email,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new FindDuplicatePersonCandidatesQuery(lastName, firstName, phone, email), ct))
            .WithName("FindDuplicatePersonCandidates")
            .WithSummary("Soft duplicate check for the create-person dialogs (advisory, never blocks creation)");
    }
}
