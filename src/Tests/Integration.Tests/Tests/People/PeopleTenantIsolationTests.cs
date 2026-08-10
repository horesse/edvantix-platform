using System.Net;
using System.Net.Http.Json;
using FSH.Modules.People.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.People;

/// <summary>
/// Cross-TENANT isolation for the People module, one case per entity: Student, Teacher,
/// Guardian directly, and StudentGuardian/StudentNote transitively (they only exist as
/// children of a Student, so a cross-tenant 404 on the parent already proves the children
/// are unreachable too — there's no way to address them independently of their student).
/// PeopleDbContext gets tenant isolation via BaseDbContext's auto-apply (see
/// docs/04 Задачи/Задачи · Новые модули.md — People, задача 2), these assert the intended
/// behavior end-to-end. Intra-tenant CRUD is covered by the feature-level tests.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PeopleTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public PeopleTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetStudentById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-stu-get-{uniqueId}");

        var studentId = await CreateStudentAsync(rootClient);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.PeopleBasePath}/students/{studentId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.PeopleBasePath}/students/{studentId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchStudents_Should_NotReturn_OtherTenants_Students()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-stu-search-{uniqueId}");

        var lastName = $"RootOnly-{uniqueId}";
        var studentId = await CreateStudentAsync(rootClient, lastName: lastName);

        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.PeopleBasePath}/students?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<StudentDto>>();

        page.Items.ShouldNotContain(s => s.Id == studentId,
            "tenant B's student list must not include tenant A's student");
    }

    [Fact]
    public async Task GetTeacherById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-tea-get-{uniqueId}");

        var teacherId = await CreateTeacherAsync(rootClient);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.PeopleBasePath}/teachers/{teacherId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.PeopleBasePath}/teachers/{teacherId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGuardianById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-gua-get-{uniqueId}");

        var guardianId = await CreateGuardianAsync(rootClient);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.PeopleBasePath}/guardians/{guardianId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.PeopleBasePath}/guardians/{guardianId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudentGuardians_Should_Return404_When_StudentOwnedByDifferentTenant()
    {
        // StudentGuardian has no route of its own — it is only reachable through its student,
        // so the parent's 404 is the whole isolation proof for the child.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-sg-{uniqueId}");

        var studentId = await CreateStudentAsync(rootClient);
        var guardianId = await CreateGuardianAsync(rootClient);
        using var linkResp = await rootClient.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/guardians",
            new { guardianId, relation = "Parent", isPrimaryPayer = true });
        linkResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/guardians");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/guardians");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var links = await ownGet.DeserializeAsync<List<StudentGuardianDto>>();
        links.ShouldContain(l => l.GuardianId == guardianId);
    }

    [Fact]
    public async Task GetStudentNotes_Should_Return404_When_StudentOwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"people-note-{uniqueId}");

        var studentId = await CreateStudentAsync(rootClient);
        using var noteResp = await rootClient.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/notes",
            new { text = "Internal note, root tenant only." });
        noteResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/notes");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/notes");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notes = await ownGet.DeserializeAsync<List<StudentNoteDto>>();
        notes.ShouldNotBeEmpty();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateStudentAsync(HttpClient client, string? lastName = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students",
            new
            {
                lastName = lastName ?? $"Student-{uniqueId}",
                firstName = "Test",
                middleName = (string?)null,
                birthDate = new DateOnly(2010, 1, 1),
                phone = "+10000000000",
                email = $"student-{uniqueId}@example.com",
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateTeacherAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/teachers",
            new
            {
                lastName = $"Teacher-{uniqueId}",
                firstName = "Test",
                middleName = (string?)null,
                phone = "+10000000001",
                email = $"teacher-{uniqueId}@example.com",
                bio = (string?)null,
                specializations = (string[]?)null,
                hourlyRate = (decimal?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create teacher: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateGuardianAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/guardians",
            new
            {
                lastName = $"Guardian-{uniqueId}",
                firstName = "Test",
                phone = "+10000000002",
                email = $"guardian-{uniqueId}@example.com",
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create guardian: {await response.Content.ReadAsStringAsync()}");
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
