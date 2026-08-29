using FSH.Modules.Tickets.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record CreateTicketCommand(
    string Title,
    string? Description = null,
    TicketPriority Priority = TicketPriority.Medium,
    Guid? AssignedToUserId = null,
    Guid? RelatedStudentId = null,
    Guid? RelatedStudyGroupId = null,
    Guid? RelatedInvoiceId = null) : ICommand<Guid>;
