using System.Net;
using System.Net.Http.Json;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.StudyGroups;

/// <summary>
/// Cross-TENANT isolation for the StudyGroups module, same shape as
/// <see cref="Integration.Tests.Tests.Curriculum.CurriculumTenantIsolationTests"/>: proven through
/// <c>GetStudyGroupById</c> (404 on a foreign group id) and <c>SearchStudyGroups</c> (no leakage
/// into another tenant's page). Intra-tenant CRUD and lifecycle/roster invariants are covered by
/// unit tests (StudyGroups.Tests).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class StudyGroupsTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public StudyGroupsTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetStudyGroupById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sg-get-{uniqueId}");

        var teacherId = await CreateTeacherAsync(rootClient);
        var courseId = await CreatePublishedCourseAsync(rootClient);
        var groupId = await CreateStudyGroupAsync(rootClient, courseId, teacherId);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.StudyGroupsBasePath}/study-groups/{groupId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.StudyGroupsBasePath}/study-groups/{groupId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchStudyGroups_Should_NotReturn_OtherTenants_Groups()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sg-search-{uniqueId}");

        var teacherId = await CreateTeacherAsync(rootClient);
        var courseId = await CreatePublishedCourseAsync(rootClient);
        var groupId = await CreateStudyGroupAsync(rootClient, courseId, teacherId, code: $"RootOnly-{uniqueId}");

        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<StudyGroupDto>>();

        page.Items.ShouldNotContain(g => g.Id == groupId,
            "tenant B's study group list must not include tenant A's study group");
    }

    [Fact]
    public async Task ChangeEnrollmentTariff_Should_Return404_When_EnrollmentOwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sg-tariff-{uniqueId}");

        var teacherId = await CreateTeacherAsync(rootClient);
        var courseId = await CreatePublishedCourseAsync(rootClient);
        var groupId = await CreateStudyGroupAsync(rootClient, courseId, teacherId);
        var studentId = await CreateStudentAsync(rootClient);
        var enrollmentId = await EnrollStudentAsync(rootClient, groupId, studentId);

        var body = new { tariffId = (Guid?)null, discountPercent = 10m };

        using var crossPut = await otherClient.PutAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/enrollments/{enrollmentId}/tariff", body);
        crossPut.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownPut = await rootClient.PutAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/enrollments/{enrollmentId}/tariff", body);
        ownPut.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ─── helpers ─────────────────────────────────────────────────────

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
                phone = "+10000000003",
                email = $"student-{uniqueId}@example.com",
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> EnrollStudentAsync(HttpClient client, Guid studyGroupId, Guid studentId)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/enrollments",
            new
            {
                studentIds = new[] { studentId },
                enrolledOn = (DateOnly?)null,
                tariffId = (Guid?)null,
                discountPercent = 0m,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to enroll student: {await response.Content.ReadAsStringAsync()}");
        var ids = await response.DeserializeAsync<List<Guid>>();
        return ids.ShouldHaveSingleItem();
    }

    private static async Task<Guid> CreatePublishedCourseAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        using var subjectResponse = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/subjects",
            new { name = $"Subject-{uniqueId}", parentId = (Guid?)null });
        subjectResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create subject: {await subjectResponse.Content.ReadAsStringAsync()}");
        var subjectId = await subjectResponse.DeserializeAsync<Guid>();

        using var courseResponse = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/courses",
            new
            {
                subjectId,
                title = $"Course-{uniqueId}",
                description = (string?)null,
                level = "Beginner",
                durationHours = 10,
                coverFileId = (Guid?)null,
            });
        courseResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create course: {await courseResponse.Content.ReadAsStringAsync()}");
        var courseId = await courseResponse.DeserializeAsync<Guid>();

        using var moduleResponse = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/courses/{courseId}/modules",
            new { title = $"Module-{uniqueId}", description = (string?)null });
        moduleResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create course module: {await moduleResponse.Content.ReadAsStringAsync()}");

        using var publishResponse = await client.PostAsync(
            $"{TestConstants.CurriculumBasePath}/courses/{courseId}/publish", content: null);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to publish course: {await publishResponse.Content.ReadAsStringAsync()}");

        return courseId;
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

    private static async Task<Guid> CreateStudyGroupAsync(
        HttpClient client, Guid courseId, Guid teacherId, string? code = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups",
            new
            {
                code = code ?? $"SG-{uniqueId}",
                name = $"Group-{uniqueId}",
                courseId,
                primaryTeacherId = teacherId,
                format = "Online",
                capacity = 10,
                startDate = DateOnly.FromDateTime(DateTime.UtcNow),
                endDate = (DateOnly?)null,
                meetingUrl = (string?)null,
                roomId = (Guid?)null,
                notes = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create study group: {await response.Content.ReadAsStringAsync()}");
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
