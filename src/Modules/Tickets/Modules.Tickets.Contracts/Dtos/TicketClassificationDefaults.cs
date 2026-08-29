namespace FSH.Modules.Tickets.Contracts.Dtos;

/// <summary>
/// Maps a <see cref="TicketCategory"/> to the <see cref="TicketAudience"/> that handles it by
/// default. Only <see cref="TicketCategory.Technical"/> is a platform concern; everything else is
/// the school's. The caller may still override the audience explicitly on create.
/// </summary>
public static class TicketClassificationDefaults
{
    public static TicketAudience AudienceFor(TicketCategory category) => category switch
    {
        TicketCategory.Technical => TicketAudience.Platform,
        _ => TicketAudience.School,
    };
}
