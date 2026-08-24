using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Payments.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Payments;

/// <summary>
/// End-to-end coverage for the <c>PerPackage</c> remaining-sessions projection surfaced on
/// <c>GET /students/{id}/balance</c> — see docs/02 Модули/Payments.md → «Баланс»/«Модель начисления».
/// Scenario: buy a package → hold K of its sessions → remaining = LessonsCount - K, computed live via
/// <c>IAttendanceQueryService.CountHeldSessionsAsync</c>, never a stored/decremented counter. Unit
/// tests (Payments.Tests/Features/GetStudentBalanceQueryHandlerTests.cs) cover the arithmetic against
/// the InMemory provider; this exercises the same path for real, across Payments + Scheduling +
/// StudyGroups against Postgres.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PackageBalanceTests
{
    private readonly AuthHelper _auth;

    public PackageBalanceTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetStudentBalance_Should_ComputePackageRemaining_As_LessonsCount_Minus_HeldSessions()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var teacherId = await CreateTeacherAsync(client, uniqueId);
        var studentId = await CreateStudentAsync(client, uniqueId);
        var courseId = await CreateCourseAsync(client, uniqueId);
        var tariffId = await CreateTariffAsync(client, courseId, uniqueId, lessonsCount: 5);
        var studyGroupId = await CreateStudyGroupAsync(client, teacherId, courseId, uniqueId);

        await EnrollStudentAsync(client, studyGroupId, studentId, tariffId);
        await ActivateStudyGroupAsync(client, studyGroupId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await GenerateAndIssuePackageInvoiceAsync(client, studyGroupId, today);

        // Hold 3 of the package's 5 lessons — 2 should remain.
        var sessionStart = DateTimeOffset.UtcNow.AddDays(1);
        for (var i = 0; i < 3; i++)
        {
            var sessionId = await CreateSessionAsync(client, studyGroupId, teacherId, sessionStart.AddHours(i * 2));
            await HoldSessionAsync(client, sessionId);
        }

        using var response = await client.GetAsync($"{TestConstants.PaymentsBasePath}/students/{studentId}/balance");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"balance lookup failed: {await response.Content.ReadAsStringAsync()}");

        var balance = await response.DeserializeAsync<StudentBalanceDto>();
        balance.Packages.Count.ShouldBe(1);
        var package = balance.Packages[0];
        package.LessonsCount.ShouldBe(5);
        package.UsedCount.ShouldBe(3);
        package.RemainingCount.ShouldBe(2);
        package.IsExpired.ShouldBeFalse();
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

    private static async Task<Guid> CreateCourseAsync(HttpClient client, string uniqueId)
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

        return courseId;
    }

    private static async Task<Guid> CreateTariffAsync(HttpClient client, Guid courseId, string uniqueId, int lessonsCount)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/tariffs",
            new
            {
                name = $"Package-{uniqueId}",
                courseId,
                kind = "PerPackage",
                amount = 200m,
                currency = "USD",
                lessonsCount,
                validDays = 60,
                chargeOnExcusedAbsence = false,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create tariff: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> CreateStudyGroupAsync(HttpClient client, Guid teacherId, Guid courseId, string uniqueId)
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
        return await groupResponse.DeserializeAsync<Guid>();
    }

    private static async Task EnrollStudentAsync(HttpClient client, Guid studyGroupId, Guid studentId, Guid tariffId)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/enrollments",
            new
            {
                studentIds = new[] { studentId },
                enrolledOn = (DateOnly?)null,
                tariffId = (Guid?)tariffId,
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

    /// <summary>Generates the group's draft invoices for a single-day period and issues them
    /// immediately — the package's counting window starts at <c>IssuedOn</c>, which
    /// <c>BulkGenerateInvoicesCommandHandler</c> sets to <paramref name="periodTo"/>.</summary>
    private static async Task GenerateAndIssuePackageInvoiceAsync(HttpClient client, Guid studyGroupId, DateOnly periodTo)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/bulk-generate",
            new
            {
                studyGroupId,
                periodFrom = periodTo,
                periodTo,
                dueDate = periodTo.AddDays(7),
                issueImmediately = true,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to generate package invoice: {await response.Content.ReadAsStringAsync()}");
        var invoiceIds = await response.DeserializeAsync<List<Guid>>();
        invoiceIds.ShouldHaveSingleItem();
    }

    private static async Task<Guid> CreateSessionAsync(HttpClient client, Guid studyGroupId, Guid teacherId, DateTimeOffset startUtc)
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
