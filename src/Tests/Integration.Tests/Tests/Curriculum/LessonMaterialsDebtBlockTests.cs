using System.Net;
using System.Net.Http.Json;
using FSH.Modules.Curriculum.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.Curriculum;

/// <summary>
/// EDX-015 — with <c>TenantSettings.RestrictMaterialsOnDebt</c> armed, a student overdue past the
/// grace window loses lesson materials (403) while the schedule stays reachable (200); paying the
/// invoice restores access. Also proves cross-tenant isolation: tenant B's flag + debt never
/// affect tenant A. The rule itself is unit-tested in Payments.Tests/Services/MaterialsAccessServiceTests.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class LessonMaterialsDebtBlockTests
{
    private readonly AuthHelper _auth;

    public LessonMaterialsDebtBlockTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Materials_Are_Blocked_While_Overdue_Past_Grace_But_Schedule_Stays_Open()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var client = await ProvisionTenantClientAsync(rootClient, $"edx015-{uniqueId}");

        // The tenant admin becomes "the student": link their Identity user to a Student row so
        // IPeopleScopeResolver resolves a StudentId (and no TeacherId → not exempt).
        var profile = await (await client.GetAsync($"{TestConstants.IdentityBasePath}/profile"))
            .DeserializeAsync<UserDto>();
        var studentId = await CreateStudentAsync(client);
        using (var link = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/link-user", new { userId = profile.Id }))
        {
            link.StatusCode.ShouldBe(HttpStatusCode.NoContent, await link.Content.ReadAsStringAsync());
        }

        var lessonId = await CreateLessonWithMaterialAsync(client);

        // An invoice that is 30 days overdue.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoiceId = await CreateOverdueIssuedInvoiceAsync(client, studentId, dueDate: today.AddDays(-30));

        // Flag OFF (default) → materials visible.
        (await GetMaterialsStatusAsync(client, lessonId)).ShouldBe(HttpStatusCode.OK);

        // Arm the flag with a zero-day grace → materials blocked, schedule still reachable.
        await SetDebtRestrictionAsync(client, restrict: true, graceDays: 0);
        (await GetMaterialsStatusAsync(client, lessonId)).ShouldBe(HttpStatusCode.Forbidden);
        (await GetMyScheduleStatusAsync(client)).ShouldBe(HttpStatusCode.OK);

        // Paying the overdue invoice in full over HTTP clears the debt → materials reachable again
        // (EDX-020 — this leg used to fail with a permanent 409 from the payment endpoint).
        using (var pay = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}/payments",
            new { amount = 100m, paidOn = today, method = "Cash", reference = (string?)null, proofFileId = (Guid?)null, note = (string?)null }))
        {
            pay.StatusCode.ShouldBe(HttpStatusCode.OK, await pay.Content.ReadAsStringAsync());
        }
        (await GetMaterialsStatusAsync(client, lessonId)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Debt_And_Flag_In_One_Tenant_Do_Not_Block_Another_Tenant()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var tenantA = await ProvisionTenantClientAsync(rootClient, $"edx015a-{uniqueId}");
        using var tenantB = await ProvisionTenantClientAsync(rootClient, $"edx015b-{uniqueId}");

        // Tenant A: linked student, overdue invoice, flag ON → blocked.
        var profileA = await (await tenantA.GetAsync($"{TestConstants.IdentityBasePath}/profile")).DeserializeAsync<UserDto>();
        var studentA = await CreateStudentAsync(tenantA);
        using (var link = await tenantA.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentA}/link-user", new { userId = profileA.Id }))
        {
            link.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateOverdueIssuedInvoiceAsync(tenantA, studentA, dueDate: today.AddDays(-30));
        await SetDebtRestrictionAsync(tenantA, restrict: true, graceDays: 0);
        var lessonA = await CreateLessonWithMaterialAsync(tenantA);
        (await GetMaterialsStatusAsync(tenantA, lessonA)).ShouldBe(HttpStatusCode.Forbidden);

        // Tenant B: its own linked student, its own lesson, NO flag, NO debt → unaffected.
        var profileB = await (await tenantB.GetAsync($"{TestConstants.IdentityBasePath}/profile")).DeserializeAsync<UserDto>();
        var studentB = await CreateStudentAsync(tenantB);
        using (var link = await tenantB.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentB}/link-user", new { userId = profileB.Id }))
        {
            link.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        var lessonB = await CreateLessonWithMaterialAsync(tenantB);
        (await GetMaterialsStatusAsync(tenantB, lessonB)).ShouldBe(HttpStatusCode.OK);
    }

    // ─── steps ───────────────────────────────────────────────────────

    private static async Task<HttpStatusCode> GetMaterialsStatusAsync(HttpClient client, Guid lessonId)
    {
        using var resp = await client.GetAsync($"{TestConstants.CurriculumBasePath}/lessons/{lessonId}/materials");
        return resp.StatusCode;
    }

    private static async Task<HttpStatusCode> GetMyScheduleStatusAsync(HttpClient client)
    {
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(7).ToString("O"));
        using var resp = await client.GetAsync($"{TestConstants.SchedulingBasePath}/sessions/my?from={from}&to={to}");
        return resp.StatusCode;
    }

    private static async Task SetDebtRestrictionAsync(HttpClient client, bool restrict, int graceDays)
    {
        using var resp = await client.PutAsJsonAsync(
            $"{TestConstants.TenantsBasePath}/settings",
            new { timeZoneId = "UTC", currency = "USD", restrictMaterialsOnDebt = restrict, debtGraceDays = graceDays });
        resp.StatusCode.ShouldBe(HttpStatusCode.NoContent, await resp.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> CreateOverdueIssuedInvoiceAsync(HttpClient client, Guid studentId, DateOnly dueDate)
    {
        using var create = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices",
            new
            {
                studentId,
                payerGuardianId = (Guid?)null,
                studyGroupId = (Guid?)null,
                periodFrom = dueDate.AddMonths(-1),
                periodTo = dueDate,
                dueDate,
                currency = "USD",
                comment = (string?)null,
                lines = new[] { new { description = "Обучение", tariffId = (Guid?)null, quantity = 1m, unitPrice = 100m } },
            });
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await create.Content.ReadAsStringAsync());
        var invoiceId = await create.DeserializeAsync<Guid>();

        using var issue = await client.PostAsJsonAsync(
            $"{TestConstants.PaymentsBasePath}/student-invoices/{invoiceId}/issue",
            new { issuedOn = dueDate.AddMonths(-1) });
        issue.StatusCode.ShouldBe(HttpStatusCode.NoContent, await issue.Content.ReadAsStringAsync());

        return invoiceId;
    }

    private static async Task<Guid> CreateLessonWithMaterialAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var subjectId = await PostForGuidAsync(client, $"{TestConstants.CurriculumBasePath}/subjects",
            new { name = $"Subject-{uniqueId}", parentId = (Guid?)null });
        var courseId = await PostForGuidAsync(client, $"{TestConstants.CurriculumBasePath}/courses",
            new { subjectId, title = $"Course-{uniqueId}", description = (string?)null, level = "Beginner", durationHours = 10, coverFileId = (Guid?)null });
        var moduleId = await PostForGuidAsync(client, $"{TestConstants.CurriculumBasePath}/courses/{courseId}/modules",
            new { title = $"Module-{uniqueId}", description = (string?)null });
        var lessonId = await PostForGuidAsync(client, $"{TestConstants.CurriculumBasePath}/modules/{moduleId}/lessons",
            new { title = $"Lesson-{uniqueId}", objectives = (string?)null, content = (string?)null, durationMinutes = 45 });

        using var mat = await client.PostAsJsonAsync(
            $"{TestConstants.CurriculumBasePath}/lessons/{lessonId}/materials",
            new { kind = "Link", title = "Видео", fileId = (Guid?)null, url = "https://example.com/v", visibleToStudents = true });
        mat.StatusCode.ShouldBe(HttpStatusCode.OK, await mat.Content.ReadAsStringAsync());

        return lessonId;
    }

    private static async Task<Guid> CreateStudentAsync(HttpClient client)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return await PostForGuidAsync(client, $"{TestConstants.PeopleBasePath}/students",
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
    }

    private static async Task<Guid> PostForGuidAsync(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"POST {url} failed: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    // ─── tenant provisioning (same pattern as CurriculumTenantIsolationTests) ──

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
            issuer = $"{tenantId}.issuer",
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

        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }
}
