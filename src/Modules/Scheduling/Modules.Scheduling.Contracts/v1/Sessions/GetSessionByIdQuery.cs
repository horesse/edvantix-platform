using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

public sealed record GetSessionByIdQuery(Guid SessionId) : IQuery<SessionDetailDto>;
