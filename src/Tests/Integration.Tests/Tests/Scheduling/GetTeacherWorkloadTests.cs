using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Scheduling.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Scheduling;

/// <summary>
/// End-to-end coverage for <c>GET /teachers/{id}/workload</c> — the query lives in Scheduling (not
/// People, which must not depend on it), and its "active groups" count runs an EF <c>Union</c> across
/// two queries in <c>StudyGroupQueryService.GetActiveGroupIdsForTeacherAsync</c>. Unit tests
/// (StudyGroups.Tests, Scheduling.Tests) cover the LINQ against the InMemory provider; this exercises
/// the same query translated for real against Postgres — see docs/02 Модули/Scheduling.md.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class GetTeacherWorkloadTests
{
    private readonly AuthHelper _auth;

    public GetTeacherWorkloadTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetTeacherWorkload_Should_Return404_When_TeacherDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/teachers/{Guid.NewGuid()}/workload");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeacherWorkload_Should_CountActiveGroupAndSessionsInPeriod()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var teacherId = await CreateTeacherAsync(client, uniqueId);
        var studentId = await CreateStudentAsync(client, uniqueId);
        var studyGroupId = await CreateStudyGroupAsync(client, teacherId, uniqueId);

        await EnrollStudentAsync(client, studyGroupId, studentId);
        await ActivateStudyGroupAsync(client, studyGroupId);

        var sessionStart = DateTimeOffset.UtcNow.AddDays(7);
        await CreateSessionAsync(client, studyGroupId, teacherId, sessionStart);

        var from = DateOnly.FromDateTime(sessionStart.UtcDateTime.AddDays(-1));
        var to = DateOnly.FromDateTime(sessionStart.UtcDateTime.AddDays(1));

        using var response = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/teachers/{teacherId}/workload?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"workload lookup failed: {await response.Content.ReadAsStringAsync()}");

        var workload = await response.DeserializeAsync<TeacherWorkloadDto>();
        workload.TeacherId.ShouldBe(teacherId);
        workload.ActiveGroupsCount.ShouldBe(1);
        workload.SessionsCount.ShouldBe(1);
        workload.TotalHours.ShouldBe(1m);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateTeacherAsync(HttpClient client, string uniqueId)
    {
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

    private static async Task<Guid> CreateStudentAsync(HttpClient client, string uniqueId)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students",
            new
            {
                lastName = $"Student-{uniqueId}",
                firstName = "Test",
                middleName = (string?)null,
                birthDate = new DateOnly(2012, 6, 15),
                phone = "+10000000002",
                email = $"student-{uniqueId}@example.com",
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateStudyGroupAsync(HttpClient client, Guid teacherId, string uniqueId)
    {
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
        return await groupResponse.DeserializeAsync<Guid>();
    }

    private static async Task EnrollStudentAsync(HttpClient client, Guid studyGroupId, Guid studentId)
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
    }

    private static async Task ActivateStudyGroupAsync(HttpClient client, Guid studyGroupId)
    {
        using var response = await client.PostAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/activate", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to activate study group: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task CreateSessionAsync(
        HttpClient client, Guid studyGroupId, Guid teacherId, DateTimeOffset startUtc)
    {
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
    }
}
