using FSH.Framework.Shared.Persistence;
using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

public sealed record SearchSessionsQuery(
    Guid? StudyGroupId = null,
    Guid? TeacherId = null,
    Guid? RoomId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    SessionStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<SessionDto>>;
