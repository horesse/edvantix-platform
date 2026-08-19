using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Identity.Data;
using FSH.Modules.People.Data;
using FSH.Modules.StudyGroups.Data;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Eventing;

/// <summary>
/// Regression coverage for the keyed-DI fix in <c>AddEventingForDbContext</c>
/// (src/BuildingBlocks/Eventing/ServiceCollectionExtensions.cs) — see
/// docs/04 Задачи/Задачи · Доработки каркаса.md → "Eventing (BuildingBlocks)".
///
/// Before the fix, <c>IOutboxStore</c>/<c>IInboxStore</c>/<c>OutboxDispatcher</c> were registered
/// unkeyed, so with 4+ modules calling <c>AddEventingForDbContext&lt;TDbContext&gt;()</c>, .NET DI
/// resolved every plain <c>IOutboxStore</c> injection to the LAST-registered module's store —
/// meaning Identity's <see cref="FSH.Modules.Identity.Contracts.Events.UserRegisteredIntegrationEvent"/>
/// was actually written (in a separate, non-atomic transaction) to whichever module loaded last
/// (StudyGroups, order 610), not to <c>identity.OutboxMessages</c>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class OutboxKeyedDispatchTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public OutboxKeyedDispatchTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RegisterUser_Should_WriteOutboxRow_ToIdentitySchema_NotToLastLoadedModule()
    {
        // Arrange — a fresh school tenant (provisioning seeds SchoolAdmin with Users.Create).
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"outbox-keyed-{unique}";
        var adminEmail = $"admin-{unique}@outbox-keyed.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var createResponse = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Outbox Keyed {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {createBody}");

        await WaitForProvisioningAsync(rootClient, tenantId);
        var tenant = await GetTenantAsync(tenantId);

        // Provisioning flips to "Completed" a beat before the seeded admin row is visible to a
        // freshly-opened login connection — same race TenantSettingsTests/SchoolRolesSeedingTests
        // work around. Retry the first login instead of asserting on a flaky 401.
        using var adminClient = await GetAuthenticatedClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);

        // Act — register a second user. UserRegistrationService.PublishUserRegisteredAsync writes
        // UserRegisteredIntegrationEvent via [FromKeyedServices(typeof(IdentityDbContext))] IOutboxStore.
        var registerUnique = Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "Outbox",
            lastName = "Regression",
            email = $"outbox-{registerUnique}@outbox-keyed.com",
            userName = $"outbox-{registerUnique}",
            password = TestConstants.DefaultPassword,
            confirmPassword = TestConstants.DefaultPassword,
        });
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created, $"Register user failed: {registerBody}");

        // Assert — the row lives in identity.OutboxMessages, and NOT in any other module's schema
        // that also calls AddEventingForDbContext (People/Curriculum/StudyGroups).
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var identityRows = await identityDb.Set<OutboxMessage>()
            .Where(m => m.TenantId == tenantId && m.Type.Contains("UserRegisteredIntegrationEvent"))
            .ToListAsync();
        identityRows.ShouldNotBeEmpty("UserRegisteredIntegrationEvent must land in identity.OutboxMessages.");

        var peopleDb = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        var peopleRows = await peopleDb.Set<OutboxMessage>()
            .Where(m => m.TenantId == tenantId && m.Type.Contains("UserRegisteredIntegrationEvent"))
            .ToListAsync();
        peopleRows.ShouldBeEmpty("UserRegisteredIntegrationEvent must NOT be misfiled into people.OutboxMessages.");

        var curriculumDb = scope.ServiceProvider.GetRequiredService<CurriculumDbContext>();
        var curriculumRows = await curriculumDb.Set<OutboxMessage>()
            .Where(m => m.TenantId == tenantId && m.Type.Contains("UserRegisteredIntegrationEvent"))
            .ToListAsync();
        curriculumRows.ShouldBeEmpty("UserRegisteredIntegrationEvent must NOT be misfiled into curriculum.OutboxMessages.");

        var studyGroupsDb = scope.ServiceProvider.GetRequiredService<StudyGroupsDbContext>();
        var studyGroupsRows = await studyGroupsDb.Set<OutboxMessage>()
            .Where(m => m.TenantId == tenantId && m.Type.Contains("UserRegisteredIntegrationEvent"))
            .ToListAsync();
        studyGroupsRows.ShouldBeEmpty(
            "UserRegisteredIntegrationEvent must NOT be misfiled into study_groups.OutboxMessages " +
            "(the pre-fix bug: StudyGroups, order 610, loads last and used to win the unkeyed DI registration).");
    }

    private async Task<HttpClient> GetAuthenticatedClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private async Task<AppTenantInfo> GetTenantAsync(string tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(tenantId);
        tenant.ShouldNotBeNull($"Tenant '{tenantId}' must exist for this test.");
        return tenant;
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
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

        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }
}
