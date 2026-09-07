namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription;

/// <summary>Builds the tenant-qualified relative feed URL handed back to the dashboard. Kept
/// relative on purpose — the client prefixes the API origin it already knows, so the backend
/// doesn't have to guess its own public host.</summary>
internal static class IcalSubscriptionPath
{
    public static string Build(string? tenant, string token) =>
        $"/api/v1/my/schedule.ics?tenant={Uri.EscapeDataString(tenant ?? string.Empty)}&token={Uri.EscapeDataString(token)}";
}
