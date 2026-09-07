namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>The current user's personal iCal feed subscription. <paramref name="Path"/> is a
/// tenant-qualified, relative URL (<c>/api/v1/my/schedule.ics?tenant=…&amp;token=…</c>) — the client
/// prefixes it with the API origin to get a link a calendar app can subscribe to.</summary>
public sealed record IcalSubscriptionDto(string Token, string Path);
