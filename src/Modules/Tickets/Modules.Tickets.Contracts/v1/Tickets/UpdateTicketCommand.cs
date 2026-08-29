using FSH.Modules.Tickets.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record UpdateTicketCommand(
    Guid TicketId,
    string Title,
    string? Description = null,
    TicketPriority Priority = TicketPriority.Medium,
    Guid? RelatedStudentId = null,
    Guid? RelatedStudyGroupId = null,
    Guid? RelatedInvoiceId = null) : ICommand<Guid>;
