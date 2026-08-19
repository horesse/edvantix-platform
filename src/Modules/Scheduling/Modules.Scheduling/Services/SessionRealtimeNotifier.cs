using FSH.Framework.Web.Realtime;
using FSH.Modules.Scheduling.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace FSH.Modules.Scheduling.Services;

public sealed class SessionRealtimeNotifier(IHubContext<AppHub> hub) : ISessionRealtimeNotifier
{
    public async Task NotifySessionChangedAsync(string? tenantId, SessionDto session, CancellationToken cancellationToken = default)
    {
        // Broadcasts are always scoped to a group — never Clients.All (realtime.md). A null tenant
        // (background/system context) has no dashboard audience to reach.
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        await hub.Clients.Group($"tenant:{tenantId}")
            .SendAsync("SessionScheduleChanged", session, cancellationToken)
            .ConfigureAwait(false);
    }
}
