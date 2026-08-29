using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Tickets.Features.v1.Tickets.UpdateTicket;

public static class UpdateTicketEndpoint
{
    public sealed record UpdateTicketRequest(
        string Title,
        string? Description,
        TicketPriority Priority,
        Guid? RelatedStudentId = null,
        Guid? RelatedStudyGroupId = null,
        Guid? RelatedInvoiceId = null);

    internal static RouteHandlerBuilder MapUpdateTicketEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/tickets/{ticketId:guid}",
                async (Guid ticketId, UpdateTicketRequest body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new UpdateTicketCommand(
                            ticketId, body.Title, body.Description, body.Priority,
                            body.RelatedStudentId, body.RelatedStudyGroupId, body.RelatedInvoiceId), ct)))
            .WithName("UpdateTicket")
            .WithSummary("Edit a ticket's title, description, priority, and related-context links")
            .RequirePermission(TicketsPermissions.Tickets.Update)
            .WithIdempotency();
    }
}
