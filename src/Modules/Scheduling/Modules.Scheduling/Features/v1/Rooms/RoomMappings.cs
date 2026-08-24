using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Features.v1.Rooms;

internal static class RoomMappings
{
    public static RoomDto ToDto(this Room r) => new(
        r.Id,
        r.Name,
        r.Capacity,
        r.Location,
        r.IsVirtual,
        r.CreatedAtUtc,
        r.UpdatedAtUtc);
}
