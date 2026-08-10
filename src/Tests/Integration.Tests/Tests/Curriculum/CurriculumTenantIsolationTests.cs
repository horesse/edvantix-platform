using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Curriculum.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Curriculum;

/// <summary>
/// Cross-TENANT isolation for the Curriculum module. <c>Subject</c> and <c>CourseModule</c> have
/// no direct GET-by-id route, so their isolation is proven transitively: through
/// <c>UpdateSubject</c> (404 on a foreign subject id) and through <c>GetLessonById</c> (a lesson
/// under another tenant's module is unreachable). <c>LessonMaterial</c> has no ownership check of
/// its own either — its isolation comes from the same shadow-TenantId row filter as every other
/// entity in <see cref="FSH.Modules.Curriculum.Data.CurriculumDbContext"/>, so a foreign
/// <c>LessonId</c> yields an empty list, not a 404. Intra-tenant CRUD is covered by unit tests
/// (Curriculum.Tests).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class CurriculumTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public CurriculumTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetCourseById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"curr-crs-get-{uniqueId}");

        var subjectId = await CreateSubjectAsync(rootClient);
        var courseId = await CreateCourseAsync(rootClient, subjectId);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.CurriculumBasePath}/courses/{courseId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.CurriculumBasePath}/courses/{courseId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchCourses_Should_NotReturn_OtherTenants_Courses()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"curr-crs-search-{uniqueId}");

        var subjectId = await CreateSubjectAsync(rootClient);
        var title = $"RootOnly-{uniqueId}";
        var courseId = await CreateCourseAsync(rootClient, subjectId, title: title);

        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.CurriculumBasePath}/courses?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<CourseDto>>();

        page.Items.ShouldNotContain(c => c.Id == courseId,
            "tenant B's course list must not include tenant A's course");
    }

    [Fact]
    public async Task UpdateSubject_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Subject has no GET-by-id route (only the /subjects/tree collection) — Update is the
        // narrowest way to prove a foreign subject id is unreachable.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"curr-subj-{uniqueId}");

        var subjectId = await CreateSubjectAsync(rootClient);

        using var crossUpdate = await otherClient.PutAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/subjects/{subjectId}",
            new { name = "Hijacked", parentId = (Guid?)null });
        crossUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLessonById_Should_Return404_When_ModuleOwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"curr-lsn-{uniqueId}");

        var subjectId = await CreateSubjectAsync(rootClient);
        var courseId = await CreateCourseAsync(rootClient, subjectId);
        var moduleId = await CreateCourseModuleAsync(rootClient, courseId);
        var lessonId = await CreateLessonAsync(rootClient, moduleId);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.CurriculumBasePath}/lessons/{lessonId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.CurriculumBasePath}/lessons/{lessonId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLessonMaterials_Should_ReturnEmpty_When_LessonOwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"curr-mat-{uniqueId}");

        var subjectId = await CreateSubjectAsync(rootClient);
        var courseId = await CreateCourseAsync(rootClient, subjectId);
        var moduleId = await CreateCourseModuleAsync(rootClient, courseId);
        var lessonId = await CreateLessonAsync(rootClient, moduleId);

        using var addResp = await rootClient.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/lessons/{lessonId}/materials",
            new { kind = "Link", title = "Видео", fileId = (Guid?)null, url = "https://example.com/v", visibleToStudents = true });
        addResp.StatusCode.ShouldBe(HttpStatusCode.OK, await addResp.Content.ReadAsStringAsync());

        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.CurriculumBasePath}/lessons/{lessonId}/materials");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var crossMaterials = await crossGet.DeserializeAsync<List<LessonMaterialDto>>();
        crossMaterials.ShouldBeEmpty("tenant B must not see tenant A's lesson materials");

        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.CurriculumBasePath}/lessons/{lessonId}/materials");
        var ownMaterials = await ownGet.DeserializeAsync<List<LessonMaterialDto>>();
        ownMaterials.ShouldNotBeEmpty();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateSubjectAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/subjects",
            new { name = $"Subject-{uniqueId}", parentId = (Guid?)null });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create subject: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateCourseAsync(HttpClient client, Guid subjectId, string? title = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/courses",
            new
            {
                subjectId,
                title = title ?? $"Course-{uniqueId}",
                description = (string?)null,
                level = "Beginner",
                durationHours = 10,
                coverFileId = (Guid?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create course: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateCourseModuleAsync(HttpClient client, Guid courseId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/courses/{courseId}/modules",
            new { title = $"Module-{uniqueId}", description = (string?)null });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create course module: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateLessonAsync(HttpClient client, Guid moduleId)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/modules/{moduleId}/lessons",
            new { title = $"Lesson-{uniqueId}", objectives = (string?)null, content = (string?)null, durationMinutes = 45 });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create lesson: {await response.Content.ReadAsStringAsync()}");
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
