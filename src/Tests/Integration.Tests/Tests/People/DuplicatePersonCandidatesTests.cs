using System.Net;
using System.Net.Http.Json;
using FSH.Modules.People.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Shouldly;
using Xunit;

namespace Integration.Tests.Tests.People;

/// <summary>
/// EDX-018 — the soft duplicate check behind the create-person dialogs
/// (<c>GET /api/v1/people/duplicate-candidates</c>). Asserts the three cases that shape it:
/// name + phone match surfaces a candidate, a differently-formatted phone still matches, and a
/// family sharing one phone across children with different names does NOT (the reason there is
/// no unique index).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class DuplicatePersonCandidatesTests
{
    private readonly AuthHelper _auth;

    public DuplicatePersonCandidatesTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Should_Flag_Existing_Person_When_Name_And_Phone_Match()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var lastName = $"Дубликат{tag}";
        var studentId = await CreateStudentAsync(client, lastName, "Иван", "+7 900 123-45-67", $"ivan-{tag}@example.com");

        // Same person re-entered, phone typed without any formatting.
        var candidates = await FindAsync(client, lastName, "Иван", phone: "79001234567", email: null);

        candidates.ShouldContain(c => c.Id == studentId && c.PersonType == "Student" && c.PhoneMatches);
    }

    [Fact]
    public async Task Should_Not_Flag_Family_Sharing_One_Phone_With_Different_Names()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var sharedPhone = "+7 911 000-11-22";
        await CreateStudentAsync(client, $"Семья{tag}", "Пётр", sharedPhone, $"petr-{tag}@example.com");

        // A sibling on the same contact phone — different first name, so not a duplicate.
        var candidates = await FindAsync(client, $"Семья{tag}", "Мария", phone: sharedPhone, email: null);

        candidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Contact_Supplied()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await CreateStudentAsync(client, $"Контакт{tag}", "Ольга", "+7 900 555-66-77", $"olga-{tag}@example.com");

        var candidates = await FindAsync(client, $"Контакт{tag}", "Ольга", phone: null, email: null);

        candidates.ShouldBeEmpty();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<IReadOnlyList<DuplicatePersonCandidateDto>> FindAsync(
        HttpClient client, string lastName, string firstName, string? phone, string? email)
    {
        var q = new List<string>
        {
            $"lastName={Uri.EscapeDataString(lastName)}",
            $"firstName={Uri.EscapeDataString(firstName)}",
        };
        if (phone is not null)
        {
            q.Add($"phone={Uri.EscapeDataString(phone)}");
        }
        if (email is not null)
        {
            q.Add($"email={Uri.EscapeDataString(email)}");
        }

        using var response = await client.GetAsync(
            $"{TestConstants.PeopleBasePath}/people/duplicate-candidates?{string.Join('&', q)}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"duplicate-candidates failed: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<List<DuplicatePersonCandidateDto>>();
    }

    private static async Task<Guid> CreateStudentAsync(
        HttpClient client, string lastName, string firstName, string phone, string email)
    {
        using var response = await client.PostAsJsonAsync(
            $"{TestConstants.PeopleBasePath}/students",
            new
            {
                lastName,
                firstName,
                middleName = (string?)null,
                birthDate = new DateOnly(2010, 1, 1),
                phone,
                email,
                managerUserId = Guid.NewGuid().ToString(),
                source = (string?)null,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create student: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }
}
