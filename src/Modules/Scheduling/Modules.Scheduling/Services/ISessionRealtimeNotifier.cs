using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Services;

/// <summary>Pushes calendar-affecting session changes to the tenant's dashboard clients over
/// SignalR — "у преподавателя открыт календарь, менеджер переносит занятие — обновление без
/// перезагрузки" (docs/02 Модули/Scheduling.md → "Реальное время"). Centralized here (rather than
/// each handler calling <c>IHubContext&lt;AppHub&gt;</c> directly) so the group name and event name
/// are defined once.</summary>
public interface ISessionRealtimeNotifier
{
    Task NotifySessionChangedAsync(string? tenantId, SessionDto session, CancellationToken cancellationToken = default);
}
