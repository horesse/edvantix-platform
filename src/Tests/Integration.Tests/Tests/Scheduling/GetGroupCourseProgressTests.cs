using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Scheduling.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Scheduling;

/// <summary>
/// End-to-end coverage for <c>GET /study-groups/{id}/course-progress</c> (EDX-019). The query lives
/// in Scheduling (not Curriculum): <c>totalLessons</c> is Curriculum's, <c>passedLessons</c> is the
/// count of distinct <c>Session.LessonId</c> among held sessions — computed on the fly, no stored
/// projection. Mirrors the acceptance check in the backlog task: hold 3 sessions carrying lessons
/// from a 10-lesson course → "3 of 10".
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class GetGroupCourseProgressTests
{
    private readonly AuthHelper _auth;

    public GetGroupCourseProgressTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetGroupCourseProgress_Should_Return404_When_GroupDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/study-groups/{Guid.NewGuid()}/course-progress");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupCourseProgress_Should_Count_HeldSessionLessons_Against_CourseTotal()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var teacherId = await CreateTeacherAsync(client, uniqueId);
        var (courseId, lessonIds) = await CreatePublishedCourseWithLessonsAsync(client, uniqueId, lessonCount: 10);
        var studyGroupId = await CreateStudyGroupAsync(client, teacherId, courseId, uniqueId);
        await ActivateStudyGroupAsync(client, studyGroupId);

        // Hold 3 sessions, each carrying a distinct program lesson. Space them a day apart so the
        // teacher-conflict check never fires.
        var start = DateTimeOffset.UtcNow.AddDays(1);
        for (var i = 0; i < 3; i++)
        {
            var sessionId = await CreateSessionAsync(client, studyGroupId, teacherId, lessonIds[i], start.AddDays(i));
            await HoldSessionAsync(client, sessionId);
        }

        // A 4th session with a lesson, left Planned — must NOT count.
        await CreateSessionAsync(client, studyGroupId, teacherId, lessonIds[3], start.AddDays(5));

        using var response = await client.GetAsync(
            $"{TestConstants.SchedulingBasePath}/study-groups/{studyGroupId}/course-progress");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"course-progress lookup failed: {await response.Content.ReadAsStringAsync()}");

        var progress = await response.DeserializeAsync<CourseProgressDto>();
        progress.StudyGroupId.ShouldBe(studyGroupId);
        progress.CourseId.ShouldBe(courseId);
        progress.TotalLessons.ShouldBe(10);
        progress.PassedLessons.ShouldBe(3);
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

    private static async Task<(Guid CourseId, IReadOnlyList<Guid> LessonIds)> CreatePublishedCourseWithLessonsAsync(
        HttpClient client, string uniqueId, int lessonCount)
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
        var moduleId = await moduleResponse.DeserializeAsync<Guid>();

        var lessonIds = new List<Guid>(lessonCount);
        for (var i = 0; i < lessonCount; i++)
        {
            using var lessonResponse = await client.PostAsJsonAsync(
                $"{TestConstants.CurriculumBasePath}/modules/{moduleId}/lessons",
                new
                {
                    title = $"Lesson {i + 1} — {uniqueId}",
                    objectives = (string?)null,
                    content = (string?)null,
                    durationMinutes = 45,
                });
            lessonResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
                $"setup failed to create lesson {i + 1}: {await lessonResponse.Content.ReadAsStringAsync()}");
            lessonIds.Add(await lessonResponse.DeserializeAsync<Guid>());
        }

        using var publishResponse = await client.PostAsync(
            $"{TestConstants.CurriculumBasePath}/courses/{courseId}/publish", content: null);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to publish course: {await publishResponse.Content.ReadAsStringAsync()}");

        return (courseId, lessonIds);
    }

    private static async Task<Guid> CreateStudyGroupAsync(
        HttpClient client, Guid teacherId, Guid courseId, string uniqueId)
    {
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

        using var studentResponse = await client.PostAsJsonAsync(
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
        studentResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await studentResponse.Content.ReadAsStringAsync()}");
        var studentId = await studentResponse.DeserializeAsync<Guid>();

        using var enrollResponse = await client.PostAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/enrollments",
            new
            {
                studentIds = new[] { studentId },
                enrolledOn = (DateOnly?)null,
                tariffId = (Guid?)null,
                discountPercent = 0m,
            });
        enrollResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to enroll student: {await enrollResponse.Content.ReadAsStringAsync()}");

        return studyGroupId;
    }

    private static async Task ActivateStudyGroupAsync(HttpClient client, Guid studyGroupId)
    {
        using var response = await client.PostAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/activate", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to activate study group: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task<Guid> CreateSessionAsync(
        HttpClient client, Guid studyGroupId, Guid teacherId, Guid lessonId, DateTimeOffset startUtc)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.SchedulingBasePath}/sessions",
            new
            {
                studyGroupId,
                lessonId = (Guid?)lessonId,
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

    private static async Task HoldSessionAsync(HttpClient client, Guid sessionId)
    {
        using var response = await client.PostAsync(
            $"{TestConstants.SchedulingBasePath}/sessions/{sessionId}/hold", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to hold session: {await response.Content.ReadAsStringAsync()}");
    }
}
