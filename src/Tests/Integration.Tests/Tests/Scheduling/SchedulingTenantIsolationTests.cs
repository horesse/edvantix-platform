using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Scheduling.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Scheduling;

/// <summary>
/// Cross-TENANT isolation for the Scheduling module, same shape as
/// <see cref="Integration.Tests.Tests.StudyGroups.StudyGroupsTenantIsolationTests"/> — one case per
/// entity (docs/04 Задачи/Задачи · Новые модули.md → Scheduling, шаг 13). Room/NonWorkingDay have no
/// GetById endpoint, so isolation is proven via list leakage; ScheduleTemplate/Attendance have no
/// GetById either, so isolation is proven transitively (404/empty on an update or a query scoped by
/// a foreign id) — same technique Curriculum used for <c>LessonMaterial</c>. Intra-tenant CRUD and
/// domain invariants (conflicts, generation, DST) are covered by unit tests (Scheduling.Tests).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SchedulingTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public SchedulingTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetRooms_Should_NotReturn_OtherTenants_Rooms()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-room-{uniqueId}");

        var roomName = $"RootOnly-Room-{uniqueId}";
        await CreateRoomAsync(rootClient, roomName);

        using var listResponse = await otherClient.GetAsync($"{TestConstants.SchedulingBasePath}/rooms");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rooms = await listResponse.DeserializeAsync<List<RoomDto>>();

        rooms.ShouldNotContain(r => r.Name == roomName,
            "tenant B's room list must not include tenant A's room");
    }

    [Fact]
    public async Task GetNonWorkingDays_Should_NotReturn_OtherTenants_Days()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-nwd-{uniqueId}");

        // Far-future, unique-ish date so it can't collide with another test's seeded holiday —
        // derived from the test run's own guid, not a security-sensitive random source.
        var offsetDays = (Math.Abs(Guid.NewGuid().GetHashCode()) % 300) + 1;
        var date = new DateOnly(2031, 1, 1).AddDays(offsetDays);
        await CreateNonWorkingDayAsync(rootClient, date, $"RootOnly-{uniqueId}");

        using var listResponse = await otherClient.GetAsync($"{TestConstants.SchedulingBasePath}/non-working-days");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var days = await listResponse.DeserializeAsync<List<NonWorkingDayDto>>();

        days.ShouldNotContain(d => d.Date == date,
            "tenant B's non-working-day list must not include tenant A's holiday");
    }

    [Fact]
    public async Task GetSessionById_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-sess-{uniqueId}");

        var (studyGroupId, teacherId) = await CreateStudyGroupAsync(rootClient);
        var sessionId = await CreateSessionAsync(rootClient, studyGroupId, teacherId);

        using var crossGet = await otherClient.GetAsync($"{TestConstants.SchedulingBasePath}/sessions/{sessionId}");
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var ownGet = await rootClient.GetAsync($"{TestConstants.SchedulingBasePath}/sessions/{sessionId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchSessions_Should_NotReturn_OtherTenants_Sessions()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-search-{uniqueId}");

        var (studyGroupId, teacherId) = await CreateStudyGroupAsync(rootClient);
        var sessionId = await CreateSessionAsync(rootClient, studyGroupId, teacherId);

        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.SchedulingBasePath}/sessions?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<SessionDto>>();

        page.Items.ShouldNotContain(s => s.Id == sessionId,
            "tenant B's session list must not include tenant A's session");
    }

    [Fact]
    public async Task UpdateScheduleTemplate_Should_Return404_When_OwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-tmpl-{uniqueId}");

        var (studyGroupId, _) = await CreateStudyGroupAsync(rootClient);
        var templateId = await CreateScheduleTemplateAsync(rootClient, studyGroupId);

        using var crossUpdate = await otherClient.PutAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/schedule-templates/{templateId}",
            new
            {
                dayOfWeek = "Wednesday",
                startTime = "19:00:00",
                durationMinutes = 45,
                roomId = (Guid?)null,
                teacherId = (Guid?)null,
                validFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                validTo = (DateOnly?)null,
                isActive = true,
            });
        crossUpdate.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "tenant B must not be able to see/update tenant A's schedule template");
    }

    [Fact]
    public async Task GetSessionAttendance_Should_ReturnEmpty_When_SessionOwnedByDifferentTenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"sch-att-{uniqueId}");

        var (studyGroupId, teacherId) = await CreateStudyGroupAsync(rootClient);
        var sessionId = await CreateSessionAsync(rootClient, studyGroupId, teacherId);

        // No GetById on Attendance — isolation is transitive through the tenant-filtered query, same
        // technique Curriculum used for LessonMaterial (empty list, not a 404, since the endpoint
        // doesn't check the parent session's existence before filtering).
        using var crossAttendance = await otherClient.GetAsync(
            $"{TestConstants.SchedulingBasePath}/sessions/{sessionId}/attendance");
        crossAttendance.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attendance = await crossAttendance.DeserializeAsync<List<AttendanceDto>>();
        attendance.ShouldBeEmpty();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task CreateRoomAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/rooms",
            new { name, capacity = 10, location = (string?)null, isVirtual = false });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create room: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task CreateNonWorkingDayAsync(HttpClient client, DateOnly date, string description)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/non-working-days",
            new { date, description });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create non-working day: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task<(Guid StudyGroupId, Guid TeacherId)> CreateStudyGroupAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        using var teacherResponse = await client.PostAsJsonAsync(
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
        teacherResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create teacher: {await teacherResponse.Content.ReadAsStringAsync()}");
        var teacherId = await teacherResponse.DeserializeAsync<Guid>();

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

        using var groupResponse = await client.PostAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups",
            new
            {
                code = $"SG-{uniqueId}",
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
        groupResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create study group: {await groupResponse.Content.ReadAsStringAsync()}");
        var studyGroupId = await groupResponse.DeserializeAsync<Guid>();

        return (studyGroupId, teacherId);
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client, Guid studyGroupId, Guid teacherId)
    {
        var startUtc = DateTimeOffset.UtcNow.AddDays(7);
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/sessions",
            new
            {
                studyGroupId,
                lessonId = (Guid?)null,
                teacherId,
                roomId = (Guid?)null,
                startUtc,
                endUtc = startUtc.AddMinutes(60),
                topic = (string?)null,
                meetingUrl = (string?)null,
                force = false,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create session: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateScheduleTemplateAsync(HttpClient client, Guid studyGroupId)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/study-groups/{studyGroupId}/schedule-templates",
            new
            {
                dayOfWeek = "Tuesday",
                startTime = "18:00:00",
                durationMinutes = 60,
                roomId = (Guid?)null,
                teacherId = (Guid?)null,
                validFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                validTo = (DateOnly?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create schedule template: {await response.Content.ReadAsStringAsync()}");
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
