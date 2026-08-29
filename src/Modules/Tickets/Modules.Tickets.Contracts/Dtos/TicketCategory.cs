using System.Text.Json.Serialization;

namespace FSH.Modules.Tickets.Contracts.Dtos;

/// <summary>
/// What a ticket is about. Drives the default <see cref="TicketAudience"/> (see
/// <c>TicketClassificationDefaults</c>) and, later, a per-tenant default assignee.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketCategory>))]
public enum TicketCategory
{
    /// <summary>Uncategorised — the default for a ticket created without a category.</summary>
    General = 0,

    /// <summary>«Оплата» — invoices, payments, refunds.</summary>
    Payment = 1,

    /// <summary>«Расписание» — lesson times, cancellations, room clashes.</summary>
    Schedule = 2,

    /// <summary>«Смена группы» — moving a student between study groups.</summary>
    GroupChange = 3,

    /// <summary>«Качество преподавания».</summary>
    TeachingQuality = 4,

    /// <summary>«Техническая проблема» — the platform itself. Routed to Edvantix, not the school.</summary>
    Technical = 5,
}
