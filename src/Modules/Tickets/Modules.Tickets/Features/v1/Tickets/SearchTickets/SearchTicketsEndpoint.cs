using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.SearchTickets;

public static class SearchTicketsEndpoint
{
    internal static RouteHandlerBuilder MapSearchTicketsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/tickets",
                async (
                    string? search,
                    TicketStatus? status,
                    TicketPriority? priority,
                    TicketCategory? category,
                    TicketAudience? audience,
                    Guid? assignedToUserId,
                    Guid? reporterUserId,
                    Guid? relatedStudentId,
                    Guid? relatedStudyGroupId,
                    Guid? relatedInvoiceId,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new SearchTicketsQuery
                    {
                        Search = search,
                        Status = status,
                        Priority = priority,
                        Category = category,
                        Audience = audience,
                        AssignedToUserId = assignedToUserId,
                        ReporterUserId = reporterUserId,
                        RelatedStudentId = relatedStudentId,
                        RelatedStudyGroupId = relatedStudyGroupId,
                        RelatedInvoiceId = relatedInvoiceId,
                        PageNumber = pageNumber ?? 1,
                        PageSize = pageSize ?? 20,
                        SortBy = sortBy,
                        SortDir = sortDir,
                    };
                    return Results.Ok(await mediator.Send(query, ct));
                })
            .WithName("SearchTickets")
            .WithSummary("Search tickets")
            .RequirePermission(TicketsPermissions.Tickets.View);
    }
}
