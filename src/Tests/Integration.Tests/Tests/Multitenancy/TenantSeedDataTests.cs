using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Multitenancy;

[Collection(FshCollectionDefinition.Name)]
public sealed class TenantSeedDataTests
{
    private readonly AuthHelper _auth;

    public TenantSeedDataTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task NewTenant_Should_BeSeededWith_AdminUser_And_Permissions()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"seed-{uniqueId}";
        var adminEmail = $"seed-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantAdminClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        var profileResponse = await tenantAdminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/profile");
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profileBody = await profileResponse.Content.ReadAsStringAsync();
        profileBody.ShouldContain(adminEmail);

        var permissionsResponse = await tenantAdminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/permissions");
        permissionsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var permissionsBody = await permissionsResponse.Content.ReadAsStringAsync();
        // A newly-seeded tenant admin must have at least one permission attached via a seeded role.
        permissionsBody.ShouldContain("Permissions.");
    }

    /// <summary>
    /// Curriculum/Payments provisioning defaults — see docs/04 Задачи/Задачи · Доработки
    /// каркаса.md → Multitenancy → "Шаги провижининга под новые модули". Both
    /// <c>CurriculumDbInitializer</c> and <c>PaymentsDbInitializer</c> run as part of the same
    /// <c>SeedTenantAsync</c> step exercised above — asserted here rather than in a parallel test
    /// so both new-tenant defaults are covered by one real Postgres-backed provisioning run.
    /// </summary>
    [Fact]
    public async Task NewTenant_Should_BeSeededWith_DefaultSubjects_And_DefaultTariff()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"seed-cp-{uniqueId}";
        var adminEmail = $"seed-cp-admin-{uniqueId}@tenant.com";

        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantAdminClient = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        using var subjectsResponse = await tenantAdminClient.GetAsync(
            $"{TestConstants.CurriculumBasePath}/subjects/tree");
        subjectsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var subjects = await subjectsResponse.DeserializeAsync<List<SubjectNodeDto>>();
        // Two top-level default directions — "Английский язык" / "Математика", see
        // CurriculumDbInitializer.SeedAsync. Not "ровно один": the task brief for this
        // initializer explicitly allows one-or-two default subjects and names both.
        subjects.Count.ShouldBe(2);
        subjects.ShouldContain(s => s.Name == "Английский язык");
        subjects.ShouldContain(s => s.Name == "Математика");

        using var tariffsResponse = await tenantAdminClient.GetAsync($"{TestConstants.PaymentsBasePath}/tariffs");
        tariffsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tariffs = await tariffsResponse.DeserializeAsync<List<TariffDto>>();
        tariffs.Count.ShouldBe(1);
        tariffs[0].Kind.ShouldBe(TariffKind.OneTime);
        tariffs[0].CourseId.ShouldBeNull();
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
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

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds.");
    }
}
