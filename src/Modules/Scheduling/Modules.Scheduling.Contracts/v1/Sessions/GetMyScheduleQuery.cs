using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

/// <summary>Resolves "my" schedule via <c>IPeopleScopeResolver</c> — teacher sees own sessions,
/// student/guardian see their (wards') groups' sessions. Gated by <c>Sessions.ViewOwn</c>.</summary>
public sealed record GetMyScheduleQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<IReadOnlyList<SessionDto>>;
