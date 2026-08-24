using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Rooms;

/// <summary>Small reference list (a handful to a few dozen rooms per school) — not paginated, same
/// convention as Curriculum's <c>GetSubjectTreeQuery</c>.</summary>
public sealed record GetRoomsQuery : IQuery<IReadOnlyList<RoomDto>>;
