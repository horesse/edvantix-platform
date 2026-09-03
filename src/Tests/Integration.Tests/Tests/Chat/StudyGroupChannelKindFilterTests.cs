using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Data;
using FSH.Modules.StudyGroups.Contracts.Events;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

/// <summary>
/// EDX-010 — a channel provisioned for a study group carries <c>SourceStudyGroupId</c>, is exposed
/// as such on <see cref="ChannelDto"/>, and <c>GET /chat/channels?kind=…</c> filters on it. Also
/// proves the marker/filter stays tenant-scoped: tenant B never sees tenant A's group channel.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class StudyGroupChannelKindFilterTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public StudyGroupChannelKindFilterTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Group_Channel_Is_Marked_And_Filterable_By_Kind()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminUserId = await GetCurrentUserIdAsync(client);

        // An ad-hoc channel the admin owns — the "standalone" control.
        var standaloneId = await CreateChannelAsync(client, $"chat-kind-adhoc-{Unique()}");

        // A study group is created → Chat provisions its private channel.
        var groupId = Guid.NewGuid();
        await PublishAsync(TestConstants.RootTenantId, new StudyGroupCreatedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, $"Kind Filter Group {Unique()}", Guid.NewGuid(), Guid.NewGuid()));

        // The teacher has no account here, so the channel is seeded empty — add the admin so it
        // surfaces in *their* channel list.
        var groupChannelId = await AddAdminToGroupChannelAsync(TestConstants.RootTenantId, groupId, adminUserId);

        // DTO carries the marker.
        using var get = await client.GetAsync($"{ChatBasePath}/channels/{groupChannelId}");
        var groupChannel = await get.DeserializeAsync<ChannelDto>();
        groupChannel.SourceStudyGroupId.ShouldBe(groupId);

        // kind=study-group → only the group channel.
        var groupOnly = await ListAsync(client, "study-group");
        groupOnly.ShouldContain(c => c.Id == groupChannelId);
        groupOnly.ShouldNotContain(c => c.Id == standaloneId);
        groupOnly.ShouldAllBe(c => c.SourceStudyGroupId != null);

        // kind=standalone → the ad-hoc channel, never the group one.
        var standaloneOnly = await ListAsync(client, "standalone");
        standaloneOnly.ShouldContain(c => c.Id == standaloneId);
        standaloneOnly.ShouldNotContain(c => c.Id == groupChannelId);
        standaloneOnly.ShouldAllBe(c => c.SourceStudyGroupId == null);

        // no filter → both.
        var all = await ListAsync(client, kind: null);
        all.ShouldContain(c => c.Id == groupChannelId);
        all.ShouldContain(c => c.Id == standaloneId);
    }

    [Fact]
    public async Task List_With_Unknown_Kind_Returns_400()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        using var response = await client.GetAsync($"{ChatBasePath}/channels?kind=bogus");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Group_Channel_Marker_Is_Tenant_Scoped()
    {
        // Tenant A (root): provision a group channel.
        var groupId = Guid.NewGuid();
        await PublishAsync(TestConstants.RootTenantId, new StudyGroupCreatedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TestConstants.RootTenantId, "corr", "StudyGroups",
            groupId, $"Tenant A Group {Unique()}", Guid.NewGuid(), Guid.NewGuid()));

        Guid channelAId = Guid.Empty;
        await InTenantScope(TestConstants.RootTenantId, async sp =>
        {
            var db = sp.GetRequiredService<ChatDbContext>();
            channelAId = (await db.Channels.AsNoTracking()
                .SingleAsync(c => c.SourceStudyGroupId == groupId)).Id;
        });

        // Tenant B: a fully provisioned separate tenant must not see it — not in the filtered list,
        // and not by id (existence must not leak even though the DTO now advertises the marker).
        using var tenantBClient = await CreateProvisionedTenantAdminClientAsync();

        var groupChannels = await ListAsync(tenantBClient, "study-group");
        groupChannels.ShouldNotContain(c => c.Id == channelAId);

        using var get = await tenantBClient.GetAsync($"{ChatBasePath}/channels/{channelAId}");
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<IReadOnlyList<ChannelDto>> ListAsync(HttpClient client, string? kind)
    {
        var url = kind is null ? $"{ChatBasePath}/channels" : $"{ChatBasePath}/channels?kind={kind}";
        using var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<IReadOnlyList<ChannelDto>>();
    }

    private static async Task<Guid> CreateChannelAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name,
            description = (string?)null,
            isPrivate = false,
        });
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<string> GetCurrentUserIdAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var user = await response.DeserializeAsync<UserDto>();
        return user.Id;
    }

    private async Task<Guid> AddAdminToGroupChannelAsync(string tenantId, Guid groupId, string adminUserId)
    {
        Guid channelId = Guid.Empty;
        await InTenantScope(tenantId, async sp =>
        {
            var db = sp.GetRequiredService<ChatDbContext>();
            var channel = await db.Channels.SingleAsync(c => c.SourceStudyGroupId == groupId);
            channel.AddMember(adminUserId, adminUserId);
            await db.SaveChangesAsync();
            channelId = channel.Id;
        });
        return channelId;
    }

    private async Task PublishAsync(string tenantId, IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var tenant = await sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        await sp.GetRequiredService<IEventBus>().PublishAsync(@event);
    }

    private async Task InTenantScope(string tenantId, Func<IServiceProvider, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(tenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        await action(scope.ServiceProvider);
    }

    // Stands up a brand-new tenant (via the root admin), waits for provisioning, returns an
    // authenticated admin client scoped to it — same flow as ChatTenantIsolationTests.
    private async Task<HttpClient> CreateProvisionedTenantAdminClientAsync()
    {
        var tenantId = $"chatkind-{Unique()}";
        var adminEmail = $"chatkind-admin-{Unique()}@tenant.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        for (int i = 0; i < 30; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
            }
            catch (HttpRequestException) when (i < 29)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Tenant {tenantId} provisioning did not complete within {maxRetries}s.");
    }
}
