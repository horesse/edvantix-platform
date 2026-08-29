using FSH.Framework.Core.Exceptions;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using FSH.Modules.Tickets.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Features.v1.Tickets.UpdateTicket;

public sealed class UpdateTicketCommandHandler(TicketsDbContext dbContext)
    : ICommandHandler<UpdateTicketCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateTicketCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket {command.TicketId} not found.");

        ticket.UpdateDetails(command.Title, command.Description, command.Priority);
        ticket.SetClassification(
            command.Category,
            command.Audience ?? TicketClassificationDefaults.AudienceFor(command.Category));
        ticket.SetRelatedContext(command.RelatedStudentId, command.RelatedStudyGroupId, command.RelatedInvoiceId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.Id;
    }
}
