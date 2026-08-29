using FSH.Framework.Shared.Persistence;
using FSH.Modules.Tickets.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record SearchTicketsQuery : IQuery<PagedResponse<TicketDto>>
{
    public string? Search { get; init; }
    public TicketStatus? Status { get; init; }
    public TicketPriority? Priority { get; init; }
    public TicketCategory? Category { get; init; }
    public TicketAudience? Audience { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public Guid? ReporterUserId { get; init; }
    public Guid? RelatedStudentId { get; init; }
    public Guid? RelatedStudyGroupId { get; init; }
    public Guid? RelatedInvoiceId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDir { get; init; }
}
