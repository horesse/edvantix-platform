using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

public sealed record GetCalendarQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? StudyGroupId,
    Guid? TeacherId,
    Guid? RoomId) : IQuery<IReadOnlyList<CalendarEntryDto>>;
