using System.Net;
using System.Net.Http.Json;
using Integration.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Scheduling;

/// <summary>
/// EDX-012 — the personal iCal feed. The feed endpoint is anonymous (calendar clients can't send a
/// bearer token): it authenticates by a personal token and resolves the tenant from <c>?tenant=</c>.
/// These cases prove the round trip (issue → fetch with no auth header → revoke → 404) and that a
/// bad token is indistinguishable from an empty schedule (both 404).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ScheduleIcsFeedTests
{
    private readonly AuthHelper _auth;
    private readonly FshWebApplicationFactory _factory;

    public ScheduleIcsFeedTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    private sealed record Subscription(string Token, string Path);

    [Fact]
    public async Task Rotate_Then_Fetch_Anonymously_Then_Revoke()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var rotateResponse = await client.PostAsync(
            $"{TestConstants.SchedulingBasePath}/my/schedule/ical-subscription/rotate", content: null);
        rotateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sub = await rotateResponse.Content.ReadFromJsonAsync<Subscription>();
        sub.ShouldNotBeNull();
        sub!.Token.ShouldNotBeNullOrWhiteSpace();
        sub.Path.ShouldStartWith("/api/v1/my/schedule.ics?");
        sub.Path.ShouldContain("tenant=");
        sub.Path.ShouldContain($"token={Uri.EscapeDataString(sub.Token)}");

        // A brand-new, unauthenticated client — no bearer token, no tenant header.
        using var anon = _factory.CreateClient();

        using var feed = await anon.GetAsync(sub.Path);
        feed.StatusCode.ShouldBe(HttpStatusCode.OK);
        feed.Content.Headers.ContentType?.MediaType.ShouldBe("text/calendar");
        var body = await feed.Content.ReadAsStringAsync();
        body.ShouldStartWith("BEGIN:VCALENDAR");
        body.ShouldContain("END:VCALENDAR");

        // Wrong token → 404 (not a distinct "unknown token" status).
        using var badToken = await anon.GetAsync("/api/v1/my/schedule.ics?tenant=root&token=not-a-real-token");
        badToken.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // GET reflects the current subscription.
        using var getResponse = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/my/schedule/ical-subscription");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var current = await getResponse.Content.ReadFromJsonAsync<Subscription>();
        current!.Token.ShouldBe(sub.Token);

        // Revoke → the copied link stops working.
        using var revokeResponse = await client.DeleteAsync(
            $"{TestConstants.SchedulingBasePath}/my/schedule/ical-subscription");
        revokeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var afterRevoke = await anon.GetAsync(sub.Path);
        afterRevoke.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var getAfterRevoke = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/my/schedule/ical-subscription");
        getAfterRevoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Feed_Requires_A_Token()
    {
        using var anon = _factory.CreateClient();

        using var noToken = await anon.GetAsync("/api/v1/my/schedule.ics?tenant=root");
        noToken.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
