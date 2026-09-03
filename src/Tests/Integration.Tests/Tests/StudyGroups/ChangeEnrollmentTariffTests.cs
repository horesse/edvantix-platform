using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Payments.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.StudyGroups;

/// <summary>
/// Covers EDX-007 end to end: <c>PUT /api/v1/enrollments/{id}/tariff</c> re-prices a live enrollment
/// without re-enrolling, and the <b>next</b> bulk invoice run for the group picks up the new tariff —
/// past invoices are untouched because <c>BulkGenerateInvoicesCommandHandler</c> resolves each
/// enrollment's tariff live from <c>IStudyGroupQueryService</c>. Spans StudyGroups + Payments +
/// Curriculum against Postgres; the domain-level guards are unit-tested in StudyGroups.Tests.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ChangeEnrollmentTariffTests
{
    private readonly AuthHelper _auth;

    public ChangeEnrollmentTariffTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ChangeEnrollmentTariff_Should_BeUsedByNextBulkInvoiceRun()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var teacherId = await CreateTeacherAsync(client, uniqueId);
        var studentId = await CreateStudentAsync(client, uniqueId);
        var courseId = await CreateCourseAsync(client, uniqueId);
        var cheapTariffId = await CreateOneTimeTariffAsync(client, courseId, $"cheap-{uniqueId}", amount: 100m);
        var pricyTariffId = await CreateOneTimeTariffAsync(client, courseId, $"pricy-{uniqueId}", amount: 250m);
        var studyGroupId = await CreateStudyGroupAsync(client, teacherId, courseId, uniqueId);

        var enrollmentId = await EnrollStudentAsync(client, studyGroupId, studentId, cheapTariffId);
        await ActivateStudyGroupAsync(client, studyGroupId);

        // Re-price the running enrollment.
        using var changeResponse = await client.PutAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/enrollments/{enrollmentId}/tariff",
            new { tariffId = (Guid?)pricyTariffId, discountPercent = 0m });
        changeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"change tariff failed: {await changeResponse.Content.ReadAsStringAsync()}");

        // Next bulk run must bill against the new tariff.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoiceId = await BulkGenerateSingleInvoiceAsync(client, studyGroupId, today);

        using var invoiceResponse = await client.GetAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}");
        invoiceResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"invoice lookup failed: {await invoiceResponse.Content.ReadAsStringAsync()}");

        var invoice = await invoiceResponse.DeserializeAsync<StudentInvoiceDetailDto>();
        invoice.Lines.ShouldHaveSingleItem();
        invoice.Lines[0].TariffId.ShouldBe(pricyTariffId);
        invoice.Lines[0].Amount.ShouldBe(250m);
        invoice.Total.ShouldBe(250m);
    }

    [Fact]
    public async Task ChangeEnrollmentTariff_Should_RejectDiscountAbove100()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var teacherId = await CreateTeacherAsync(client, uniqueId);
        var studentId = await CreateStudentAsync(client, uniqueId);
        var courseId = await CreateCourseAsync(client, uniqueId);
        var tariffId = await CreateOneTimeTariffAsync(client, courseId, uniqueId, amount: 100m);
        var studyGroupId = await CreateStudyGroupAsync(client, teacherId, courseId, uniqueId);
        var enrollmentId = await EnrollStudentAsync(client, studyGroupId, studentId, tariffId);

        using var response = await client.PutAsJsonAsync(
            $"{TestConstants.StudyGroupsBasePath}/enrollments/{enrollmentId}/tariff",
            new { tariffId = (Guid?)tariffId, discountPercent = 150m });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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

    private static async Task<Guid> CreateOneTimeTariffAsync(HttpClient client, Guid courseId, string uniqueId, decimal amount)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/tariffs",
            new
            {
                name = $"Tariff-{uniqueId}",
                courseId,
                kind = "OneTime",
                amount,
                currency = "USD",
                lessonsCount = 0,
                validDays = 0,
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

    private static async Task<Guid> EnrollStudentAsync(HttpClient client, Guid studyGroupId, Guid studentId, Guid tariffId)
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
        var ids = await response.DeserializeAsync<List<Guid>>();
        return ids.ShouldHaveSingleItem();
    }

    private static async Task ActivateStudyGroupAsync(HttpClient client, Guid studyGroupId)
    {
        using var response = await client.PostAsync(
            $"{TestConstants.StudyGroupsBasePath}/study-groups/{studyGroupId}/activate", content: null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent,
            $"setup failed to activate study group: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task<Guid> BulkGenerateSingleInvoiceAsync(HttpClient client, Guid studyGroupId, DateOnly period)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/bulk-generate",
            new
            {
                studyGroupId,
                periodFrom = period,
                periodTo = period,
                dueDate = period.AddDays(7),
                issueImmediately = false,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"bulk-generate failed: {await response.Content.ReadAsStringAsync()}");
        var invoiceIds = await response.DeserializeAsync<List<Guid>>();
        return invoiceIds.ShouldHaveSingleItem();
    }
}
