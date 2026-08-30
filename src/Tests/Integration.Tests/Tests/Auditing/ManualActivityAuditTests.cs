using System.Text.Json;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Covers the manual <c>IAuditClient.WriteActivityAsync</c> calls added for non-CRUD, sensitive
/// reads that the EF interceptor cannot see: the debtors report (Payments) and internal student
/// notes (People). See docs/04 Задачи/Задачи · Доработки каркаса.md → Auditing.
/// Audit writes go through the async channel worker, so each test polls the list endpoint.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ManualActivityAuditTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly AuthHelper _auth;

    public ManualActivityAuditTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetDebtorsReport_Should_Write_An_Activity_Audit()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var report = await client.GetAsync($"{TestConstants.PaymentsBasePath}/reports/debtors");
        report.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await PollForActivityAsync(client, "GetDebtorsReport")).ShouldBeTrue(
            "an Activity audit named GetDebtorsReport should be recorded for the debtors export");
    }

    [Fact]
    public async Task GetStudentNotes_Should_Write_An_Activity_Audit()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var studentId = await CreateStudentAsync(client);
        var note = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students/{studentId}/notes",
            new { text = "Internal note." });
        note.StatusCode.ShouldBe(HttpStatusCode.OK);

        var notes = await client.GetAsync($"{TestConstants.PeopleBasePath}/students/{studentId}/notes");
        notes.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await PollForActivityAsync(client, "GetStudentNotes")).ShouldBeTrue(
            "an Activity audit named GetStudentNotes should be recorded for viewing internal notes");
    }

    private static async Task<bool> PollForActivityAsync(HttpClient client, string name)
    {
        for (int i = 0; i < 40; i++)
        {
            var response = await client.GetAsync(
                $"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=50&search={Uri.EscapeDataString(name)}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var paged = JsonSerializer.Deserialize<PagedResult<AuditSummaryDto>>(
                await response.Content.ReadAsStringAsync(), JsonOptions);

            if (paged is not null && paged.Items.Any(r => r.EventType == AuditEventType.Activity))
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
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
                birthDate = new DateOnly(2010, 1, 1),
                phone = "+10000000000",
                email = $"student-{uniqueId}@example.com",
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
