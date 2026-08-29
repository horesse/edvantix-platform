using System.Text.Json.Serialization;

namespace FSH.Modules.Tickets.Contracts.Dtos;

/// <summary>
/// Who handles the ticket. The two ticket flows from docs/02 Модули/Tickets.md that the module
/// previously did not distinguish: a school user asking Edvantix support (<see cref="Platform"/>)
/// vs. a student/guardian asking the school administration (<see cref="School"/>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketAudience>))]
public enum TicketAudience
{
    /// <summary>Handled by the school's administration. The default for school-domain categories.</summary>
    School = 0,

    /// <summary>Handled by Edvantix platform support.</summary>
    Platform = 1,
}
