using System.Net;
using System.Net.Http.Json;
using FSH.Modules.People.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.People;

/// <summary>
/// End-to-end scenario for <c>IPeopleScopeResolver</c> / <c>GET /people/me/scope</c>: an empty
/// scope resolves and caches for a fresh account, then linking a guardian and adding a ward must
/// both invalidate that cache — otherwise the frontend would keep showing a stale "no scope" for
/// up to the 30-minute HybridCache expiration (see PeopleScopeResolver.cs).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PeopleScopeResolverTests
{
    private readonly AuthHelper _auth;

    public PeopleScopeResolverTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetMyScope_Should_Reflect_LinkedGuardian_And_Wards_After_Invalidation()
    {
        // A freshly provisioned tenant so the admin's account has no pre-existing People links
        // that would make the "empty scope" assertion below flaky.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var tenantClient = await ProvisionTenantClientAsync(rootClient, $"people-scope-{uniqueId}");

        using var profileResp = await tenantClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        profileResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await profileResp.DeserializeAsync<UserDto>();

        using var beforeResp = await tenantClient.GetAsync($"{TestConstants.PeopleBasePath}/people/me/scope");
        beforeResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResp.DeserializeAsync<PeopleScope>();
        before.GuardianId.ShouldBeNull();
        before.StudentId.ShouldBeNull();
        before.TeacherId.ShouldBeNull();
        before.WardStudentIds.ShouldBeEmpty();

        var guardianId = await CreateGuardianAsync(tenantClient);
        using var linkResp = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/guardians/{guardianId}/link-user",
            new { userId = profile.Id });
        linkResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var studentId = await CreateStudentAsync(tenantClient);
        using var addGuardianResp = await tenantClient.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/guardians",
            new { guardianId, relation = "Parent", isPrimaryPayer = true });
        addGuardianResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // The link-user and add-guardian handlers each invalidate the cached scope for this
        // userId — if either forgot to, this would still show the pre-link snapshot.
        using var afterResp = await tenantClient.GetAsync($"{TestConstants.PeopleBasePath}/people/me/scope");
        afterResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var after = await afterResp.DeserializeAsync<PeopleScope>();
        after.GuardianId.ShouldBe(guardianId);
        after.WardStudentIds.ShouldContain(studentId);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateGuardianAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/guardians",
            new
            {
                lastName = $"Guardian-{uniqueId}",
                firstName = "Test",
                phone = "+10000000003",
                email = $"guardian-{uniqueId}@example.com",
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create guardian: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateStudentAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students",
            new
            {
                lastName = $"Student-{uniqueId}",
                firstName = "Test",
                middleName = (string?)null,
                birthDate = new DateOnly(2012, 6, 15),
                phone = "+10000000004",
                email = $"student-{uniqueId}@example.com",
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private async Task<HttpClient> ProvisionTenantClientAsync(HttpClient rootClient, string tenantId)
    {
        var adminEmail = $"{tenantId}-admin@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);
        return await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);
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

        var finalResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }
}
